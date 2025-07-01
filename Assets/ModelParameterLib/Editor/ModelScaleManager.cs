using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using ModelParameterLib.Module;

public class ModelScaleManager : EditorWindow
{
    private class ItemInfo
    {
        public string Category;
        public string Name;
        public Dictionary<string, float> RealSize;
        public float? Volume;
        public float? Scale;
    }

    private List<ItemInfo> allItems = new List<ItemInfo>();
    private List<ItemInfo> filteredItems = new List<ItemInfo>();
    private string filterCategory = "";
    private string filterName = "";
    private Vector2 scrollPos;
    private string newCategory = "";
    private string newName = "";
    private int calcModeIndex = 0;
    private string[] calcModes = new[] { "立方体", "圆柱体", "球体" };
    // 立方体
    private float length = 1.0f, width = 1.0f, height = 1.0f;
    // 圆柱体/球体
    private float diameter = 1.0f, cyHeight = 1.0f;
    // 体积/scale预览
    private float? previewVolume = null;
    private float? previewScale = null;
    private string aiStatus = "";
    private bool aiFilling = false;
    private bool showApiKey = false;
    private int? aiTypeIndex = null; // 0:立方体, 1:圆柱体, 2:球体
    private bool manualInputMode = true; // 新增：手动/AI补全模式切换
    private ModelAIFiller aiFiller; // 新增AI补全服务

    [MenuItem("Tools/ModelParameterLib/比例库管理")]
    public static void ShowWindow()
    {
        GetWindow<ModelScaleManager>("比例库管理");
    }

    private void OnEnable()
    {
        LoadData();
        ApplyFilter();
        ResetNewItemFields();
        aiFiller = new ModelAIFiller();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        // 新增：手动/AI补全模式切换开关
        manualInputMode = EditorGUILayout.ToggleLeft("手动输入模式", manualInputMode);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("DeepSeek API Key", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (showApiKey)
            ModelAIFiller.DeepSeekApiKey = EditorGUILayout.TextField(ModelAIFiller.DeepSeekApiKey);
        else
            ModelAIFiller.DeepSeekApiKey = EditorGUILayout.PasswordField(ModelAIFiller.DeepSeekApiKey);
        if (GUILayout.Button(showApiKey ? "隐藏" : "显示", GUILayout.Width(60)))
            showApiKey = !showApiKey;
        if (GUILayout.Button("保存API Key", GUILayout.Width(100)))
        {
            aiStatus = "API Key已保存";
        }
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(aiStatus))
        {
            if (aiStatus.Contains("API Key已保存"))
                EditorGUILayout.HelpBox("API Key已保存", MessageType.Info);
            else if (aiStatus.Contains("补全中"))
                EditorGUILayout.HelpBox(aiStatus, MessageType.None);
            else if (aiStatus.Contains("补全成功"))
                EditorGUILayout.HelpBox(aiStatus, MessageType.Info);
            else if (aiStatus.Contains("补全失败"))
                EditorGUILayout.HelpBox(aiStatus, MessageType.Error);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("添加新物品", EditorStyles.boldLabel);

        // 物品名在最上面，AI补全按钮在右侧
        EditorGUILayout.BeginHorizontal();
        newName = EditorGUILayout.TextField("物品名", newName ?? "");
        EditorGUI.BeginDisabledGroup(aiFilling);
        if (GUILayout.Button(aiFilling ? "补全中..." : "AI补全", GUILayout.Width(60)))
        {
            aiFilling = true;
            aiStatus = "补全中...";
            aiFiller.FillSizeAsync(newName, newCategory, ModelAIFiller.DeepSeekApiKey, (success, jsonResult, errorMsg) =>
            {
                aiFilling = false;
                if (success && !string.IsNullOrEmpty(jsonResult))
                {
                    try
                    {
                        var obj = Newtonsoft.Json.Linq.JObject.Parse(jsonResult);
                        newCategory = obj["category"]?.ToString() ?? newCategory;
                        // 判断类型
                        if (obj["length"] != null && obj["width"] != null && obj["height"] != null)
                        {
                            aiTypeIndex = 0; // 立方体
                            length = (float)obj["length"];
                            width = (float)obj["width"];
                            height = (float)obj["height"];
                        }
                        else if (obj["diameter"] != null && obj["height"] != null)
                        {
                            aiTypeIndex = 1; // 圆柱体
                            diameter = (float)obj["diameter"];
                            height = (float)obj["height"];
                        }
                        else if (obj["diameter"] != null)
                        {
                            aiTypeIndex = 2; // 球体
                            diameter = (float)obj["diameter"];
                        }
                        else
                        {
                            aiTypeIndex = null;
                        }
                        manualInputMode = false;
                        aiStatus = "补全成功";
                    }
                    catch (Exception ex)
                    {
                        aiStatus = "补全失败: AI返回格式异常 " + ex.Message;
                    }
                }
                else
                {
                    aiStatus = "补全失败: " + (errorMsg ?? "未知错误");
                }
                Repaint();
            });
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        // 其余输入框全部包裹
        EditorGUI.BeginDisabledGroup(!manualInputMode);

        // 类别输入框
        newCategory = EditorGUILayout.TextField("类别", newCategory);

        // 类型选择和尺寸输入框
        calcModeIndex = EditorGUILayout.Popup("尺寸类型", calcModeIndex, calcModes);
        string typeLabel = "未知";
        if (calcModeIndex == 0 && length > 0 && width > 0 && height > 0)
            typeLabel = "立方体/长方体";
        else if (calcModeIndex == 1 && diameter > 0 && height > 0)
            typeLabel = "圆柱体";
        else if (calcModeIndex == 2 && diameter > 0)
            typeLabel = "球体";
        EditorGUILayout.LabelField($"当前尺寸类型: {typeLabel}", EditorStyles.miniLabel);

        if (calcModeIndex == 0) // 立方体
        {
            length = EditorGUILayout.FloatField("长 (cm)", length);
            width = EditorGUILayout.FloatField("宽 (cm)", width);
            height = EditorGUILayout.FloatField("高 (cm)", height);
        }
        else if (calcModeIndex == 1) // 圆柱体
        {
            diameter = EditorGUILayout.FloatField("直径 (cm)", diameter);
            height = EditorGUILayout.FloatField("高 (cm)", height);
        }
        else if (calcModeIndex == 2) // 球体
        {
            diameter = EditorGUILayout.FloatField("直径 (cm)", diameter);
        }

        EditorGUI.EndDisabledGroup();

        previewVolume = (calcModeIndex == 0 && length > 0 && width > 0 && height > 0) ? (float?)Math.Round(length * width * height, 2) :
                        (calcModeIndex == 1 && diameter > 0 && height > 0) ? (float?)Math.Round(Math.PI * Math.Pow(diameter / 2f, 2) * height, 2) :
                        (calcModeIndex == 2 && diameter > 0) ? (float?)Math.Round((4f / 3f) * Math.PI * Math.Pow(diameter / 2f, 3), 2) : null;
        // scale = 最大边(cm)/100
        float maxEdge = 0f;
        if (calcModeIndex == 0 && length > 0 && width > 0 && height > 0)
            maxEdge = Mathf.Max(length, width, height);
        else if (calcModeIndex == 1 && diameter > 0 && height > 0)
            maxEdge = Mathf.Max(diameter, height);
        else if (calcModeIndex == 2 && diameter > 0)
            maxEdge = diameter;
        previewScale = maxEdge > 0.01f ? (float?)Math.Round(maxEdge / 100f, 4) : null;
        EditorGUILayout.LabelField($"体积(cm³): {(previewVolume.HasValue ? previewVolume.Value.ToString("F2") : "-")}");
        EditorGUILayout.LabelField($"scale(最大边m): {(previewScale.HasValue ? previewScale.Value.ToString("F4") : "-")}");
        if (GUILayout.Button("添加"))
        {
            var realSize = new Dictionary<string, float>();
            float? volume = null;
            float? scale = null;
            if (calcModeIndex == 0 && length > 0 && width > 0 && height > 0)
            {
                realSize["length"] = length;
                realSize["width"] = width;
                realSize["height"] = height;
                volume = (float)Math.Round(length * width * height, 2);
                maxEdge = Mathf.Max(length, width, height);
            }
            else if (calcModeIndex == 1 && diameter > 0 && height > 0)
            {
                realSize["diameter"] = diameter;
                realSize["height"] = height;
                volume = (float)Math.Round(Math.PI * Math.Pow(diameter / 2f, 2) * height, 2);
                maxEdge = Mathf.Max(diameter, height);
            }
            else if (calcModeIndex == 2 && diameter > 0)
            {
                realSize["diameter"] = diameter;
                volume = (float)Math.Round((4f / 3f) * Math.PI * Math.Pow(diameter / 2f, 3), 2);
                maxEdge = diameter;
            }
            if (maxEdge > 0.01f)
                scale = (float)Math.Round(maxEdge / 100f, 4);
            var item = new ItemInfo
            {
                Category = newCategory,
                Name = newName,
                RealSize = realSize,
                Volume = volume,
                Scale = scale
            };
            allItems.Add(item);
            SaveToJson();
            ApplyFilter();
            EditorUtility.DisplayDialog("添加成功", $"已添加 {newCategory} - {newName}", "OK");
            aiTypeIndex = null;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("筛选", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        string prevFilterCategory = filterCategory;
        string prevFilterName = filterName;
        filterCategory = EditorGUILayout.TextField("类别包含", filterCategory);
        filterName = EditorGUILayout.TextField("物品名包含", filterName);
        // 检测输入框变为空时自动刷新
        if ((prevFilterCategory != filterCategory && string.IsNullOrEmpty(filterCategory)) ||
            (prevFilterName != filterName && string.IsNullOrEmpty(filterName)))
        {
            ApplyFilter();
        }
        if (GUILayout.Button("筛选", GUILayout.Width(60)))
        {
            ApplyFilter();
        }
        if (GUILayout.Button("导出CSV", GUILayout.Width(80)))
        {
            ExportCSV();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"共 {filteredItems.Count} 条", EditorStyles.miniLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("类别", GUILayout.Width(120));
        EditorGUILayout.LabelField("物品名", GUILayout.Width(180));
        EditorGUILayout.LabelField("真实尺寸", GUILayout.Width(400));
        EditorGUILayout.LabelField("体积(cm³)", GUILayout.Width(100));
        EditorGUILayout.LabelField("scale(最大边m)", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        foreach (var item in filteredItems)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.Category, GUILayout.Width(120));
            EditorGUILayout.LabelField(item.Name, GUILayout.Width(180));
            EditorGUILayout.LabelField(FormatRealSize(item.RealSize), GUILayout.Width(400));
            EditorGUILayout.LabelField(item.Volume.HasValue ? item.Volume.Value.ToString("F2") : "null", GUILayout.Width(100));
            EditorGUILayout.LabelField(item.Scale.HasValue ? item.Scale.Value.ToString("F4") : "null", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void LoadData()
    {
        allItems.Clear();
        string jsonPath = "Assets/ModelParameterLib/ModelScaleDatabase.json";
        if (!File.Exists(jsonPath))
        {
            Debug.LogError("找不到 ModelScaleDatabase.json");
            return;
        }

        var root = JObject.Parse(File.ReadAllText(jsonPath));
        foreach (var cat in root)
        {
            string category = cat.Key;
            var items = cat.Value as JObject;
            if (items == null) continue;
            foreach (var obj in items)
            {
                string name = obj.Key;
                var itemObj = obj.Value as JObject;
                if (itemObj == null) continue;

                // 读取真实尺寸
                var realSize = new Dictionary<string, float>();
                var sizeObj = itemObj["真实尺寸"] as JObject;
                if (sizeObj != null)
                {
                    foreach (var prop in sizeObj)
                    {
                        if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
                            realSize[prop.Key] = prop.Value.Value<float>();
                    }
                }

                // 读取体积
                float? volume = null;
                if (itemObj["体积"] != null && 
                    (itemObj["体积"].Type == JTokenType.Float || itemObj["体积"].Type == JTokenType.Integer))
                {
                    volume = itemObj["体积"].Value<float>();
                }

                // 读取scale
                float? scale = null;
                if (itemObj["scale"] != null && 
                    (itemObj["scale"].Type == JTokenType.Float || itemObj["scale"].Type == JTokenType.Integer))
                {
                    scale = itemObj["scale"].Value<float>();
                }

                allItems.Add(new ItemInfo
                {
                    Category = category,
                    Name = name,
                    RealSize = realSize,
                    Volume = volume,
                    Scale = scale
                });
            }
        }
    }

    private void SaveToJson()
    {
        // 构建新结构
        var root = new JObject();
        foreach (var item in allItems)
        {
            if (!root.ContainsKey(item.Category))
                root[item.Category] = new JObject();
            var catObj = (JObject)root[item.Category];
            var itemObj = new JObject();
            var sizeObj = new JObject();
            foreach (var kv in item.RealSize)
                sizeObj[kv.Key] = kv.Value;
            itemObj["真实尺寸"] = sizeObj;
            if (item.Volume.HasValue)
                itemObj["体积"] = Math.Round(item.Volume.Value, 2);
            if (item.Scale.HasValue)
                itemObj["scale"] = Math.Round(item.Scale.Value, 4);
            catObj[item.Name] = itemObj;
        }
        File.WriteAllText("Assets/ModelParameterLib/ModelScaleDatabase.json", root.ToString());
        AssetDatabase.Refresh();
    }

    private void ApplyFilter()
    {
        filteredItems = allItems
            .Where(i =>
                (string.IsNullOrEmpty(filterCategory) || i.Category.Contains(filterCategory, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(filterName) || i.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase))
            )
            .ToList();
    }

    private void ExportCSV()
    {
        string path = EditorUtility.SaveFilePanel("导出CSV", "", "ModelVolumeExport.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();
        sb.AppendLine("类别,物品名,真实尺寸,体积(cm³),scale(最大边m)");
        foreach (var item in filteredItems)
        {
            sb.AppendLine($"\"{item.Category}\",\"{item.Name}\",\"{FormatRealSize(item.RealSize)}\",{(item.Volume.HasValue ? item.Volume.Value.ToString("F2") : "null")},{(item.Scale.HasValue ? item.Scale.Value.ToString("F4") : "null")}");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        EditorUtility.RevealInFinder(path);
    }

    private string FormatRealSize(Dictionary<string, float> size)
    {
        if (size == null || size.Count == 0) return "null";
        return string.Join("; ", size.Select(kv => $"{kv.Key}:{kv.Value}"));
    }

    private void ResetNewItemFields()
    {
        newCategory = "";
        newName = "";
        length = 0f;
        width = 0f;
        height = 0f;
        diameter = 0f;
        aiTypeIndex = null;
    }

    /// <summary>
    /// 多选弹窗，阻塞等待用户选择，返回选中索引，取消返回-1
    /// </summary>
    private int ShowMultiOptionDialog(string title, string[] options)
    {
        // 用自定义EditorWindow实现阻塞式选择
        return MultiOptionSelector.ShowDialog(title, options);
    }

    // 内部类：自定义弹窗
    private class MultiOptionSelector : EditorWindow
    {
        private static int result = -1;
        private static bool done = false;
        private static string[] _options;
        private static string _title;
        private int selectedIdx = 0;

        public static int ShowDialog(string title, string[] options)
        {
            result = -1;
            done = false;
            _options = options;
            _title = title;
            MultiOptionSelector window = ScriptableObject.CreateInstance<MultiOptionSelector>();
            window.titleContent = new GUIContent(title);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 180);
            window.ShowModal();
            return result;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
            EditorGUILayout.Space();
            selectedIdx = EditorGUILayout.Popup("请选择一个典型类别/尺寸：", selectedIdx, _options);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("确定"))
            {
                result = selectedIdx;
                done = true;
                Close();
            }
            if (GUILayout.Button("取消"))
            {
                result = -1;
                done = true;
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
