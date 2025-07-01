using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using NodeGraph;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityMCP.Editor.Commands
{
    public static class EventCommandHandler
    {
        /// <summary>
        /// 入口：自动完成流程分析、资源匹配、事件脚本生成
        /// </summary>
        public static object FlowEventForth(JObject @params)
        {
            try
            {
                string coursename = (string)@params["coursename"];
                EventLinkAnalyzer.AnalyzeAndSaveNodeEventLinks(coursename);
                EventAutoMatcher.AutoMatch(coursename);
                EventScriptGenerator.CreateUnityEvent(coursename);
                return new { success = true, message = "FlowEventForth executed successfully." };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing FlowEventForth: {ex}");
                return new { success = false, message = ex.Message };
            }
        }

        /// <summary>
        /// 入口：批量添加Prefab物体到场景
        /// </summary>
        public static object AddEventObject(JObject @params)
        {
            try
            {
                string coursename = (string)@params["coursename"];
                return EventObjectUtility.AddEventObjectsToScene(coursename);
            }
            catch (Exception ex)
            {
                Debug.LogError($"AddEventObject failed: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }

        [MenuItem("Tools/最后一步")]
        public static void FlowEventForth2()//全自动固定系统
        {
            EventLinkAnalyzer.AnalyzeAndSaveNodeEventLinks();
            EventAutoMatcher.AutoMatch();
            EventScriptGenerator.CreateUnityEvent();
        }
    }

    //把当前所有要添加的物品自动添加到场景上并给予组件
    public static class EventObjectUtility
    {
        /// <summary>
        /// 批量将Prefab实例化到场景，并添加必要组件
        /// </summary>
        public static object AddEventObjectsToScene(string coursename)
        {
            string parentName = EventCommandConstants.GameObjectRoot;
            var parentObj = GameObject.Find(parentName);
            if (parentObj == null)
                throw new Exception($"找不到父物体: {parentName}");

            string perfabsDir = EventCommandConstants.GetPrefabDir(coursename);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { perfabsDir });
            if (guids.Length == 0)
                throw new Exception($"在 {perfabsDir} 下找不到任何 prefab");

            List<string> createdObjects = new List<string>();
            foreach (var guid in guids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null) continue;
                instance.transform.SetParent(parentObj.transform);

                AddComponentIfMissing(instance, "ObjectRegister");
                AddClickableIfMissing(instance);
                AddComponentIfMissing<BoxCollider>(instance);

                createdObjects.Add(instance.name);
            }

            CreateSubObjects();

            return new { success = true, created = createdObjects };
        }

        private static void AddComponentIfMissing(GameObject obj, string scriptName)
        {
            if (obj.GetComponent(scriptName) == null)
            {
                var scriptType = Type.GetType(scriptName) ?? GetTypeByName(scriptName);
                if (scriptType != null)
                    obj.AddComponent(scriptType);
            }
        }

        private static void AddComponentIfMissing<T>(GameObject obj) where T : Component
        {
            if (obj.GetComponent<T>() == null)
                obj.AddComponent<T>();
        }

        private static void AddClickableIfMissing(GameObject obj)
        {
            if (obj.GetComponent<Clickable>() == null)
            {
                var scriptType = Type.GetType("Clickable") ?? GetTypeByName("Clickable");
                if (scriptType != null)
                {
                    obj.AddComponent(scriptType);
                    var click = obj.GetComponent<Clickable>();
                    var canvas = GameObject.Find(EventCommandConstants.CanvasName);
                    if (canvas != null && canvas.transform.childCount > 1)
                        click.uiPrefab = canvas.transform.GetChild(1).gameObject;
                }
            }
        }

        private static Type GetTypeByName(string scriptName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(scriptName);
                if (type != null)
                    return type;
            }
            return null;
        }

        /// <summary>
        /// 创建细小子物体（Cube）到父物体下
        /// </summary>
        private static void CreateSubObjects()
        {
            var objdic = AllUnityEvent.GetInstanceInEditor().objectpartobj;
            if (objdic == null || objdic.Count == 0)
            {
                Debug.LogWarning("objectpartobj字典为空");
                return;
            }

            foreach (var item in objdic)
            {
                var parobj = GameObjectPool.GetInstanceInEditor().allNeedObject;
                GameObject parentObj = parobj.ContainsKey(item.Key) ? parobj[item.Key] : null;
                if (parentObj == null) continue;

                string[] childNames = System.Text.RegularExpressions.Regex.Split(item.Value, "[,，]");
                foreach (string childName in childNames)
                {
                    string trimmedName = childName.Trim();
                    if (string.IsNullOrEmpty(trimmedName)) continue;
                    if (parentObj.transform.Find(trimmedName) != null) continue;

                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = trimmedName;
                    cube.transform.SetParent(parentObj.transform);
                    cube.transform.localPosition = Vector3.zero;
                    cube.transform.localRotation = Quaternion.identity;
                    cube.transform.localScale = new Vector3(.02f, .02f, .02f);
                }
            }
        }
    }

    //事件默认路径管理
    public static class EventCommandConstants
    {
        public const string GameObjectRoot = "GameObjectRoot";
        public const string CanvasName = "Canvas";
        public static string GetPrefabDir(string course) => $"Assets/{course}/Prefabs";
        public static string GetAudioDir(string course) => $"Assets/{course}/Audio";
        public static string GetScriptPath(string course, string className) => $"Assets/{course}/Scripts/{className}.cs";
    }

    //自动匹配音频和高亮物体
    public static class EventAutoMatcher
    {
        /// <summary>
        /// 自动对当前默认的nodegraph进行对应的音频文件和高亮物体的添加
        /// </summary>
        public static void AutoMatch(string coursename = "默认")
        {
            // 1. 查找EventManager中的NodeGraph
            var eventmanager = EventManager.GetInstanceInEditor();
            if (eventmanager == null || eventmanager.graphs == null)
            {
                Debug.LogError("EventManager或其graphs未初始化！");
                return;
            }

            NodeGraph.NodeGraph targetGraph = null;
            if (eventmanager.graphs.ContainsKey(coursename))
            {
                targetGraph = eventmanager.graphs[coursename];
            }
            else
            {
                string assetDir = $"Assets/{coursename}";
                string[] guids = AssetDatabase.FindAssets("t:NodeGraph", new[] { assetDir });
                if (guids != null && guids.Length > 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var nodeGraphAsset = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
                    if (nodeGraphAsset != null)
                    {
                        targetGraph = nodeGraphAsset;
                        eventmanager.graphs[coursename] = targetGraph;
                        Debug.Log($"已从Asset中加载NodeGraph: {assetPath}");
                    }
                }
                if (targetGraph == null)
                {
                    Debug.LogError($"EventManager中未找到名为 {coursename} 的NodeGraph，且Assets目录下也未找到！");
                    return;
                }
            }

            // 2. 获取所有FlowEventNode节点
            var flowEventNodes = targetGraph.flowEventNodes;
            if (flowEventNodes == null || flowEventNodes.Count == 0)
            {
                Debug.LogError($"NodeGraph({coursename})中没有找到FlowEventNode！");
                return;
            }

            string audioFolderPath;
            // 3. 匹配音频
            if (coursename == "默认")
            {

                audioFolderPath = $"Assets/{eventmanager.graphs[coursename].name}/Audio";
            }
            else
            {
                audioFolderPath = $"Assets/{coursename}/Audio";
            }

            if (!Directory.Exists(audioFolderPath))
            {
                Debug.LogWarning($"音频文件夹不存在: {audioFolderPath}");
                return;
            }

            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioFolderPath });
            if (audioGuids == null || audioGuids.Length == 0)
            {
                Debug.LogWarning($"在 {audioFolderPath} 目录下没有找到音频文件！");
                return;
            }

            Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
            foreach (string guid in audioGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    audioClips[clip.name] = clip;
                }
            }

            // 4. 获取AllUnityEvent和GameObjectPool实例
            var allevent = AllUnityEvent.GetInstanceInEditor();
            var objectpool = GameObjectPool.GetInstanceInEditor();
            if (allevent == null || objectpool == null)
            {
                Debug.LogError("Cannot find AllUnityEvent or GameObjectPool instance!");
                return;
            }

            var holdDict = allevent.HoldDict;
            var holdForCombineDict = allevent.HoldForCombineDict;

            int audioMatchCount = 0;
            int highlightMatchCount = 0;

            // 5. 遍历所有FlowEventNode节点
            foreach (var node in flowEventNodes)
            {
                if (node == null) continue;

                // 获取节点的eventName字段
                var eventNameField = node.GetType().GetField("eventName");
                if (eventNameField == null) continue;
                string eventName = eventNameField.GetValue(node) as string;
                if (string.IsNullOrEmpty(eventName)) continue;

                // 1. 匹配音频
                string audioKey = eventName + "配音";
                if (audioClips.ContainsKey(audioKey))
                {
                    var inAudioClipField = node.GetType().GetField("inAudioClip",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (inAudioClipField != null)
                    {
                        inAudioClipField.SetValue(node, audioClips[audioKey]);
                        audioMatchCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot find inAudioClip field for node: {eventName}");
                    }
                }
                else
                {
                    Debug.Log($"No matching audio found for {eventName}, looking for: {audioKey}");
                }

                // 2. 匹配高亮物体
                var selectableObectsField = node.GetType().GetField("selectableObects");
                var selectableObectsIDField = node.GetType().GetField("selectableObectsID");
                var endActionEnumField = node.GetType().GetField("endActionEnum");

                if (selectableObectsField != null && selectableObectsIDField != null && endActionEnumField != null)
                {
                    var endAction = (EventEndAction)endActionEnumField.GetValue(node);
                    var selectableObects = selectableObectsField.GetValue(node) as List<GameObject>;
                    var selectableObectsID = selectableObectsIDField.GetValue(node) as List<GameObjectID>;

                    if (selectableObects == null)
                    {
                        selectableObects = new List<GameObject>();
                        selectableObectsField.SetValue(node, selectableObects);
                    }

                    if (selectableObectsID == null)
                    {
                        selectableObectsID = new List<GameObjectID>();
                        selectableObectsIDField.SetValue(node, selectableObectsID);
                    }
                    if (endAction == EventEndAction.Hold && holdDict.ContainsKey(eventName))
                    {
                        Debug.Log($"处理Hold节点: {eventName}");
                        var linkedEvents = holdDict[eventName];
                        if (linkedEvents.Count > 0)
                        {
                            string targetEventName = linkedEvents[0];
                            Debug.Log($"获取链接事件: {targetEventName}");
                            var nexthand = allevent.flownodeDic[targetEventName].currentClickObj;
                            AddHighlightObject(objectpool, nexthand, selectableObects, selectableObectsID, ref highlightMatchCount);
                        }
                    }
                    else if (endAction == EventEndAction.HoldForCombine && holdForCombineDict.ContainsKey(eventName))
                    {
                        Debug.Log($"处理HoldForCombine节点: {eventName}");
                        var linkedEvents = holdForCombineDict[eventName];
                        if (linkedEvents.Count > 0)
                        {
                            string targetEventName = linkedEvents[0];
                            Debug.Log($"获取链接事件: {targetEventName}");
                            var nexthand = allevent.flownodeDic[targetEventName].currentClickObj;
                            AddHighlightObject(objectpool, nexthand, selectableObects, selectableObectsID, ref highlightMatchCount);
                        }
                    }
                }

                EditorUtility.SetDirty(targetGraph);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"自动匹配完成！音频匹配: {audioMatchCount} 个，高亮物体匹配: {highlightMatchCount} 个");
        }

        private static void AddHighlightObject(GameObjectPool objectpool, string objectName,
   List<GameObject> selectableObects, List<GameObjectID> selectableObectsID, ref int highlightMatchCount)
        {
            if (objectpool.allNeedObject.ContainsKey(objectName))
            {
                var highlightObject = objectpool.allNeedObject[objectName];
                if (highlightObject != null)
                {
                    selectableObects.Add(highlightObject);
                    GameObjectID goID = new GameObjectID
                    {
                        ID = highlightObject.GetInstanceID(),
                        name = highlightObject.name
                    };
                    selectableObectsID.Add(goID);
                    highlightMatchCount++;
                    Debug.Log($"成功添加高亮物体: {objectName}");
                }
            }
        }

    }

    //自动处理nodegraph连接信息
    public static class EventLinkAnalyzer
    {
        /// <summary>
        /// 自动对当前默认的nodegraph或者传入的参数，进行nodegraph流程解析
        /// </summary>
        public static void AnalyzeAndSaveNodeEventLinks(string coursename = "默认")
        {
            // 获取当前打开的NodeGraph
            NodeGraph.NodeGraph targetGraph = null;

            // 根据名称查找指定的NodeGraph
            var eventmanager = EventManager.GetInstanceInEditor();
            if(eventmanager.graphs.ContainsKey(coursename))
                targetGraph = eventmanager.graphs[coursename];
            else           
            {
                string assetDir = $"Assets/{coursename}";
                string[] guids = AssetDatabase.FindAssets("t:NodeGraph", new[] { assetDir });
                if (guids != null && guids.Length > 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var nodeGraphAsset = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
                    if (nodeGraphAsset != null)
                    {
                        targetGraph = nodeGraphAsset;
                        eventmanager.graphs[coursename] = targetGraph; // 加入字典
                        Debug.Log($"已从Asset中加载NodeGraph: {assetPath}");
                    }
                }
                if (targetGraph == null)
                {
                    Debug.LogError($"EventManager中未找到名为 {coursename} 的NodeGraph，且Assets目录下也未找到！");
                    return;
                }
            }

            var holdDict = new Dictionary<string, List<string>>();
            var holdForCombineDict = new Dictionary<string, List<string>>();

            var flowEventNodes = targetGraph.flowEventNodes;
            var combineNodes = targetGraph.combineNodes;

            // 1. HoldForCombine类型分析
            foreach (var combineNode in combineNodes)
            {
                if (combineNode == null) continue;
                var inputLinks = targetGraph.Links.Where(link => link.TargetNodeGUID == combineNode.GUID).ToList();
                var connectedEventNames = inputLinks
                    .Select(link => flowEventNodes.FirstOrDefault(n => n.GUID == link.BaseNodeGUID))
                    .Where(n => n != null)
                    .Select(n => n.eventName)
                    .Distinct()
                    .ToList();

                foreach (var eventName in connectedEventNames)
                {
                    holdForCombineDict[eventName] = new List<string>(connectedEventNames);
                }
            }

            // 2. Hold类型分析
            foreach (var nodeData in flowEventNodes)
            {
                if (nodeData == null) continue;
                var eventName = nodeData.eventName;
                var endAction = nodeData.endActionEnum;

                if (endAction == EventEndAction.Hold)
                {
                    bool hasOutput = targetGraph.Links.Any(link => link.BaseNodeGUID == nodeData.GUID);
                    if (!hasOutput) continue;

                    List<string> outputEventNames = new List<string>();
                    foreach (var link in targetGraph.Links)
                    {
                        if (link.BaseNodeGUID == nodeData.GUID)
                        {
                            var targetNodeData = flowEventNodes.FirstOrDefault(n => n.GUID == link.TargetNodeGUID);
                            if (targetNodeData != null && targetNodeData.endActionEnum == EventEndAction.Hold)
                            {
                                outputEventNames.Add(targetNodeData.eventName);
                            }
                            else if (targetNodeData != null && holdForCombineDict.ContainsKey(targetNodeData.eventName))
                            {
                                outputEventNames = holdForCombineDict[targetNodeData.eventName];
                            }
                            else
                            {
                                outputEventNames = new List<string>();
                            }
                        }
                    }
                    holdDict[eventName] = outputEventNames;
                }
            }

            // 直接写入AllUnityEvent
            var allUnityEvent = AllUnityEvent.GetInstanceInEditor();
            if (allUnityEvent == null)
            {
                Debug.LogError("AllUnityEvent实例未找到，请确保已正确初始化");
                return;
            }

            allUnityEvent.HoldDict = new Dictionary<string, List<string>>(holdDict);
            allUnityEvent.HoldForCombineDict = new Dictionary<string, List<string>>(holdForCombineDict);
            Debug.Log($"分析结果已写入AllUnityEvent: HoldDict({holdDict.Count}), HoldForCombineDict({holdForCombineDict.Count})");
        }
    }

    //自动生成相关事件声明及监听
    public static class EventScriptGenerator
    {
        /// <summary>
        /// 自动对当前默认的nodegraph的所设计的事件进行声明及添加监听函数
        /// </summary>
        public static object CreateUnityEvent(string nodegraphname = "默认")
        {
            try
            {
                var allevent = AllUnityEvent.GetInstanceInEditor();
                if (allevent == null)
                {
                    throw new Exception("Cannot find AllUnityEvent instance!");
                }
                NodeGraph.NodeGraph targetGraph = null;
                var eventmanager = EventManager.GetInstanceInEditor();
                if (eventmanager == null || eventmanager.graphs == null || !eventmanager.graphs.ContainsKey(nodegraphname))
                {
                    string assetDir = $"Assets/{nodegraphname}";
                    string[] guids = AssetDatabase.FindAssets("t:NodeGraph", new[] { assetDir });
                    if (guids != null && guids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var nodeGraphAsset = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
                        if (nodeGraphAsset != null)
                        {
                            targetGraph = nodeGraphAsset;
                            Debug.Log($"已从Asset中加载NodeGraph: {assetPath}");                           
                        }
                    }
                    if (targetGraph == null)
                    {
                        Debug.LogError($"EventManager中未找到名为 {nodegraphname} 的NodeGraph，且Assets目录下也未找到！");
                    }
                }

                var holdDict = allevent.HoldDict;
                var holdForCombineDict = allevent.HoldForCombineDict;
                var nodeGraph = eventmanager.graphs[nodegraphname] == null ? targetGraph : eventmanager.graphs["默认"];
                var nodes = nodeGraph.flowEventNodes;

                // 生成时间戳
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string className = $"EventListeners_{timestamp}";

                // 生成脚本内容
                var scriptContent = new StringBuilder();
                scriptContent.AppendLine("using UnityEngine;");
                scriptContent.AppendLine("using UnityEngine.Events;");
                scriptContent.AppendLine("using System.Collections.Generic;");
                scriptContent.AppendLine();
                scriptContent.AppendLine($"public class {className} : SceneSingleton<{className}>, IEventListeners");
                scriptContent.AppendLine("{");
                scriptContent.AppendLine();
                scriptContent.AppendLine("    private EventUI eventUI;");
                scriptContent.AppendLine();

                // 声明所有事件
                int eventCount = 0;
                scriptContent.AppendLine("    #region Events");
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    var eventName = node.eventName;

                    if (allevent.flownodeDic.ContainsKey(eventName))
                    {
                        var eventData = allevent.flownodeDic[eventName];

                        if (!string.IsNullOrEmpty(eventData.enterEvent))
                        {
                            scriptContent.AppendLine($"        [Tooltip(\"{eventData.enterEvent}\")]");
                            scriptContent.AppendLine($"        //{eventName}");
                            scriptContent.AppendLine($"        public UnityEvent enter{eventCount};");
                            scriptContent.AppendLine();
                        }

                        if (!string.IsNullOrEmpty(eventData.exitEvent))
                        {
                            scriptContent.AppendLine($"        [Tooltip(\"{eventData.exitEvent}\")]");
                            scriptContent.AppendLine($"        //{eventName}");
                            scriptContent.AppendLine($"        public UnityEvent exit{eventCount};");
                            scriptContent.AppendLine();
                        }
                    }
                    eventCount++;
                }
                scriptContent.AppendLine("    #endregion");
                scriptContent.AppendLine();

                // Awake方法
                scriptContent.AppendLine("    private void Awake()");
                scriptContent.AppendLine("    {");
                scriptContent.AppendLine("        eventUI = GameObject.Find(\"Canvas\").transform.GetChild(2).GetComponent<EventUI>();");
                scriptContent.AppendLine("        RegisterEvents();");
                scriptContent.AppendLine("    }");
                scriptContent.AppendLine();

                // RegisterEvents方法
                scriptContent.AppendLine("    public void RegisterEvents()");
                scriptContent.AppendLine("    {");

                eventCount = 0;
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    var eventName = node.eventName;

                    if (allevent.flownodeDic.ContainsKey(eventName))
                    {
                        var eventData = allevent.flownodeDic[eventName];

                        if (!string.IsNullOrEmpty(eventData.enterEvent))
                        {
                            scriptContent.AppendLine($"        // {eventData.enterDes}");
                            scriptContent.AppendLine($"        enter{eventCount}.AddListener(() =>");
                            scriptContent.AppendLine($"        {{");
                            scriptContent.AppendLine($"            Debug.Log($\"{eventData.enterEvent} 触发: {eventData.enterDes}\");");

                            if (holdDict.ContainsKey(eventName))
                            {
                                var linkedEvents = holdDict[eventName];
                                scriptContent.AppendLine($"            // 处理链接事件");
                                scriptContent.AppendLine($"            List<string> linkedEventNames = new List<string>(){{");
                                foreach (var linkedEvent in linkedEvents)
                                {
                                    scriptContent.AppendLine($"            \"{linkedEvent}\",");
                                }
                                scriptContent.AppendLine("              };");
                                var nexthand = allevent.flownodeDic[linkedEvents[0]].currentClickObj;
                                scriptContent.AppendLine("             EventManager.instance.baseEventService.HoldForClickedEvent" +
                                    $"(GameObjectPool.instance.allNeedObject[\"{nexthand}\"],linkedEventNames,eventUI,\"{nexthand}\");");
                            }
                            else if (holdForCombineDict.ContainsKey(eventName))
                            {
                                var linkedEvents = holdForCombineDict[eventName];
                                scriptContent.AppendLine($"            // 处理链接事件");
                                scriptContent.AppendLine($"            List<string> linkedEventNames = new List<string>(){{");
                                foreach (var linkedEvent in linkedEvents)
                                {
                                    scriptContent.AppendLine($"            \"{linkedEvent}\",");
                                }
                                scriptContent.AppendLine("              };");
                                scriptContent.AppendLine("             EventManager.instance.baseEventService.HoldForClickedEvent" +
                                    $"(GameObjectPool.instance.allNeedObject[\"{eventData.currentClickObj}\"],linkedEventNames,eventUI,\"{eventData.currentClickObj}\");");
                            }
                            else
                            {
                                scriptContent.AppendLine($"            //此节点没有后续节点");
                            }

                            scriptContent.AppendLine($"            // TODO: 在这里添加自定义逻辑");
                            scriptContent.AppendLine($"        }});");
                            scriptContent.AppendLine();
                        }

                        if (!string.IsNullOrEmpty(eventData.exitEvent))
                        {
                            scriptContent.AppendLine($"        // {eventData.exitDes}");
                            scriptContent.AppendLine($"        exit{eventCount}.AddListener(() =>");
                            scriptContent.AppendLine($"        {{");
                            if (!string.IsNullOrEmpty(eventData.exitDes))
                            {
                                string exitContent = eventData.exitDes;
                                if (exitContent.StartsWith("记录"))
                                {
                                    scriptContent.AppendLine($"            EventManager.instance.ShowRecord" +
                                        $"(GameObjectPool.instance.allNeedObject[\"{eventData.currentClickObj}\"],\"{eventData.exitDes.Substring(3)}\");");
                                }
                                else if (exitContent.StartsWith("数值变化"))
                                {
                                    float a = 0, b = 0;
                                    string[] parts = Regex.Split(eventData.exitDes, @"[^\d]+");
                                    if (parts.Length >= 3)
                                    {
                                        a = float.Parse(parts[1]);
                                        b = float.Parse(parts[2]);
                                    }
                                    scriptContent.AppendLine($"            EventManager.instance.baseEventService.AnimateTextValue" +
                                        $"(GameObjectPool.instance.allNeedObject[\"{eventData.currentClickObj}\"],{a},{b});");
                                }
                            }
                            scriptContent.AppendLine($"        }});");
                            scriptContent.AppendLine();
                        }
                    }
                    eventCount++;
                }

                scriptContent.AppendLine("    }");
                scriptContent.AppendLine("}");

                // 写入文件 - 文件名包含时间戳

                string scriptPath = $"Assets\\{nodeGraph.name}\\Scripts\\{className}.cs";
                File.WriteAllText(scriptPath, scriptContent.ToString());
                AssetDatabase.Refresh();



                return new { success = true, message = $"成功生成{nodegraphname}事件监听器脚本！文件名: {className}.cs" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

    }

}


