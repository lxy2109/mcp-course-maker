using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ModelParameterLib.Module;
using System;
using System.Collections;
using Newtonsoft.Json;

namespace ModelParameterLib.Core{
/// <summary>
/// 3D模型自动贴合与自适应摆放工具（支持AI参数、分组、多分布方式）
/// </summary>
public class AutoPlacer : MonoBehaviour
{
    public enum LayoutMode { Linear, Grid, Circle }

    [System.Serializable]
    public class ItemRule
    {
        [BoxGroup("基础信息")]
        [LabelText("关键字")]
        public string keyword;

        [BoxGroup("基础信息")]
        [LabelText("父物体关键字")]
        public string parentKeyword;

        [BoxGroup("最终变换")]
        [LabelText("最终世界坐标")]
        public Vector3 position;

        [BoxGroup("最终变换")]
        [LabelText("最终世界欧拉角")]
        public Vector3 rotation;

        [BoxGroup("分布与分组")]
        [LabelText("分布方式")]
        public LayoutMode layoutMode = LayoutMode.Linear;

        [BoxGroup("分布与分组")]
        [LabelText("网格行数")]
        public int gridRow = 1;

        [BoxGroup("分布与分组")]
        [LabelText("圆形半径")]
        public float circleRadius = 1.0f;

        [BoxGroup("分布与分组")]
        [LabelText("分组名")]
        public string group;
    }

    // 支持AI扩展字段
    [System.Serializable]
    public class ItemRuleExt : ItemRule
    {
        public Vector3 frontDir = Vector3.zero; // AI mesh分析正面
    }

    [ShowInInspector]
    [DictionaryDrawerSettings(KeyLabel = "模型名/关键字", ValueLabel = "AI规则")]
    public Dictionary<string, ItemRule> itemRules = new Dictionary<string, ItemRule>();

    [BoxGroup("基础参数")]
    [LabelText("主轴方向（如Y为上下贴合）")]
    public Vector3 mainAxis = Vector3.up;

    [BoxGroup("基础参数")]
    [LabelText("横向排列间距")]
    public float horizontalSpacing = 0.2f;

    [BoxGroup("基础参数")]
    [LabelText("网格分布间距")]
    public Vector2 gridSpacing = new Vector2(0.3f, 0.3f);

    [Button("一键AI分析并填充规则")]
    public void AIAnalyzeButton()
    {
        AnalyzeAllWithAI();
    }

    [Button("自动摆放（应用规则）")]
    public void AutoArrangeButton()
    {
        AutoArrange();
    }

    /// <summary>
    /// 一键AI分析所有模型，填充itemRules，分析结果立即显示在Inspector
    /// </summary>
    public void AnalyzeAllWithAI(string sceneHint = "")
    {
        var root = GameObject.Find("GameObjectRoot");
        if (root == null)
        {
            Debug.LogError("未找到名为GameObjectRoot的GameObject！");
            return;
        }
        var aiFiller = new ModelAIFiller();
        itemRules.Clear();
        int total = root.transform.childCount;
        int finished = 0;
        string logDir = "Assets/ModelParameterLib/AIPlacementLogs";
#if UNITY_EDITOR
        try {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path)) {
                string sceneDir = System.IO.Path.GetDirectoryName(scene.path);
                if (!string.IsNullOrEmpty(sceneDir)) {
                    logDir = System.IO.Path.Combine(sceneDir, "AIPlacementLogs");
                }
            }
        } catch (Exception ex) {
            Debug.LogWarning($"[AutoPlacer] 获取场景目录失败，使用默认AIPlacementLogs目录: {ex.Message}");
        }
#endif
        if (!System.IO.Directory.Exists(logDir))
            System.IO.Directory.CreateDirectory(logDir);
        Debug.Log($"[AutoPlacer] AI分析日志保存目录: {logDir}");
        foreach (Transform child in root.transform)
        {
            string modelName = child.gameObject.name;
            // 获取bounds信息
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            var mr = child.GetComponent<MeshRenderer>();
            if (mr != null) bounds = mr.bounds;
            else {
                var mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) bounds = mf.sharedMesh.bounds;
            }
            Vector3 scale = child.localScale;
            string boundsInfo = $"Bounds center: {bounds.center}, size: {bounds.size}, scale: {scale}";
            string fullSceneHint = sceneHint + $"\n{boundsInfo}";
            // 记录请求参数（只序列化基础数值，避免循环引用）
            var requestLog = new {
                modelName = modelName,
                boundsCenter = new { x = bounds.center.x, y = bounds.center.y, z = bounds.center.z },
                boundsSize = new { x = bounds.size.x, y = bounds.size.y, z = bounds.size.z },
                scale = new { x = scale.x, y = scale.y, z = scale.z },
                sceneHint = fullSceneHint,
                time = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string logPath = $"{logDir}/{modelName}_log.json";
            if (System.IO.File.Exists(logPath))
            {
                // 直接读取json，解析为ItemRule
                string jsonText = System.IO.File.ReadAllText(logPath);
                var logObj = JsonConvert.DeserializeObject<AIPlacementLog>(jsonText);
                if (logObj != null && !string.IsNullOrEmpty(logObj.aiResult))
                {
                    var rule = ParseAIPlacementJson(modelName, logObj.aiResult);
                    if (rule != null)
                        itemRules[modelName] = rule;
                }
                finished++;
                if (finished == total)
                {
#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
                    Debug.Log("AI分析全部完成！（部分或全部使用缓存）");
                }
                continue;
            }
            // 没有缓存才调用AI
            aiFiller.AnalyzePlacementAsync(
                modelName,
                fullSceneHint,
                ModelAIFiller.DeepSeekApiKey,
                (success, json, error) =>
                {
                    finished++;
                    // 记录AI返回
                    var logObj = new AIPlacementLog {
                        request = JsonConvert.SerializeObject(requestLog, Formatting.Indented),
                        aiResult = json,
                        error = error
                    };
                    System.IO.File.WriteAllText(logPath, JsonConvert.SerializeObject(logObj, Formatting.Indented));
                    if (success)
                    {
                        var rule = ParseAIPlacementJson(modelName, json);
                        if (rule != null)
                            itemRules[modelName] = rule;
                    }
                    else
                    {
                        Debug.LogError($"AI分析失败: {modelName} {error}");
                    }
                    if (finished == total)
                    {
#if UNITY_EDITOR
                        EditorUtility.SetDirty(this);
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
                        Debug.Log("AI分析全部完成！");
                    }
                }
            );
        }
    }

    /// <summary>
    /// 自动批量摆放所有子物体（支持分组、分布方式、AI参数）
    /// 只根据itemRules内容进行实际摆放，不再调用AI
    /// </summary>
    public void AutoArrange()
    {
        Debug.Log("[AutoPlacer] 开始自动摆放");
        // 1. 获取GameObjectRoot下的所有子物体
        var allObjs = new List<GameObject>();
        var root = GameObject.Find("GameObjectRoot");
        if (root == null)
        {
            Debug.LogError("未找到名为GameObjectRoot的GameObject！");
            return;
        }
        foreach (Transform child in root.transform)
            allObjs.Add(child.gameObject);

        // 2. 按分组处理（无group的为默认组）
        var groupDict = new Dictionary<string, List<GameObject>>();
        foreach (var obj in allObjs)
        {
            var rule = GetRule(obj.name);
            string group = rule != null && !string.IsNullOrEmpty(rule.group) ? rule.group : "__default__";
            if (!groupDict.ContainsKey(group)) groupDict[group] = new List<GameObject>();
            groupDict[group].Add(obj);
        }

        foreach (var group in groupDict)
        {
            Debug.Log($"[AutoPlacer] 正在摆放分组: {group.Key}, 包含{group.Value.Count}个物体");
            ArrangeGroup(group.Value);
        }
        Debug.Log("[AutoPlacer] 自动摆放完成");
    }

    /// <summary>
    /// 按组自动摆放
    /// </summary>
    void ArrangeGroup(List<GameObject> objs)
    {
        if (objs == null || objs.Count == 0) return;
        GameObject baseObj = FindBaseObject(objs);
        if (baseObj == null) baseObj = objs[0];
        Debug.Log($"[AutoPlacer] 分组基准物体: {baseObj.name}");
        var children = new List<GameObject>();
        foreach (var obj in objs)
        {
            if (obj == baseObj) continue;
            var rule = GetRule(obj.name);
            if (rule == null || string.IsNullOrEmpty(rule.parentKeyword) || baseObj.name.Contains(rule.parentKeyword))
                children.Add(obj);
        }
        LayoutMode mode = LayoutMode.Linear;
        int gridRow = 1;
        float circleRadius = 1.0f;
        if (children.Count > 0)
        {
            var rule = GetRule(children[0].name);
            if (rule != null)
            {
                mode = rule.layoutMode;
                gridRow = rule.gridRow;
                circleRadius = rule.circleRadius;
            }
        }
        Debug.Log($"[AutoPlacer] 分布方式: {mode}, gridRow: {gridRow}, circleRadius: {circleRadius}");
        switch (mode)
        {
            case LayoutMode.Linear:
                PlaceLinear(children, baseObj);
                break;
            case LayoutMode.Grid:
                PlaceGrid(children, baseObj, gridRow);
                break;
            case LayoutMode.Circle:
                PlaceCircle(children, baseObj, circleRadius);
                break;
        }
    }

    /// <summary>
    /// 查找分组的基准物体：优先用parentKeyword在场景中找，有则用；否则用objs中最底层（没有其它物体以它为parent的）
    /// </summary>
    GameObject FindBaseObject(List<GameObject> objs)
    {
        // 1. 优先查找parentKeyword在场景中存在的物体
        foreach (var obj in objs)
        {
            var rule = GetRule(obj.name);
            if (rule != null && !string.IsNullOrEmpty(rule.parentKeyword))
            {
                var parentGo = GameObject.Find(rule.parentKeyword);
                if (parentGo != null)
                    return parentGo;
            }
        }
        // 2. 查找最底层父物体（没有其它物体以它为parentKeyword）
        foreach (var obj in objs)
        {
            bool isBase = true;
            foreach (var other in objs)
            {
                if (other == obj) continue;
                var rule = GetRule(other.name);
                if (rule != null && !string.IsNullOrEmpty(rule.parentKeyword) && rule.parentKeyword == obj.name)
                {
                    isBase = false;
                    break;
                }
            }
            if (isBase) return obj;
        }
        // 3. 都找不到，返回第一个
        return objs.Count > 0 ? objs[0] : null;
    }

    /// <summary>
    /// 线性分布（已禁用自动横向偏移，只用AI建议位置）
    /// </summary>
    void PlaceLinear(List<GameObject> objs, GameObject baseObj)
    {
        foreach (var obj in objs)
        {
            SetDefaultRotation(obj);
            PlaceOnTop(baseObj, obj);
            ApplyOffset(obj);
        }
    }

    /// <summary>
    /// 网格分布（已禁用自动网格偏移，只用AI建议位置）
    /// </summary>
    void PlaceGrid(List<GameObject> objs, GameObject baseObj, int row)
    {
        foreach (var obj in objs)
        {
            SetDefaultRotation(obj);
            PlaceOnTop(baseObj, obj);
            ApplyOffset(obj);
        }
    }

    /// <summary>
    /// 圆形分布（已禁用自动圆形偏移，只用AI建议位置）
    /// </summary>
    void PlaceCircle(List<GameObject> objs, GameObject baseObj, float radius)
    {
        foreach (var obj in objs)
        {
            SetDefaultRotation(obj);
            PlaceOnTop(baseObj, obj);
            ApplyOffset(obj);
        }
    }

    /// <summary>
    /// 设置默认朝向（始终采用AI分析结果）
    /// </summary>
    void SetDefaultRotation(GameObject obj)
    {
        // 1. 默认平放（本地X轴朝上，Z轴为正面）
        obj.transform.rotation = Quaternion.Euler(90, 0, 0);

        // 2. 让Z轴正对摄像机（只调整Y轴）
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 objPos = obj.transform.position;
            Vector3 camPos = cam.transform.position;
            Vector3 lookDir = camPos - objPos;
            lookDir.y = 0; // 只绕Y轴旋转
            if (lookDir.sqrMagnitude > 0.001f)
            {
                float targetY = Quaternion.LookRotation(lookDir).eulerAngles.y;
                Vector3 euler = obj.transform.eulerAngles;
                obj.transform.eulerAngles = new Vector3(90, targetY, 0);
                Debug.Log($"[AutoPlacer] {obj.name} 平放，正面已朝向主摄像机，最终rotation: {{90, {targetY}, 0}}");
            }
        }
        else
        {
            obj.transform.rotation = Quaternion.Euler(90, 0, 0);
            Debug.Log($"[AutoPlacer] {obj.name} 平放，主摄像机未找到，rotation: {{90, 0, 0}}");
        }
        UpdateRuleTransform(obj.name, obj.transform.position, obj.transform.rotation.eulerAngles);
    }

    // mesh主面法线分析：遍历所有三角面，找最大面积面法线
    Vector3 AnalyzeMeshMainFaceNormal(GameObject obj)
    {
        var mf = obj.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return Vector3.forward;
        var mesh = mf.sharedMesh;
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        float maxArea = 0f;
        Vector3 mainNormal = Vector3.forward;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            if (area > maxArea)
            {
                maxArea = area;
                mainNormal = normal;
            }
        }
        return mainNormal;
    }

    // 让物体正面（Z轴）绕Y轴朝向主摄像机
    void FaceToMainCameraY(GameObject obj)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 objPos = obj.transform.position;
        Vector3 camPos = cam.transform.position;
        Vector3 lookDir = camPos - objPos;
        lookDir.y = 0; // 只绕Y轴旋转
        if (lookDir.sqrMagnitude > 0.001f)
        {
            float targetY = Quaternion.LookRotation(lookDir).eulerAngles.y;
            Vector3 euler = obj.transform.eulerAngles;
            obj.transform.eulerAngles = new Vector3(euler.x, targetY, euler.z);
            Debug.Log($"[AutoPlacer] {obj.name} 正面已朝向主摄像机，Y轴: {targetY}");
        }
    }

    /// <summary>
    /// 贴合到baseObj上表面（主轴贴合，MeshRenderer.bounds世界空间贴合）
    /// </summary>
    void PlaceOnTop(GameObject baseObj, GameObject objToPlace)
    {
        var rule = GetRule(objToPlace.name);
        if (rule != null)
        {
            // 1. 以分析出的基准物体为参考，AI建议的position为相对偏移（只用X/Z，Y完全由bounds计算）
            Vector3 basePos = baseObj.transform.position;
            Vector3 aiOffset = rule.position;
            float aiY = aiOffset.y;
            aiOffset.y = 0f; // 强制Y为0
            // 先设置rotation
            SetDefaultRotation(objToPlace);
            // 设置X/Z
            Vector3 pos = new Vector3(basePos.x + aiOffset.x, basePos.y, basePos.z + aiOffset.z);
            objToPlace.transform.position = pos;
            // 获取MeshRenderer.bounds（世界空间）
            var baseMr = baseObj.GetComponent<MeshRenderer>();
            var objMr = objToPlace.GetComponent<MeshRenderer>();
            if (baseMr == null || objMr == null)
            {
                Debug.LogWarning($"[AutoPlacer] {baseObj.name}或{objToPlace.name}缺少MeshRenderer，无法进行bounds贴合，直接用AI建议坐标");
                UpdateRuleTransform(objToPlace.name, objToPlace.transform.position, objToPlace.transform.rotation.eulerAngles);
                return;
            }
            // 计算deltaY
            float baseTopY = baseMr.bounds.max.y;
            float objBottomY = objMr.bounds.min.y;
            float deltaY = baseTopY - objBottomY;
            // 修正Y
            objToPlace.transform.position += new Vector3(0, deltaY, 0);
            UpdateRuleTransform(objToPlace.name, objToPlace.transform.position, objToPlace.transform.rotation.eulerAngles);
            Debug.Log($"[AutoPlacer] 基准物体: {baseObj.name} 类型: {baseObj.GetType().Name} pos: {baseObj.transform.position}, AI建议pos: {rule.position}, 强制Y=0后: {aiOffset}, baseTopY: {baseTopY}, objBottomY: {objBottomY}, deltaY: {deltaY}, 最终pos: {objToPlace.transform.position}, AI原始Y: {aiY}");
        }
        else
        {
            objToPlace.transform.position = baseObj.transform.position;
            SetDefaultRotation(objToPlace);
            UpdateRuleTransform(objToPlace.name, objToPlace.transform.position, objToPlace.transform.rotation.eulerAngles);
        }
    }

    /// <summary>
    /// 获取物体包围盒
    /// </summary>
    Bounds GetBounds(GameObject obj)
    {
        var mr = obj.GetComponent<MeshRenderer>();
        if (mr != null) return mr.bounds;
        var mf = obj.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            return mf.sharedMesh.bounds;
        return new Bounds(Vector3.zero, Vector3.one);
    }

    /// <summary>
    /// 获取物体在主轴方向的宽度（用于横向分布）
    /// </summary>
    float GetObjectWidth(GameObject obj)
    {
        var bounds = GetBounds(obj);
        return bounds.size.x * obj.transform.localScale.x;
    }

    /// <summary>
    /// 计算物体在主轴方向的表面坐标
    /// </summary>
    float GetSurfacePosition(Vector3 pos, Bounds bounds, Vector3 axis, bool isTop)
    {
        int axisIdx = GetAxisIndex(axis);
        float center = bounds.center[axisIdx] * objScaleOnAxis(axis, bounds, pos);
        float half = (bounds.size[axisIdx] / 2) * objScaleOnAxis(axis, bounds, pos);
        return pos[axisIdx] + center + (isTop ? half : -half);
    }

    /// <summary>
    /// 获取主轴索引（0:X, 1:Y, 2:Z）
    /// </summary>
    int GetAxisIndex(Vector3 axis)
    {
        axis = axis.normalized;
        if (axis == Vector3.right) return 0;
        if (axis == Vector3.up) return 1;
        if (axis == Vector3.forward) return 2;
        return 1;
    }

    /// <summary>
    /// 获取物体在主轴方向的缩放
    /// </summary>
    float objScaleOnAxis(Vector3 axis, Bounds bounds, Vector3 pos)
    {
        var obj = FindObjectAtPosition(pos);
        if (obj == null) return 1f;
        int idx = GetAxisIndex(axis);
        return obj.transform.localScale[idx];
    }

    /// <summary>
    /// 通过位置查找物体
    /// </summary>
    GameObject FindObjectAtPosition(Vector3 pos)
    {
        foreach (Transform child in GameObject.Find("GameObjectRoot").transform)
        {
            if ((child.position - pos).sqrMagnitude < 0.01f)
                return child.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 应用规则表中的位置微调
    /// </summary>
    void ApplyOffset(GameObject obj)
    {
        // 不再使用offset，直接同步
        UpdateRuleTransform(obj.name, obj.transform.position, obj.transform.rotation.eulerAngles);
    }

    /// <summary>
    /// 获取物品规则
    /// </summary>
    ItemRule GetRule(string objName)
    {
        if (itemRules != null && itemRules.ContainsKey(objName))
            return itemRules[objName];
        // 支持模糊匹配（如关键字包含）
        if (itemRules != null)
        {
            foreach (var kv in itemRules)
            {
                if (!string.IsNullOrEmpty(kv.Value.keyword) && objName.Contains(kv.Value.keyword))
                    return kv.Value;
            }
        }
        return null;
    }

    private ItemRule ParseAIPlacementJson(string modelName, string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var aiData = JsonUtility.FromJson<AIPlacementData>(json);
            var rule = new ItemRule();
            rule.keyword = modelName;
            rule.parentKeyword = aiData.parent;
            // 健壮性校验 rotation
            if (aiData.rotation != null && aiData.rotation.Length == 3)
            {
                bool valid = true;
                foreach (var v in aiData.rotation)
                {
                    if (float.IsNaN(v) || float.IsInfinity(v) || Mathf.Abs(v) > 720)
                        valid = false;
                }
                if (valid)
                    rule.rotation = new Vector3(aiData.rotation[0], aiData.rotation[1], aiData.rotation[2]);
                else
                {
                    Debug.LogWarning($"[AutoPlacer] {modelName} AI返回的rotation数值异常，已用Vector3.zero替代: {string.Join(",", aiData.rotation)}");
                    rule.rotation = Vector3.zero;
                }
            }
            else
            {
                Debug.LogWarning($"[AutoPlacer] {modelName} AI返回的rotation字段无效，已用Vector3.zero替代");
                rule.rotation = Vector3.zero;
            }
            rule.layoutMode = ParseLayoutMode(aiData.layout_mode);
            rule.group = aiData.group;
            return rule;
        }
        catch (Exception ex)
        {
            Debug.LogError($"AI JSON解析失败: {ex.Message}\n{json}");
            return null;
        }
    }

    [Serializable]
    private class AIPlacementData
    {
        public string parent;
        public float[] rotation;
        public string layout_mode;
        public string group;
    }

    private LayoutMode ParseLayoutMode(string mode)
    {
        if (string.IsNullOrEmpty(mode)) return LayoutMode.Linear;
        switch (mode.ToLower())
        {
            case "grid": return LayoutMode.Grid;
            case "circle": return LayoutMode.Circle;
            default: return LayoutMode.Linear;
        }
    }

    [System.Serializable]
    private class AIPlacementLog
    {
        public string request;
        public string aiResult;
        public string error;
    }

    // 新增：同步ItemRule的position和rotation
    void UpdateRuleTransform(string objName, Vector3 pos, Vector3 euler)
    {
        var rule = GetRule(objName);
        if (rule != null)
        {
            rule.position = pos;
            rule.rotation = euler;
        }
    }
}
}
