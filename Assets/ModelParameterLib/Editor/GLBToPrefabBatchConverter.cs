using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using ModelParameterLib.Module;
using ModelParameterLib.Editor;
using ModelParameterLib.Data;
using Newtonsoft.Json.Linq;

namespace ModelParameterLib.Editor
{
    /// <summary>
    /// 🚀 GLB文件批量转换为Prefab的工具窗口 (结构重构版)
    /// 只负责UI、事件、主流程调度，所有业务逻辑委托给独立模块
    /// </summary>
    public class GLBToPrefabBatchConverter : EditorWindow
    {
        // 依赖的功能模块
        private CourseManager courseManager;
        private GLBFileScanner fileScanner;
        private GLBFileProcessor fileProcessor;
        private JsonScaleDataLoader jsonLoader;
        private ConversionStats stats;
        private ConversionReportGenerator reportGenerator;
        private ModelAIFiller aiFiller;
        //private bool aiFilling = false;
        //private string aiStatus = "";
        //private string aiResult = "";

        // UI相关字段
        private Vector2 scrollPosition;
        private bool isProcessing = false;
        private volatile bool cancelRequested = false;
        private float progressValue = 0f;
        private string currentProcessingFile = "";
        //private string aiItemName = "";
        //private string aiCategoryHint = "";
        //private float aiLength = 0, aiWidth = 0, aiHeight = 0, aiDiameter = 0;
        //private int? aiTypeIndex = null;

        private Dictionary<string, string> aiStatusPerFile = new Dictionary<string, string>(); // "pending", "success", "failed"
        private Dictionary<string, ScaleData> aiResultPerFile = new Dictionary<string, ScaleData>();

        //private string standardScaleDbPath = "Assets/ModelParameterLib/ModelScaleDatabase.json";

        // 附加值应用选项
        private bool applyCollider = true;
        private bool applyRigidbody = true;
        private bool applyGlassMaterial = true;
        private bool applyScaleFromLibrary = true;

        private bool showApiKey = false;
        private string apiKeyStatus = "";

        // 新增：整体缩放倍数，默认50
        private float scaleMultiplier = 50f;

        private HashSet<string> missingScaleFiles = new HashSet<string>(); // 记录比例库缺失的文件名
        
        [MenuItem("Tools/ModelParameterLib/GLB转预制件 &g")]
        public static void ShowWindow()
        {
            var window = GetWindow<GLBToPrefabBatchConverter>("GLB预制件转换器");
            window.Show();
            window.InitializeModules();
            window.RefreshAll();
        }

        private void InitializeModules()
        {
            courseManager ??= new CourseManager();
            fileScanner ??= new GLBFileScanner();
            fileProcessor ??= new GLBFileProcessor();
            jsonLoader ??= new JsonScaleDataLoader();
            stats ??= new ConversionStats();
            reportGenerator ??= new ConversionReportGenerator();
            aiFiller ??= new ModelAIFiller();
        }

        private void RefreshAll()
        {
            courseManager.ScanForCourseFolders();
            if (courseManager.availableCourseFolders.Count > 0)
            {
                courseManager.selectedCourseIndex = 0;
                courseManager.selectedCourseFolder = courseManager.availableCourseFolders[0];
                courseManager.UpdatePathsForSelectedCourse();
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("DeepSeek API Key");
            EditorGUILayout.BeginHorizontal();
            if (showApiKey)
                ModelAIFiller.DeepSeekApiKey = EditorGUILayout.TextField(ModelAIFiller.DeepSeekApiKey);
            else
                ModelAIFiller.DeepSeekApiKey = EditorGUILayout.PasswordField(ModelAIFiller.DeepSeekApiKey);
            if (GUILayout.Button(showApiKey ? "隐藏" : "显示", GUILayout.Width(60)))
                showApiKey = !showApiKey;
            if (GUILayout.Button("保存API Key", GUILayout.Width(100)))
            {
                apiKeyStatus = "API Key已保存";
            }
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(apiKeyStatus))
            {
                EditorGUILayout.HelpBox(apiKeyStatus, MessageType.Info);
            }
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
            GUILayout.Label("GLB批量转Prefab工具", EditorStyles.largeLabel);
            GUILayout.Label("—— 只负责UI和流程调度，所有业务操作委托功能类", EditorStyles.miniLabel);
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawCourseSelectionPanel();
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawPathDisplayPanel();
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawFileManagementPanel();
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSettingsPanel();
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProgressAndControlPanel();
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawStatisticsPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }
        
        private void DrawCourseSelectionPanel()
        {
            GUILayout.Label("课程选择", EditorStyles.boldLabel);
            if (courseManager.availableCourseFolders.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现任何课程文件夹，请检查Assets目录结构。", MessageType.Warning);
                if (GUILayout.Button("刷新课程列表"))
                {
                    courseManager.ScanForCourseFolders();
                }
                return;
            }
            var displayNames = courseManager.availableCourseFolders.Select(path => Path.GetFileName(path)).ToArray();
            int newIndex = EditorGUILayout.Popup("课程：", courseManager.selectedCourseIndex, displayNames);
            if (newIndex != courseManager.selectedCourseIndex)
            {
                courseManager.SetSelectedCourse(newIndex);
                LoadAllScaleLibraries();
                aiStatusPerFile.Clear();
                aiResultPerFile.Clear();
            }
            if (GUILayout.Button("刷新课程列表"))
            {
                courseManager.ScanForCourseFolders();
            }
            GUILayout.Space(5);
        }
        
        private void DrawPathDisplayPanel()
        {
            GUILayout.Label("路径信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("源文件夹:", courseManager.sourceFolder);
            EditorGUILayout.LabelField("输出文件夹:", courseManager.outputFolder);
            GUILayout.Space(5);
        }
        
        private void DrawFileManagementPanel()
        {
            if (fileScanner == null || courseManager == null)
            {
                Debug.LogError("fileScanner 或 courseManager 未初始化！");
                EditorGUILayout.HelpBox("内部错误：fileScanner 或 courseManager 未初始化。", MessageType.Error);
                return;
            }
            GUILayout.Label("GLB文件列表", EditorStyles.boldLabel);
            var glbFiles = fileScanner.ScanForGLBFiles(courseManager.sourceFolder, courseManager.outputFolder);
            if (glbFiles.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现GLB文件。", MessageType.Info);
                if (GUILayout.Button("重新扫描GLB文件"))
                {
                    LoadAllScaleLibraries();
                    fileScanner?.ScanForGLBFiles(courseManager.sourceFolder, courseManager.outputFolder);
                    aiStatusPerFile.Clear();
                    aiResultPerFile.Clear();
                    Repaint();
                }
                return;
            }
            EditorGUILayout.LabelField($"共 {glbFiles.Count} 个GLB文件");
            GUIStyle leftAlignStyle = new GUIStyle(EditorStyles.label);
            leftAlignStyle.alignment = TextAnchor.MiddleLeft;
            GUILayout.BeginVertical("box");
            // 表头
            GUILayout.BeginHorizontal();
            GUILayout.Label("文件名", leftAlignStyle);
            GUILayout.Label("大小", leftAlignStyle);
            GUILayout.Label("状态", leftAlignStyle);
            GUILayout.Label("GLB scale", leftAlignStyle);
            GUILayout.EndHorizontal();
            foreach (var file in glbFiles)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(file.fileName, leftAlignStyle);
                GUILayout.Label($"{file.fileSize / 1024} KB", leftAlignStyle);
                GUIStyle statusStyle = file.hasPrefab ? leftAlignStyle : leftAlignStyle;
                Color origColor = GUI.color;
                if (file.hasPrefab) GUI.color = Color.green;
                else GUI.color = Color.red;
                GUILayout.Label(file.hasPrefab ? "已生成Prefab" : "未生成", statusStyle);
                GUI.color = origColor;
                // 优先显示比例库scale
                string pureName = System.Text.RegularExpressions.Regex.Replace(file.fileNameWithoutExtension, @"\d+$", "");
                var (scaleData, sourceDb) = jsonLoader.GetScaleDataForModelAll(pureName);
                string scaleStr = "无";
                string volumeStr = "无";
                string categoryStr = "未知";
                if (scaleData != null)
                {
                    if (scaleData.ScaleValue.HasValue)
                        scaleStr = scaleData.ScaleValue.Value.ToString("F4");
                    else if (scaleData.Scale != null)
                        scaleStr = scaleData.Scale.ToString();
                    if (scaleData.Volume.HasValue)
                        volumeStr = scaleData.Volume.Value.ToString("F2");
                    if (!string.IsNullOrEmpty(scaleData.Category))
                        categoryStr = scaleData.Category;
                }
                else if (file.jsonScaleData != null && (file.jsonScaleData.ScaleValue.HasValue || file.jsonScaleData.Scale != Vector3.one))
                {
                    if (file.jsonScaleData.ScaleValue.HasValue)
                        scaleStr = file.jsonScaleData.ScaleValue.Value.ToString("F4");
                    else
                        scaleStr = file.jsonScaleData.Scale.ToString();
                }
                GUILayout.Label($"GLB scale: {scaleStr}", leftAlignStyle);
                GUILayout.EndHorizontal();

                // 1. 先查比例库，查到就显示并设为success
                if (scaleData != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    GUILayout.Label($"比例库匹配: scale={scaleStr} 体积={volumeStr} (类别: {categoryStr}, 来源: {sourceDb})", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                    aiStatusPerFile[file.fileNameWithoutExtension] = "success";
                }
                else
                {
                    // 2. 只有没查到比例库，才AI补全
                    if (!aiStatusPerFile.ContainsKey(file.fileNameWithoutExtension) || aiStatusPerFile[file.fileNameWithoutExtension] == "failed")
                    {
                        aiStatusPerFile[file.fileNameWithoutExtension] = "pending";
                        GUILayout.Label("无比例库数据，自动AI补全中...");
                        aiFiller.FillSizeAsync(file.fileNameWithoutExtension, "", GetApiKey(), (success, jsonResult, errorMsg) =>
                        {
                            if (success && !string.IsNullOrEmpty(jsonResult))
                            {
                                var obj = JObject.Parse(jsonResult);
                                var newScaleData = new ScaleData
                                {
                                    RealWorldSizeDict = new Dictionary<string, float>()
                                };
                                float? length = null, width = null, height = null, diameter = null;
                                foreach (var prop in obj.Properties())
                                {
                                    if (prop.Name != "category" && (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer))
                                        newScaleData.RealWorldSizeDict[prop.Name] = prop.Value.Value<float>();
                                    if (prop.Name == "length") length = prop.Value.Value<float>();
                                    if (prop.Name == "width") width = prop.Value.Value<float>();
                                    if (prop.Name == "height") height = prop.Value.Value<float>();
                                    if (prop.Name == "diameter") diameter = prop.Value.Value<float>();
                                }
                                // 计算最大边
                                float maxEdge = 0f;
                                if (length.HasValue || width.HasValue || height.HasValue || diameter.HasValue) {
                                    float l = length ?? 0f;
                                    float w = width ?? 0f;
                                    float h = height ?? 0f;
                                    float d = diameter ?? 0f;
                                    maxEdge = Mathf.Max(l, w, h, d);
                                }
                                // 2. scale = 最大边(cm)/100
                                float? scale = null;
                                if (maxEdge > 0.01f) scale = (float)Math.Round(maxEdge / 100f, 4);
                                // 自动体积/scale计算
                                float? volume = null;
                                if (length.HasValue && width.HasValue && height.HasValue)
                                {
                                    volume = (float)Math.Round(length.Value * width.Value * height.Value, 2);
                                }
                                else if (diameter.HasValue && height.HasValue)
                                {
                                    volume = (float)Math.Round(Math.PI * Math.Pow(diameter.Value / 2f, 2) * height.Value, 2);
                                }
                                else if (diameter.HasValue)
                                {
                                    volume = (float)Math.Round((4f / 3f) * Math.PI * Math.Pow(diameter.Value / 2f, 3), 2);
                                }
                                newScaleData.Volume = volume;
                                newScaleData.ScaleValue = scale;
                                newScaleData.Category = obj["category"]?.ToString() ?? "未分类";
                                // 自动去除itemName末尾数字
                                string pureName2 = System.Text.RegularExpressions.Regex.Replace(file.fileNameWithoutExtension, @"\d+$", "");
                                Debug.Log($"[GLBToPrefabBatchConverter] AI补全后真实尺寸: {JsonConvert.SerializeObject(newScaleData.RealWorldSizeDict)}，写入key: {pureName2}");
                                Debug.Log($"[ScaleDataWriter] 写入 {newScaleData.Category}-{pureName2} 真实尺寸: {JsonConvert.SerializeObject(newScaleData.RealWorldSizeDict)}");
                                ScaleDataWriter.AddOrUpdateScaleData(GetScaleDbPath(), newScaleData.Category, pureName2, newScaleData);
                                // AI补全写入后，自动reload比例库并刷新UI
                                LoadAllScaleLibraries();
                                aiStatusPerFile[file.fileNameWithoutExtension] = "success";
                                aiResultPerFile[file.fileNameWithoutExtension] = newScaleData;
                                Repaint();
                        }
                        else
                        {
                                Debug.LogError("AI补全失败: " + errorMsg);
                                aiStatusPerFile[file.fileNameWithoutExtension] = "failed";
                                Repaint();
                            }
                        });
                    }
                    else if (aiStatusPerFile[file.fileNameWithoutExtension] == "pending")
                    {
                        GUILayout.Label("AI补全中...");
                    }
                    else if (aiStatusPerFile[file.fileNameWithoutExtension] == "failed")
                    {
                        GUILayout.Label("AI补全失败，请检查网络或API Key");
                        if (GUILayout.Button("重试AI补全", GUILayout.Width(100)))
                        {
                            aiStatusPerFile[file.fileNameWithoutExtension] = "pending";
                            aiFiller.FillSizeAsync(file.fileNameWithoutExtension, "", GetApiKey(), (success, jsonResult, errorMsg) =>
                            {
                                if (success && !string.IsNullOrEmpty(jsonResult))
                                {
                                    var obj = JObject.Parse(jsonResult);
                                    var newScaleData = new ScaleData
                                    {
                                        RealWorldSizeDict = new Dictionary<string, float>()
                                    };
                                    float? length = null, width = null, height = null, diameter = null;
                                    foreach (var prop in obj.Properties())
                                    {
                                        if (prop.Name != "category" && (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer))
                                            newScaleData.RealWorldSizeDict[prop.Name] = prop.Value.Value<float>();
                                        if (prop.Name == "length") length = prop.Value.Value<float>();
                                        if (prop.Name == "width") width = prop.Value.Value<float>();
                                        if (prop.Name == "height") height = prop.Value.Value<float>();
                                        if (prop.Name == "diameter") diameter = prop.Value.Value<float>();
                                    }
                                    float? volume = null;
                                    if (length.HasValue && width.HasValue && height.HasValue)
                                    {
                                        volume = (float)Math.Round(length.Value * width.Value * height.Value, 2);
                                    }
                                    else if (diameter.HasValue && height.HasValue)
                                    {
                                        volume = (float)Math.Round(Math.PI * Math.Pow(diameter.Value / 2f, 2) * height.Value, 2);
                                    }
                                    else if (diameter.HasValue)
                                    {
                                        volume = (float)Math.Round((4f / 3f) * Math.PI * Math.Pow(diameter.Value / 2f, 3), 2);
                                    }
                                    float maxEdge = 0f;
                                    if (length.HasValue || width.HasValue || height.HasValue || diameter.HasValue) {
                                        float l = length ?? 0f;
                                        float w = width ?? 0f;
                                        float h = height ?? 0f;
                                        float d = diameter ?? 0f;
                                        maxEdge = Mathf.Max(l, w, h, d);
                                    }
                                    float? scale = null;
                                    if (maxEdge > 0.01f) scale = (float)Math.Round(maxEdge / 100f, 4);
                                    newScaleData.Volume = volume;
                                    newScaleData.ScaleValue = scale;
                                    newScaleData.Category = obj["category"]?.ToString() ?? "未分类";
                                    string pureName2 = System.Text.RegularExpressions.Regex.Replace(file.fileNameWithoutExtension, @"\d+$", "");
                                    Debug.Log($"[GLBToPrefabBatchConverter] AI补全后真实尺寸: {JsonConvert.SerializeObject(newScaleData.RealWorldSizeDict)}，写入key: {pureName2}");
                                    Debug.Log($"[ScaleDataWriter] 写入 {newScaleData.Category}-{pureName2} 真实尺寸: {JsonConvert.SerializeObject(newScaleData.RealWorldSizeDict)}");
                                    ScaleDataWriter.AddOrUpdateScaleData(GetScaleDbPath(), newScaleData.Category, pureName2, newScaleData);
                                    LoadAllScaleLibraries();
                                    aiStatusPerFile[file.fileNameWithoutExtension] = "success";
                                    aiResultPerFile[file.fileNameWithoutExtension] = newScaleData;
                                    Repaint();
                                }
                                else
                                {
                                    Debug.LogError("AI补全失败: " + errorMsg);
                                    aiStatusPerFile[file.fileNameWithoutExtension] = "failed";
                                    Repaint();
                                }
                            });
                        }
                    }
                }
            }
            GUILayout.EndVertical();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新扫描GLB文件", GUILayout.ExpandWidth(true)))
            {
                LoadAllScaleLibraries();
                fileScanner?.ScanForGLBFiles(courseManager.sourceFolder, courseManager.outputFolder);
                aiStatusPerFile.Clear();
                aiResultPerFile.Clear();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawSettingsPanel()
        {
            GUILayout.Label("转换设置", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // 新增：整体缩放倍数输入
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Prefab和场景实例缩放倍数", GUILayout.Width(180));
            scaleMultiplier = EditorGUILayout.FloatField(scaleMultiplier, GUILayout.Width(80));
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.01f, 1000f);
            GUILayout.Label("倍 (Prefab和场景实例均应用)", GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            float totalWidth = position.width - 40; // 适当减去边距
            float toggleMinWidth = 220f;
            int togglesPerRow = Mathf.Max(1, Mathf.FloorToInt(totalWidth / toggleMinWidth));
            int toggleCount = 4;
            int drawn = 0;

            bool[] values = { applyCollider, applyRigidbody, applyGlassMaterial, applyScaleFromLibrary };
            string[] labels = {
                "为Prefab添加MeshCollider组件",
                "为Prefab添加Rigidbody组件",
                "自动识别并应用玻璃材质（需AI分析）",
                "应用比例库中的缩放数据（推荐开启）"
            };

            EditorGUILayout.BeginVertical();
            while (drawn < toggleCount)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < togglesPerRow && drawn < toggleCount; i++, drawn++)
                {
                    switch (drawn)
                    {
                        case 0: applyCollider = EditorGUILayout.ToggleLeft(labels[0], applyCollider, GUILayout.MinWidth(toggleMinWidth)); break;
                        case 1: applyRigidbody = EditorGUILayout.ToggleLeft(labels[1], applyRigidbody, GUILayout.MinWidth(toggleMinWidth)); break;
                        case 2: applyGlassMaterial = EditorGUILayout.ToggleLeft(labels[2], applyGlassMaterial, GUILayout.MinWidth(toggleMinWidth)); break;
                        case 3: applyScaleFromLibrary = EditorGUILayout.ToggleLeft(labels[3], applyScaleFromLibrary, GUILayout.MinWidth(toggleMinWidth)); break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        private void DrawProgressAndControlPanel()
        {
            GUILayout.Label("批量转换控制", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isProcessing);
            if (GUILayout.Button("开始批量转换", GUILayout.ExpandWidth(true)))
            {
                var glbFiles = fileScanner.ScanForGLBFiles(courseManager.sourceFolder, courseManager.outputFolder);
                var selectedFiles = glbFiles;
                if (selectedFiles.Count > 0)
                {
                    StartConversion(selectedFiles);
                }
            }
            EditorGUI.EndDisabledGroup();
            if (isProcessing)
            {
                if (GUILayout.Button("停止", GUILayout.ExpandWidth(true)))
                {
                    StopConversion();
                }
            }
            EditorGUILayout.EndHorizontal();
            if (isProcessing)
            {
                EditorGUILayout.LabelField("进度：");
                Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(rect, progressValue, $"{progressValue * 100:F0}%");
                EditorGUILayout.LabelField("当前文件：", currentProcessingFile);
                GUILayout.Space(5);
            }
            GUILayout.Space(5);
        }

        private void DrawStatisticsPanel()
        {
            GUILayout.Label("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"已处理文件数：{stats.processedFiles}\n" +
                $"成功转换数：{stats.successfulConversions}\n" +
                $"失败转换数：{stats.failedConversions}\n" +
                $"总用时：{stats.totalProcessingTime:F2} 秒",
                MessageType.Info
            );
            GUILayout.Space(5);
        }

        // 只负责调度，所有业务操作委托功能类
        private async void StartConversion(List<GLBFileInfo> selectedFiles)
        {
            if (isProcessing) return;
            isProcessing = true;
            cancelRequested = false;
            // 自动创建目标Prefabs文件夹
            if (!string.IsNullOrEmpty(courseManager.outputFolder) && !System.IO.Directory.Exists(courseManager.outputFolder))
            {
                System.IO.Directory.CreateDirectory(courseManager.outputFolder);
            }
            stats.Reset();
            stats.StartTiming();
            missingScaleFiles.Clear();
            var options = new GLBFileProcessor.GLBProcessOptions
            {
                applyCollider = applyCollider,
                applyRigidbody = applyRigidbody,
                applyGlassMaterial = applyGlassMaterial,
                applyScaleFromLibrary = applyScaleFromLibrary,
                scaleMultiplier = scaleMultiplier // 关键：Prefab本体缩放倍数
            };
            try
            {
                int total = selectedFiles.Count;
                List<string> skippedFiles = new List<string>(); // 新增: 汇总已转换模型
                for (int i = 0; i < total; i++)
                {
                    if (cancelRequested) break;
                    var file = selectedFiles[i];
                    if (file.hasPrefab)
                    {
                        skippedFiles.Add(file.fileName); // 只记录，不弹窗
                        continue;
                    }
                    currentProcessingFile = file.fileName;
                    progressValue = (float)(i + 1) / total;
                    Repaint();
                    // 查比例库，忽略物品名末尾数字
                    string pureName = System.Text.RegularExpressions.Regex.Replace(file.fileNameWithoutExtension, @"\d+$", "");

                    var (scaleData, _, _, _) = jsonLoader.GetScaleDataForModelAllFuzzy(pureName, 0.75f);
                    if (scaleData == null)
                    {
                        Debug.LogError($"[比例库缺失] {file.fileName} 未找到比例数据，需先补全！");
                        missingScaleFiles.Add(file.fileName);
                        stats.processedFiles++;
                        stats.failedConversions++;
                        continue;
                    }
                    file.jsonScaleData = scaleData;
                    bool success = await fileProcessor.CreatePrefabFromGLB(file, jsonLoader, options, courseManager.outputFolder);
                    stats.processedFiles++;
                    if (success) stats.successfulConversions++;
                    else stats.failedConversions++;

                    // 新增：自动实例化Prefab到场景，并设置父物体为GameObjectRoot
                    string prefabPath = Path.Combine(courseManager.outputFolder, file.fileNameWithoutExtension + ".prefab").Replace('\\', '/');
                    var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefabAsset != null)
                    {
                        // 查找或创建GameObjectRoot
                        GameObject root = GameObject.Find("GameObjectRoot");
                        if (root == null)
                        {
                            // 优先从预制体加载
                            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MCP/GameObjectRoot.prefab");
                            if (rootPrefab != null)
                            {
                                root = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
                            }
                            else
                            {
                                root = new GameObject("GameObjectRoot");
                            }
                        }
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                        if (instance != null)
                        {
                            instance.transform.SetParent(root.transform, false);
                            // 不再额外乘以scaleMultiplier，Prefab本体已缩放
                            // 🔥 新增：为场景中的实例添加Clickable uiPrefab赋值
                            AssignClickableUIPrefab(instance);
                        }
                    }
                }
                // 新增: 批量弹窗提示（仅当全部都已转换时才弹）
                if (skippedFiles.Count > 0 && skippedFiles.Count == selectedFiles.Count)
                {
                    string msg = "以下模型已完成转换，不可重复转换：\n" + string.Join("\n", skippedFiles);
                    EditorUtility.DisplayDialog("转换提示", msg, "确定");
                }
            }
            finally
            {
                isProcessing = false;
                cancelRequested = false;
                currentProcessingFile = string.Empty;
                progressValue = 1f;
                // 修正：批量转换后重新扫描GLB文件列表，确保hasPrefab状态和报告内容准确
                var refreshedFiles = fileScanner.ScanForGLBFiles(courseManager.sourceFolder, courseManager.outputFolder);
                reportGenerator.GenerateReport(stats, refreshedFiles, courseManager.selectedCourseFolder);
                Repaint();
            }
        }
        private void StopConversion()
        {
            cancelRequested = true;
            currentProcessingFile = "已停止";
        }

        // 课程切换/窗口打开时清空AI状态缓存
        private void OnEnable()
        {
            InitializeModules();
            aiStatusPerFile.Clear();
            aiResultPerFile.Clear();
            LoadAllScaleLibraries();
        }

        // 工具方法：获取API Key
        private static string GetApiKey()
        {
            return EditorPrefs.GetString("DeepSeekApiKey", "");
        }
        // 工具方法：获取比例库路径
        private static string GetScaleDbPath()
        {
            return "Assets/ModelParameterLib/ModelScaleDatabase.json";
        }

        private void LoadAllScaleLibraries()
        {
            var paths = new List<string>();
            // 课程专属比例库
            if (!string.IsNullOrEmpty(courseManager.selectedCourseFolder))
                paths.Add(JsonScaleDataLoader.GetScaleDbPathForCourse(courseManager.selectedCourseFolder));
            // 标准比例库
            paths.Add(GetScaleDbPath());
            jsonLoader.LoadAllScaleData(paths.ToArray());
        }

        private static async Task<ScaleData> GetOrFillScaleDataAsync(
            string fileNameWithoutExtension,
            JsonScaleDataLoader jsonLoader,
            string scaleDbPath,
            string deepSeekApiKey,
            ModelAIFiller aiFiller)
        {
            // 1. 优先查比例库
            var (scaleData, _, _, _) = jsonLoader.GetScaleDataForModelAllFuzzy(fileNameWithoutExtension, 0.75f);
            if (scaleData != null) return scaleData; // 有数据直接返回

            // 2. 只有比例库没有数据时才调用AI分析
            var tcs = new TaskCompletionSource<bool>();
            aiFiller.FillSizeAsync(fileNameWithoutExtension, "", deepSeekApiKey, (success, jsonResult, errorMsg) =>
            {
                if (success && !string.IsNullOrEmpty(jsonResult))
                {
                    var obj = JObject.Parse(jsonResult);
                    var newScaleData = new ScaleData
                    {
                        RealWorldSizeDict = new Dictionary<string, float>()
                    };
                    float? length = null, width = null, height = null, diameter = null;
                    foreach (var prop in obj.Properties())
                    {
                        if (prop.Name != "category" && (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer))
                            newScaleData.RealWorldSizeDict[prop.Name] = prop.Value.Value<float>();
                        if (prop.Name == "length") length = prop.Value.Value<float>();
                        if (prop.Name == "width") width = prop.Value.Value<float>();
                        if (prop.Name == "height") height = prop.Value.Value<float>();
                        if (prop.Name == "diameter") diameter = prop.Value.Value<float>();
                    }
                    float maxEdge = 0f;
                    if (length.HasValue || width.HasValue || height.HasValue || diameter.HasValue)
                    {
                        float l = length ?? 0f;
                        float w = width ?? 0f;
                        float h = height ?? 0f;
                        float d = diameter ?? 0f;
                        maxEdge = Mathf.Max(l, w, h, d);
                    }
                    float? scale = null;
                    if (maxEdge > 0.01f) scale = (float)Math.Round(maxEdge / 100f, 4);
                    float? volume = null;
                    if (length.HasValue && width.HasValue && height.HasValue)
                        volume = (float)Math.Round(length.Value * width.Value * height.Value, 2);
                    else if (diameter.HasValue && height.HasValue)
                        volume = (float)Math.Round(Math.PI * Math.Pow(diameter.Value / 2f, 2) * height.Value, 2);
                    else if (diameter.HasValue)
                        volume = (float)Math.Round((4f / 3f) * Math.PI * Math.Pow(diameter.Value / 2f, 3), 2);
                    newScaleData.Volume = volume;
                    newScaleData.ScaleValue = scale;
                    newScaleData.Category = obj["category"]?.ToString() ?? "未分类";
                    string pureName2 = System.Text.RegularExpressions.Regex.Replace(fileNameWithoutExtension, @"\d+$", "");
                    ScaleDataWriter.AddOrUpdateScaleData(scaleDbPath, newScaleData.Category, pureName2, newScaleData);
                    tcs.SetResult(true);
                }
                else
                {
                    Debug.LogError("AI补全失败: " + errorMsg);
                    tcs.SetResult(false);
                }
            });
            await tcs.Task;
            // 3. AI补全后reload比例库再查一次
            jsonLoader.LoadAllScaleData(new[] { scaleDbPath });
            (scaleData, _, _, _) = jsonLoader.GetScaleDataForModelAllFuzzy(fileNameWithoutExtension, 0.75f);
            return scaleData;
        }

        public static async Task<object> RunBatchConvert(string courseRootFolder, string deepSeekApiKey = null, float scaleMultiplier = 50f)
        {
            if (string.IsNullOrEmpty(deepSeekApiKey))
                deepSeekApiKey = GetApiKey();
            var modelsFolder = Path.Combine(courseRootFolder, "Models");
            var outputFolder = Path.Combine(courseRootFolder, "Prefabs");
            var courseManager = new CourseManager();
            var fileScanner = new GLBFileScanner();
            var fileProcessor = new GLBFileProcessor();
            var jsonLoader = new JsonScaleDataLoader();
            var aiFiller = new ModelAIFiller();
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            jsonLoader.LoadAllScaleData(new[] { GetScaleDbPath() });

            var glbFiles = fileScanner.ScanForGLBFiles(modelsFolder, outputFolder);
            var options = new GLBFileProcessor.GLBProcessOptions
            {
                applyCollider = true,
                applyRigidbody = true,
                applyGlassMaterial = true,
                applyScaleFromLibrary = true,
                scaleMultiplier = scaleMultiplier // 关键：Prefab本体缩放倍数
            };
            foreach (var file in glbFiles)
            {
                // 跳过：已生成Prefab或场景中已存在同名实例
                string prefabPath = Path.Combine(outputFolder, file.fileNameWithoutExtension + ".prefab").Replace('\\', '/');
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var sceneInstance = GameObject.Find(file.fileNameWithoutExtension);
                if (prefabAsset != null || sceneInstance != null)
                {
                    // 已生成Prefab或场景中已存在实例，跳过
                    continue;
                }
                // 只在比例库查不到时才AI分析
                var scaleData = await GetOrFillScaleDataAsync(
                    file.fileNameWithoutExtension, jsonLoader, GetScaleDbPath(), deepSeekApiKey, aiFiller);
                if (scaleData == null)
                {
                    Debug.LogError($"AI补全后仍无比例数据: {file.fileNameWithoutExtension}");
                    continue;
                }
                file.jsonScaleData = scaleData;
                await fileProcessor.CreatePrefabFromGLB(file, jsonLoader, options, outputFolder);

                // 新增：自动实例化Prefab到场景，并设置父物体为GameObjectRoot
                prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset != null)
                {
                    // 查找或创建GameObjectRoot
                    GameObject root = GameObject.Find("GameObjectRoot");
                    if (root == null)
                    {
                        // 优先从预制体加载
                        var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MCP/GameObjectRoot.prefab");
                        if (rootPrefab != null)
                        {
                            root = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
                        }
                        else
                        {
                            root = new GameObject("GameObjectRoot");
                        }
                    }
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                    if (instance != null)
                    {
                        instance.transform.SetParent(root.transform, false);
                        // 不再额外乘以scaleMultiplier，Prefab本体已缩放
                        // 🔥 新增：为场景中的实例添加Clickable uiPrefab赋值
                        AssignClickableUIPrefab(instance);
                    }
                }
            }

            // 1. 重新扫描文件，统计结果
            var refreshedFiles = fileScanner.ScanForGLBFiles(modelsFolder, outputFolder);

            // 新增：统计输入输出数量
            int inputCount = glbFiles.Count;
            int outputCount = refreshedFiles.Count(f => f.hasPrefab);
            float progress = inputCount > 0 ? (float)outputCount / inputCount : 1f;

            // 2. 构造报告对象
            var report = new {
                finished = true,
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                inputCount = inputCount,         // 新增
                outputCount = outputCount,       // 新增
                progress = progress,             // 新增
                processedFiles = refreshedFiles.Count,
                results = refreshedFiles.Select(f => new {
                    file = f.fileName,
                    prefab = f.hasPrefab,
                    error = f.hasPrefab ? null : "未生成Prefab"
                }).ToList()
            };

            // 3. 不再写入JSON报告文件，直接返回report对象
            return report;
        }

        // 🔥 新增：为场景中的实例添加Clickable uiPrefab赋值的工具方法
        private static void AssignClickableUIPrefab(GameObject instance)
        {
            var clickables = instance.GetComponentsInChildren<Clickable>(true);
            var canvas = GameObject.Find("Canvas");
            GameObject uiPrefab = null;
            if (canvas != null && canvas.transform.childCount > 2)
                uiPrefab = canvas.transform.GetChild(2).gameObject;
            foreach (var clickable in clickables)
            {
                clickable.uiPrefab = uiPrefab;
                Debug.Log($"[场景实例化] 为 {clickable.gameObject.name} 的Clickable组件赋值uiPrefab: {(uiPrefab != null ? uiPrefab.name : "null")}");
            }
        }
    }
} 