using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ModelParameterLib.Module;
using System;
using System.Collections;
using Newtonsoft.Json;
using ModelParameterLib.Data;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace ModelParameterLib.Core{

    /// <summary>
    /// 3D模型自动贴合与自适应摆放工具（支持AI参数、分组、多分布方式）
    /// </summary>
    
    public class AutoPlacer: MonoBehaviour
    {
        #region Inspector参数与按钮
        [ShowInInspector]
        [DictionaryDrawerSettings(KeyLabel = "模型名/关键字", ValueLabel = "AI规则")]
        public Dictionary<string, ItemRule> itemRules = new Dictionary<string, ItemRule>();

        [BoxGroup("基础参数")]
        [LabelText("网格分布间距 (X/Z)")]
        [MinValue(0.01f)]
        public Vector2 gridSpacing = new Vector2(0.1f, 0.2f);

        [BoxGroup("基础参数")]
        [LabelText("是否自动Y轴贴合父物体表面")]
        public bool autoYAlign = true;

        [Button("一键AI分析并填充规则")]
        public void AIAnalyzeButton()
        {
            AnalyzeAllWithAI();
        }

        [Button("自动摆放（应用规则）")]
        public void AutoArrangeButton()
        {
            if (hasArranged)
            {
                Debug.LogWarning("[AutoPlacer] 已自动摆放过，无需重复执行。");
                return;
            }
            AutoArrange();
            //hasArranged = true;
        }
        #endregion

        #region AI分析与规则填充
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
                // === 新增：自动拼接桌面(parent)分布优化提示 ===
                string parentName = "实验桌"; // 默认parent
                var parentRule = GetRule(modelName);
                if (parentRule != null && !string.IsNullOrEmpty(parentRule.parentKeyword))
                    parentName = parentRule.parentKeyword;
                var parentObj = GameObject.Find(parentName);
                string tableHint = "";
                if (parentObj != null)
                {
                    var parentBounds = GetWorldMeshBounds(parentObj);
                    tableHint = $"桌面中心为({parentBounds.center.x:F2},{parentBounds.center.y:F2},{parentBounds.center.z:F2})，长宽分别为{parentBounds.size.x:F2}和{parentBounds.size.z:F2}。请将所有物体均匀分布在桌面上，避免重叠，每个物体的X/Z应尽量分散，不要全部靠近中心。position字段以桌面中心为原点，X/Z范围在[-{parentBounds.size.x/2:F2}, +{parentBounds.size.x/2:F2}]和[-{parentBounds.size.z/2:F2}, +{parentBounds.size.z/2:F2}]内。";
                }
                // === 新增：mesh.bounds和底部顶点信息 ===
                string meshInfo = "";
                var meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var mesh = meshFilter.sharedMesh;
                    var verts = mesh.vertices;
                    float minY = mesh.bounds.min.y;
                    List<string> bottomVerts = new List<string>();
                    foreach (var v in verts)
                    {
                        if (Mathf.Abs(v.y - minY) < 1e-4f)
                            bottomVerts.Add($"[{v.x:F3},{v.y:F3},{v.z:F3}]");
                    }
                    meshInfo = $"mesh_bounds: min={mesh.bounds.min}, max={mesh.bounds.max}, center={mesh.bounds.center}, size={mesh.bounds.size}; bottom_vertices: [{string.Join(";", bottomVerts)}]。请根据mesh_bounds和bottom_vertices信息，判断物体真实占地面积和形状，给出合理的分布建议。position字段请确保物体底部完全落在桌面范围内，避免重叠和悬空。";
                }
                string fullSceneHint = sceneHint + $"\n{boundsInfo}\n{tableHint}\n{meshInfo}";
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
                        if (autoArrangeCoroutine != null)
                            EditorCoroutineUtility.StopCoroutine(autoArrangeCoroutine);
                        autoArrangeCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(WaitForItemRulesAndAutoArrange());
    #endif
                        Debug.Log("AI分析全部完成！（部分或全部使用缓存）");
                        if (itemRules.Count == GetPlacableModelCount())
                        {
                            Debug.Log("[AutoPlacer] AI分析完成后自动检测到itemRules数量齐全，自动开始自动摆放...");
                            AutoArrange();
                            hasArranged = true;
                        }
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
                            if (autoArrangeCoroutine != null)
                                EditorCoroutineUtility.StopCoroutine(autoArrangeCoroutine);
                            autoArrangeCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(WaitForItemRulesAndAutoArrange());
    #endif
                            Debug.Log("AI分析全部完成！");
                        }
                    }
                );
            }
        }
        #endregion

        #region 自动摆放主流程
        /// <summary>
        /// 自动批量摆放所有子物体（支持分组、分布方式、AI参数）
        /// 只根据itemRules内容进行实际摆放，不再调用AI
        /// </summary>
        public void AutoArrange()
        {
            Debug.Log("[AutoPlacer] 开始自动摆放");
            // 打印所有itemRules的key
            Debug.Log("[AutoPlacer] 当前itemRules key:");
            foreach (var rule in itemRules)
            {
                Debug.Log($"  key: {rule.Key}");
            }
            // 打印GameObjectRoot下所有子物体名称
            var root = GameObject.Find("GameObjectRoot");
            if (root == null)
            {
                Debug.LogError("[AutoPlacer] 未找到名为GameObjectRoot的GameObject！");
                return;
            }
            Debug.Log("[AutoPlacer] GameObjectRoot下所有子物体:");
            foreach (Transform child in root.transform)
            {
                Debug.Log($"  child: {child.gameObject.name}");
            }
            // 1. 获取GameObjectRoot下的所有子物体，剔除所有parentKeyword为自身的物体（如实验桌）
            var allObjs = new List<GameObject>();
            foreach (Transform child in root.transform)
            {
                var rule = GetRule(child.gameObject.name);
                if (rule == null) continue;
                if (rule.keyword == rule.parentKeyword) continue; // 跳过基准物体
                allObjs.Add(child.gameObject);
            }
            if (allObjs.Count == 0)
            {
                Debug.LogWarning("[AutoPlacer] 没有可自动摆放的物体（allObjs.Count==0），提前return");
                return;
            }
            // 2. 找到基准物体（如实验桌）
            GameObject parentObj = null;
            foreach (Transform child in root.transform)
            {
                var rule = GetRule(child.gameObject.name);
                if (rule != null && rule.keyword == rule.parentKeyword)
                {
                    parentObj = child.gameObject;
                    break;
                }
            }
            if (parentObj == null)
            {
                Debug.LogWarning("[AutoPlacer] 未找到基准物体（如实验桌），提前return");
                return;
            }
            var parentBounds = GetWorldMeshBounds(parentObj);
            float baseY = parentBounds.max.y;

            // === 新增：主模型优先排布到桌面中心 ===
            GameObject mainObj = null;
            foreach (var obj in allObjs)
            {
                var rule = GetRule(obj.name);
                if (rule != null && rule.isMainModel)
                {
                    mainObj = obj;
                    break;
                }
            }
            if (mainObj != null)
            {
                Vector3 center = parentBounds.center;
                mainObj.transform.position = new Vector3(center.x, baseY, center.z);
                if (autoYAlign) PlaceOnTop(parentObj, mainObj);
                allObjs.Remove(mainObj);
            }

            // 3. XZ平面自适应包围盒分布（防重叠，自动换行）
            float margin = 0.1f; // 桌面边缘留白
            float minSpacingX = gridSpacing.x;
            float minSpacingZ = gridSpacing.y;

            float tableMinX = parentBounds.min.x + margin;
            float tableMaxX = parentBounds.max.x - margin;
            float tableMinZ = parentBounds.min.z + margin;
            float tableMaxZ = parentBounds.max.z - margin;
            float curZ = tableMinZ;

            int i = 0;
            while (i < allObjs.Count)
            {
                float curX = tableMinX;
                float maxRowDepth = 0f;
                int rowStartI = i;
                // 一行内尽量多排物体，直到放不下
                while (i < allObjs.Count)
                {
                    var obj = allObjs[i];
                    var bounds = GetWorldMeshBounds(obj);
                    float objWidth = bounds.size.x;
                    float objDepth = bounds.size.z;
                    // 如果当前物体放下会超出桌面右边界，则换行
                    if (curX + objWidth > tableMaxX)
                    {
                        // 如果本行一个都没排，强制排下第一个
                        if (i == rowStartI)
                        {
                            float x = curX + objWidth / 2f;
                            float z = curZ + objDepth / 2f;
                            obj.transform.position = new Vector3(x, baseY, z);
                            if (autoYAlign) PlaceOnTop(parentObj, obj);
                            curX += objWidth + minSpacingX;
                            maxRowDepth = Mathf.Max(maxRowDepth, objDepth);
                            i++;
                        }
                        break;
                    }
                    // 正常排布
                    float x2 = curX + objWidth / 2f;
                    float z2 = curZ + objDepth / 2f;
                    obj.transform.position = new Vector3(x2, baseY, z2);
                    if (autoYAlign) PlaceOnTop(parentObj, obj);
                    curX += objWidth + minSpacingX;
                    maxRowDepth = Mathf.Max(maxRowDepth, objDepth);
                    i++;
                }
                // 换行
                curZ += maxRowDepth + minSpacingZ;
                // 如果超出桌面下边界，提前终止
                if (curZ > tableMaxZ)
                    break;
            }
            Debug.Log("[AutoPlacer] XZ网格分布+Y轴贴合自动摆放完成");
        }
        #endregion

        #region 贴合与检测
        /// <summary>
        /// 设置默认朝向（全部由代码自动设置，AI不参与rotation）
        /// 只需设置根对象transform.rotation，所有mesh子物体自动跟随
        /// </summary>
        void SetDefaultRotation(GameObject obj)
        {
            // 统一设置z轴负方向为前
            obj.transform.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);
            Debug.Log($"[AutoPlacer] {obj.name} rotation已设置为z轴负方向");
        }

        // 恢复 PlaceOnTop 用于Y轴贴合
        void PlaceOnTop(GameObject baseObj, GameObject objToPlace, bool ignoreAIOffset = false)
        {
            var rule = GetRule(objToPlace.name);
            SetDefaultRotation(objToPlace); // 所有模型都设置rotation
            if (rule != null && rule.keyword == rule.parentKeyword)
            {
                // 跳过position设置
                return;
            }
            var baseBounds = GetWorldMeshBounds(baseObj);
            float tableTopY = baseBounds.max.y;
            // 1. 先让模型中心X/Z不变，Y初步放在桌面表面
            Vector3 targetPos = new Vector3(objToPlace.transform.position.x, tableTopY, objToPlace.transform.position.z);
            objToPlace.transform.position = targetPos;

            // 2. 递归查找所有MeshFilter，收集所有底部顶点的世界坐标
            var meshFilters = objToPlace.GetComponentsInChildren<MeshFilter>();
            List<Vector3> allBottomVerts = new List<Vector3>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                var vertices = mesh.vertices;
                float minY = mesh.bounds.min.y;
                Matrix4x4 mat = mf.transform.localToWorldMatrix;
                foreach (var v in vertices)
                {
                    if (Mathf.Abs(v.y - minY) < 1e-4f)
                        allBottomVerts.Add(mat.MultiplyPoint3x4(v));
                }
            }
            if (allBottomVerts.Count == 0)
            {
                Debug.LogWarning($"[AutoPlacer] {objToPlace.name} 未检测到底部顶点，直接贴合中心");
                objToPlace.transform.position = targetPos;
                return;
            }
            // 3. 计算所有底部顶点的最小Y
            float minVertY = float.MaxValue;
            foreach (var v in allBottomVerts) minVertY = Mathf.Min(minVertY, v.y);
            float deltaY = tableTopY - minVertY;
            objToPlace.transform.position += new Vector3(0, deltaY, 0);
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 获取物体所有MeshFilter的网格bounds（世界坐标）
        /// </summary>
        Bounds GetWorldMeshBounds(GameObject obj)
        {
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);

            Bounds worldBounds = new Bounds();
            bool first = true;
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var meshBounds = mf.sharedMesh.bounds;
                // 8个顶点全部转换到世界坐标
                Vector3[] corners = new Vector3[8];
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                corners[0] = mf.transform.TransformPoint(new Vector3(min.x, min.y, min.z));
                corners[1] = mf.transform.TransformPoint(new Vector3(max.x, min.y, min.z));
                corners[2] = mf.transform.TransformPoint(new Vector3(min.x, max.y, min.z));
                corners[3] = mf.transform.TransformPoint(new Vector3(min.x, min.y, max.z));
                corners[4] = mf.transform.TransformPoint(new Vector3(max.x, max.y, min.z));
                corners[5] = mf.transform.TransformPoint(new Vector3(min.x, max.y, max.z));
                corners[6] = mf.transform.TransformPoint(new Vector3(max.x, min.y, max.z));
                corners[7] = mf.transform.TransformPoint(new Vector3(max.x, max.y, max.z));
                foreach (var v in corners)
                {
                    if (first)
                    {
                        worldBounds = new Bounds(v, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        worldBounds.Encapsulate(v);
                    }
                }
            }
            return worldBounds;
        }

        /// <summary>
        /// 支持假设位置和旋转的mesh bounds计算
        /// </summary>
        Bounds GetWorldMeshBounds(GameObject obj, Vector3 pos, Quaternion rot)
        {
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0) return new Bounds(pos, Vector3.zero);
            Bounds worldBounds = new Bounds();
            bool first = true;
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var meshBounds = mf.sharedMesh.bounds;
                Matrix4x4 mat = Matrix4x4.TRS(pos, rot, mf.transform.lossyScale);
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                Vector3[] corners = new Vector3[8];
                corners[0] = mat.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
                corners[1] = mat.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
                corners[2] = mat.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));
                corners[3] = mat.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
                corners[4] = mat.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
                corners[5] = mat.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));
                corners[6] = mat.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
                corners[7] = mat.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));
                foreach (var v in corners)
                {
                    if (first)
                    {
                        worldBounds = new Bounds(v, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        worldBounds.Encapsulate(v);
                    }
                }
            }
            return worldBounds;
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
                rule.isMainModel = aiData.is_main_model;
                return rule;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AI JSON解析失败: {ex.Message}\n{json}");
                return null;
            }
        }

//         private void OnEnable()
//         {
// #if UNITY_EDITOR
//             EditorApplication.hierarchyChanged += OnHierarchyChanged;
// #endif
//             hasArranged = false; // 每次启用都重置，确保无论如何都能执行一次
//             int placableCount = GetPlacableModelCount();
//             lastChildCount = placableCount;
//             // 无论数量是否变化，都强制检测一次
//             if (itemRules.Count < placableCount)
//             {
//                 Debug.Log("[AutoPlacer] OnEnable: 检测到itemRules数量不足，自动启动AI分析...");
//                 AnalyzeAllWithAI();
//             }
//             else if (itemRules.Count == placableCount)
//             {
//                 Debug.Log("[AutoPlacer] OnEnable: itemRules数量已齐，自动开始自动摆放...");
//                 AutoArrange();
//                 hasArranged = true;
//             }
//             else
//             {
//                 Debug.Log("[AutoPlacer] OnEnable: itemRules数量与可摆放模型数量不符，建议检查数据。");
//             }
//         }

        private int GetPlacableModelCount()
        {
            var root = GameObject.Find("GameObjectRoot");
            if (root == null) return 0;
            int count = 0;
            foreach (Transform child in root.transform)
            {
                var rule = GetRule(child.gameObject.name);
                if (rule != null && !string.IsNullOrEmpty(rule.parentKeyword) && rule.keyword == rule.parentKeyword)
                    continue; // 跳过基准物体
                count++;
            }
            return count;
        }

#if UNITY_EDITOR
        private int lastChildCount = -1;
        private EditorCoroutine autoArrangeCoroutine;

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return; // 只在编辑器下
            hasArranged = false;
            int placableCount = GetPlacableModelCount();
            if (itemRules.Count < placableCount)
            {
                Debug.Log("[AutoPlacer] OnValidate: 检测到itemRules数量不足，自动启动AI分析...");
                AnalyzeAllWithAI();
            }
            else if (itemRules.Count == placableCount)
            {
                Debug.Log("[AutoPlacer] OnValidate: itemRules数量已齐，自动开始自动摆放...");
                AutoArrange();
                hasArranged = true;
            }
        }

        private void OnHierarchyChanged()
        {
            var root = GameObject.Find("GameObjectRoot");
            if (root == null) return;
            int currentCount = GetPlacableModelCount();
            if (currentCount != lastChildCount)
            {
                Debug.Log($"[AutoPlacer] 检测到GameObjectRoot子物体数量变化: {lastChildCount} → {currentCount}，自动重新分析与摆放");
                lastChildCount = currentCount;
                hasArranged = false;
                itemRules.Clear();
                AnalyzeAllWithAI();
            }
        }

        // 新增：只统计可摆放物体的itemRules条目
        private int GetPlacableItemRuleCount()
        {
            int count = 0;
            foreach (var kv in itemRules)
            {
                var rule = kv.Value;
                if (rule != null && !(rule.keyword == rule.parentKeyword))
                    count++;
            }
            return count;
        }

        private IEnumerator WaitForItemRulesAndAutoArrange()
        {
            while (true)
            {
                int placableCount = GetPlacableModelCount();
                int placableRuleCount = GetPlacableItemRuleCount();
                Debug.Log($"[AutoPlacer][协程] itemRules.Count={itemRules.Count}, placableRuleCount={placableRuleCount}, placableCount={placableCount}, hasArranged={hasArranged}");
                if (placableRuleCount == placableCount && !hasArranged)
                {
                    Debug.Log("[AutoPlacer][协程] 检测到可摆放itemRules数量齐全，自动开始自动摆放...");
                    AutoArrange();
                    hasArranged = true;
                    break;
                }
                yield return new EditorWaitForSeconds(0.2f);
            }
        }
#endif
        #endregion

        private bool hasArranged = false;
    }

}
