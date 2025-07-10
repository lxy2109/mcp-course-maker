using UnityEngine;                // 导入Unity引擎核心功能命名空间
using UnityEditor;                // 导入Unity编辑器功能命名空间
using System;                     // 导入基础系统功能命名空间
using System.IO;                  // 导入文件和流操作功能命名空间
using System.Text;                // 导入文本处理功能命名空间
using System.Linq;                // 导入LINQ查询功能命名空间
using Newtonsoft.Json.Linq;       // 导入JSON处理库命名空间
using NodeGraph;                 // 导入NodeGraph命名空间
using System.Collections.Generic;  // 添加集合功能命名空间
using System.Reflection;           // 添加反射功能命名空间


namespace UnityMCP.Editor.Commands 
{
    /// <summary>
    /// 处理内容生成相关命令的静态类
    /// </summary>
    public static class NodeGraphCommandHandler
    {
        // public的方法
        // public static object CreateEmptyNodeGraph(JObject @params); 创建一个空的NodeGraph ScriptableObject文件
        // public static object GetNodeGraphInfo(JObject @params); 获取NodeGraph信息给LLM
        // public static object ImportExcelToNodeGraph(JObject @params); 将Excel文件数据导入到NodeGraph SO文件
        
        
        
        
        /// <summary>
        /// 创建一个空的NodeGraph ScriptableObject文件
        /// </summary>
        /// <param name="params">包含文件信息的JSON对象</param>
        /// <returns>创建结果</returns>

        #region 创建空的NodeGraph SO

        public static object CreateEmptyNodeGraph(JObject @params)
        {
            // 获取必需参数
            string name = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
            
            // 获取可选参数，默认路径为Assets/NodeGraphTool/Test
            string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";
            
            // 确保目录存在
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            // 构建完整的资产路径
            string assetPath = $"{path}/{name}.asset";
            
            try
            {
                // 检查是否已存在同名资产
                var existingAsset = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
                if (existingAsset != null)
                {
                    throw new Exception($"NodeGraph资产已存在: {assetPath}");
                }
                
                // 创建新的NodeGraph ScriptableObject
                var nodeGraph = ScriptableObject.CreateInstance<NodeGraph.NodeGraph>();
                
                // 保存资产
                AssetDatabase.CreateAsset(nodeGraph, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                return new
                {
                    success = true,
                    path = assetPath,
                    nodeCount = GetTotalNodeCount(nodeGraph)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"创建空NodeGraph失败: {ex.Message}");
            }
        }

        #endregion
        
        
        // 修改后的 GetTotalNodeCount 方法
        private static int GetTotalNodeCount(NodeGraph.NodeGraph nodeGraph)
        {
            // 基本节点类型计数
            int count = nodeGraph.startNodeDatas.Count +
                        nodeGraph.eventNodes.Count +
                        nodeGraph.stateNodes.Count +
                        nodeGraph.combineNodes.Count +
                        nodeGraph.compareNodes.Count +
                        nodeGraph.flowEventNodes.Count +
                        nodeGraph.stateEndNodeDatas.Count +
                        nodeGraph.stateStartNodeDatas.Count;
            
            // 数据节点类型计数
            count += nodeGraph.floatNodes.Count +
                     nodeGraph.intNodes.Count +
                     nodeGraph.vector2Nodes.Count +
                     nodeGraph.vector3Nodes.Count +
                     nodeGraph.vector4Nodes.Count +
                     nodeGraph.colorNodes.Count;
            
            // 其他特殊节点类型计数
            count += nodeGraph.stickyNodes.Count;
            
            // 如果有任何新的节点类型集合，添加在此处
            
            return count;
        }
        
        
        
        
        /// <summary>
        /// 获取NodeGraph信息给LLM
        /// </summary>
        /// <param name="params"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        #region 获取NodeGraph信息给LLM
        /// <summary>
        /// 获取NodeGraph信息给LLM
        /// </summary>
        /// <param name="params">包含节点图信息的JSON对象</param>
        /// <returns>包含节点图数据的JSON对象或错误信息</returns>
        public static object GetNodeGraphInfo(JObject @params)
        {
            try
            {
                // 从参数中获取对象名称
                string name = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
        
                // 从参数中获取路径
                string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";
        
                // 构建完整的资产路径
                string assetPath = $"{path}/{name}.asset";
        
                // 加载NodeGraph资产
                var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
        
                if (nodeGraph == null)
                {
                    return new
                    {
                        success = false,
                        message = $"NodeGraph资产未找到: {assetPath}"
                    };
                }
        
                // 获取所有节点数据
                var startNodes = SerializeNodeList(nodeGraph.startNodeDatas);
                var eventNodes = SerializeNodeList(nodeGraph.eventNodes);
                var stateNodes = SerializeNodeList(nodeGraph.stateNodes);
                var combineNodes = SerializeNodeList(nodeGraph.combineNodes);
                var compareNodes = SerializeNodeList(nodeGraph.compareNodes);
                var flowEventNodes = SerializeNodeList(nodeGraph.flowEventNodes);
                var stateEndNodes = SerializeNodeList(nodeGraph.stateEndNodeDatas);
                var stateStartNodes = SerializeNodeList(nodeGraph.stateStartNodeDatas);
                var floatNodes = SerializeNodeList(nodeGraph.floatNodes);
                var intNodes = SerializeNodeList(nodeGraph.intNodes);
                var vector2Nodes = SerializeNodeList(nodeGraph.vector2Nodes);
                var vector3Nodes = SerializeNodeList(nodeGraph.vector3Nodes);
                var vector4Nodes = SerializeNodeList(nodeGraph.vector4Nodes);
                var colorNodes = SerializeNodeList(nodeGraph.colorNodes);
                var stickyNodes = SerializeNodeList(nodeGraph.stickyNodes);
        
                var connections = nodeGraph.Links.Select(link => new
                {
                    sourceGuid = link.BaseNodeGUID,
                    sourcePort = link.OutputPortName,
                    sourceType = link.BaseNodeType,
                    targetGuid = link.TargetNodeGUID,
                    targetPort = link.TargetPortName,
                    targetType = link.TargetNodeType
                }).ToList();
        
                var groupData = nodeGraph.GetAllGroup();
                List<object> groups = new List<object>();
                if (groupData != null)
                {
                    groups = groupData.Select(group => new
                    {
                        guid = group.GUID,
                        title = group.title,
                        position = new { x = group.posX, y = group.posY, width = group.width, height = group.height },
                        nodeGuids = group.nodeGuids
                    }).Cast<object>().ToList();
                }
        
                return new
                {
                    success = true,
                    name = name,
                    path = assetPath,
                    nodes = new
                    {
                        startNodes,
                        eventNodes,
                        stateNodes,
                        combineNodes,
                        compareNodes,
                        flowEventNodes,
                        stateEndNodes,
                        stateStartNodes,
                        floatNodes,
                        intNodes,
                        vector2Nodes,
                        vector3Nodes,
                        vector4Nodes,
                        colorNodes,
                        stickyNodes
                    },
                    connections,
                    groups,
                    totalNodeCount = GetTotalNodeCount(nodeGraph)
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"获取NodeGraph信息失败: {ex.Message}",
                    stackTrace = ex.StackTrace
                };
            }
        }
        
        // 辅助方法：序列化节点列表
        private static List<object> SerializeNodeList<T>(List<T> nodes) where T : NodeBaseData
        {
            return nodes.Select(node => SerializeNode(node)).ToList();
        }
        
        // 辅助方法：序列化单个节点
        private static object SerializeNode(NodeBaseData node)
        {
            // 基本属性
            var baseData = new Dictionary<string, object>
            {
                { "guid", node.GUID },
                { "nodeName", node.NodeName },
                { "position", new { x = node.Position.x, y = node.Position.y, width = node.Position.width, height = node.Position.height } }
            };
            
            // 根据节点类型添加特定属性
            switch (node)
            {
                case EventNodeData eventNode:
                    baseData["description"] = eventNode.description;
                    baseData["eventName"] = eventNode.eventName;
                    baseData["objectName"] = eventNode.obj != null ? eventNode.obj.name : null;
                    baseData["timelineAsset"] = eventNode.timelineAsset != null ? eventNode.timelineAsset.name : null;
                    break;
                    
                case StateNodeData stateNode:
                    baseData["description"] = stateNode.description;
                    baseData["stateName"] = stateNode.stateName;
                    baseData["mainUI"] = stateNode.mainUI != null ? stateNode.mainUI.name : null;
                    baseData["eventCount"] = stateNode.eventCount;
                    break;
                    
                case FlowEventNodeData flowEventNode:
                    baseData["description"] = flowEventNode.description;
                    baseData["flowGraph"] = flowEventNode.flowGraph;
                    baseData["eventName"] = flowEventNode.eventName;
                    baseData["eventContent"] = flowEventNode.eventContent;
                    baseData["handTip"] = flowEventNode.handTip;
                    baseData["itemCount"] = flowEventNode.itemCount;
                    baseData["timelineCount"] = flowEventNode.timelineCount;
                    
                    // 添加事件相关字段
                    baseData["enterEventName"] = flowEventNode.enterEventName;
                    baseData["enterEventContent"] = flowEventNode.enterEventContent;
                    baseData["exitEventName"] = flowEventNode.exitEventName;
                    baseData["exitEventContent"] = flowEventNode.exitEventContent;
                    
                    // 添加音频和配音相关字段
                    baseData["voiceName"] = flowEventNode.voiceName;
                    baseData["voiceContent"] = flowEventNode.voiceContent;
                    baseData["inAudioClip"] = flowEventNode.inAudioClip != null ? AssetDatabase.GetAssetPath(flowEventNode.inAudioClip) : null;
                    
                    // 添加Timeline相关字段
                    baseData["cameraTimelineName"] = flowEventNode.cameraTimelineName;
                    baseData["cameraTimelineContent"] = flowEventNode.cameraTimelineContent;
                    baseData["objectTimelineName"] = flowEventNode.objectTimelineName;
                    baseData["objectTimelineContent"] = flowEventNode.objectTimelineContent;
                    baseData["endActionEnum"] = flowEventNode.endActionEnum.ToString();
                    
                    // 序列化可交互对象ID列表
                    if (flowEventNode.selectableObectsID != null && flowEventNode.selectableObectsID.Count > 0)
                    {
                        var selectableObjectsList = flowEventNode.selectableObectsID.Select(obj => new {
                            name = obj.name,
                            id = obj.ID
                        }).ToList();
                        baseData["selectableObectsID"] = selectableObjectsList;
                    }
                    
                    // 序列化Timeline资产列表
                    if (flowEventNode.timelineAssets != null && flowEventNode.timelineAssets.Count > 0)
                    {
                        var timelinePaths = flowEventNode.timelineAssets
                            .Where(asset => asset != null)
                            .Select(asset => AssetDatabase.GetAssetPath(asset))
                            .ToList();
                        baseData["timelineAssets"] = timelinePaths;
                    }
                    break;
                    
                case FloatNodeData floatNode:
                    baseData["value"] = floatNode.a;
                    break;
                    
                case IntNodeData intNode:
                    baseData["value"] = intNode.a;
                    break;
                    
                case Vector2NodeData vector2Node:
                    baseData["value"] = new { x = vector2Node.a.x, y = vector2Node.a.y };
                    break;
                    
                case Vector3NodeData vector3Node:
                    baseData["value"] = new { x = vector3Node.a.x, y = vector3Node.a.y, z = vector3Node.a.z };
                    break;
                    
                case Vector4NodeData vector4Node:
                    baseData["value"] = new { x = vector4Node.a.x, y = vector4Node.a.y, z = vector4Node.a.z, w = vector4Node.a.w };
                    break;
                    
                case ColorNodeData colorNode:
                    baseData["value"] = new { r = colorNode.a.r, g = colorNode.a.g, b = colorNode.a.b, a = colorNode.a.a };
                    baseData["isHdr"] = colorNode.isHdr;
                    break;
                    
                case CombineNodeData combineNode:
                    baseData["description"] = combineNode.description;
                    baseData["inputCount"] = combineNode.inputCount;
                    break;
                    
                case CompareNodeData compareNode:
                    baseData["compareType"] = compareNode.compare.ToString();
                    break;
                    
                case StickyNodeData stickyNode:
                    baseData["description"] = stickyNode.description;
                    break;
            }
            
            return baseData;
        }

        #endregion
        

        /// <summary>
        /// 将Excel文件数据导入到NodeGraph SO文件
        /// </summary>
        /// <param name="params">包含导入信息的JSON对象</param>
        /// <returns>导入结果</returns>
        public static object ImportExcelToNodeGraph(JObject @params)
        {
            // 获取必需参数
            string nodeGraphPath = (string)@params["nodeGraphPath"] ?? throw new Exception("参数'nodeGraphPath'是必需的。");
            string excelPath = (string)@params["excelPath"] ?? throw new Exception("参数'excelPath'是必需的。");
        
            // 获取可选参数
            bool generateVoice = @params["generateVoice"] != null ? (bool)@params["generateVoice"] : true;
            bool autoRegisterAndOpen = @params["autoRegisterAndOpen"] != null ? (bool)@params["autoRegisterAndOpen"] : true;
        
            // 处理路径 - 确保Excel路径是绝对路径
            if (!Path.IsPathRooted(excelPath))
            {
                if (excelPath.StartsWith("Assets/"))
                    excelPath = excelPath.Substring("Assets/".Length);
                else if (excelPath.StartsWith("Assets\\"))
                    excelPath = excelPath.Substring("Assets\\".Length);
                excelPath = Path.Combine(Application.dataPath, excelPath);
            }
        
            if (!File.Exists(excelPath))
            {
                throw new Exception($"Excel文件不存在: {excelPath}");
            }
        
            // 加载NodeGraph资产
            string fullNodeGraphPath = nodeGraphPath;
            if (!nodeGraphPath.StartsWith("Assets/"))
            {
                fullNodeGraphPath = $"Assets/{nodeGraphPath}";
            }
        
            var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(fullNodeGraphPath);
            if (nodeGraph == null)
            {
                throw new Exception($"NodeGraph文件不存在: {fullNodeGraphPath}");
            }
        
            try
            {
                // 步骤1和2: 创建DataInfoCreator并设置Excel文件路径
                var dataInfoCreator = ScriptableObject.CreateInstance<DataInfoCreator>();
                dataInfoCreator.assetFilePath = excelPath;
        
                // 步骤3: 提取步骤数据
                // 由于GetAssets是私有方法，需要通过反射调用
                var getAssetsMethod = typeof(DataInfoCreator).GetMethod("GetAssets",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (getAssetsMethod == null)
                {
                    throw new Exception("无法找到GetAssets方法");
                }
                getAssetsMethod.Invoke(dataInfoCreator, null);
        
                if (dataInfoCreator.datas.Count == 0)
                {
                    throw new Exception("从Excel提取数据失败，数据为空");
                }
        
                // 在创建节点之前，确保 AllUnityEvent 实例存在
                var allUnityEvent = AllUnityEvent.GetInstanceInEditor();
                if (allUnityEvent == null)
                {
                    throw new Exception("AllUnityEvent实例未找到，请确保已正确初始化");
                }

                // 在添加事件之前，确保字典已初始化
                if (allUnityEvent.flownodeDic == null)
                {
                    allUnityEvent.flownodeDic = new Dictionary<string, FlowEventNodeD>();
                }
                
                
                // 步骤4: 创建节点
                // 首先清空现有数据
                nodeGraph.ClearAllNodeDatas();
                
                Dictionary<Vector2, NodeBaseData> positionToNodeMap = new Dictionary<Vector2, NodeBaseData>();
        
                int x = 0;
                int y = 0;
                string tempFlowGraph = "";
                int count = 0;
                string nowFlowGraph = "";
                
                Rect startNodePos = new Rect(300 * x, 300 * y, 100, 150);
                StartNodeData startNode = new StartNodeData();
                startNode.Position = startNodePos;
                startNode.GUID = Guid.NewGuid().ToString();
                startNode.NodeName = "StartNode";
                nodeGraph.startNodeDatas.Add(startNode);
                positionToNodeMap[new Vector2(x, y)] = startNode;
        
                // 创建节点，复制自NodeGraphWindow.CreateAllVoice中的逻辑
                for (int i = 0; i < dataInfoCreator.datas.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("从Excel中生成节点", "创建节点中", (float)i / dataInfoCreator.datas.Count);
        
                    // 创建 FlowEventNodeD 结构
                    FlowEventNodeD nodeData = new FlowEventNodeD
                    {
                        currentClickObj = dataInfoCreator.datas[i].handTip,
                        enterEvent = dataInfoCreator.datas[i].enterEventName,
                        enterDes = dataInfoCreator.datas[i].enterEventContent,
                        exitEvent = dataInfoCreator.datas[i].exitEventName,
                        exitDes = dataInfoCreator.datas[i].exitEventContent
                    };
                    
                    allUnityEvent.flownodeDic[dataInfoCreator.datas[i].eventName] = nodeData;
        
                    // 按步骤名称组织节点布局
                    if (tempFlowGraph != dataInfoCreator.datas[i].flowGraph)
                    {
                        y = 0;
        
                        if (count > 0)
                        {
                            x++;
                            Vector2 combinePos = new Vector2(300 * x, 0);

                            // 创建合并节点数据
                            x++;
                            Rect combineNodePos = new Rect(300 * x, 300 * y, 100, 150);
                            CombineNodeData combineNode = new CombineNodeData();
                            combineNode.Position = combineNodePos;
                            combineNode.GUID = Guid.NewGuid().ToString();
                            combineNode.NodeName = "CombineNode";
                            combineNode.inputCount = count + 1;
                            nodeGraph.combineNodes.Add(combineNode);
                            positionToNodeMap[new Vector2(x, y)] = combineNode;

                    
                            // 在CombineNode右侧创建一个空的FlowEventNodeData节点
                            x++;
                            Rect emptyNodePos = new Rect(300 * x, 300 * y, 100, 150);
                            FlowEventNodeData emptyNode = new FlowEventNodeData();
                            emptyNode.Position = emptyNodePos;
                            emptyNode.GUID = Guid.NewGuid().ToString();
                            emptyNode.NodeName = "FlowEventNode";
                            emptyNode.eventName = nowFlowGraph + "后置";
                            emptyNode.enterEventName = nowFlowGraph + "后置事件";
                            nodeGraph.flowEventNodes.Add(emptyNode);
                            
                            // 添加到字典中
                            FlowEventNodeD nodedata = new FlowEventNodeD
                            {
                                enterEvent = emptyNode.enterEventName
                            };
                            allUnityEvent.flownodeDic[emptyNode.eventName] = nodedata;
                            
                            positionToNodeMap[new Vector2(x, y)] = emptyNode;
                        }
        
                        x++;
                        Rect nodePos = new Rect(300 * x, 300 * y, 100, 150);
        
                        // 创建流程事件节点
                        nowFlowGraph = dataInfoCreator.datas[i].flowGraph;
                        
                        FlowEventNodeData flowEventNode = new FlowEventNodeData();
                        flowEventNode.Position = nodePos;
                        flowEventNode.GUID = Guid.NewGuid().ToString();
                        flowEventNode.NodeName = "FlowEventNode";
                
                        // 设置节点属性
                        flowEventNode.flowGraph = dataInfoCreator.datas[i].flowGraph;
                        flowEventNode.eventName = dataInfoCreator.datas[i].eventName;
                        flowEventNode.eventContent = dataInfoCreator.datas[i].eventContent;
                        flowEventNode.enterEventName = dataInfoCreator.datas[i].enterEventName;
                        flowEventNode.enterEventContent = dataInfoCreator.datas[i].enterEventContent;
                        flowEventNode.exitEventName = dataInfoCreator.datas[i].exitEventName;
                        flowEventNode.exitEventContent = dataInfoCreator.datas[i].exitEventContent;
                        flowEventNode.cameraTimelineName = dataInfoCreator.datas[i].cameraTimelineName;
                        flowEventNode.cameraTimelineContent = dataInfoCreator.datas[i].cameraTimelineContent;
                        flowEventNode.objectTimelineName = dataInfoCreator.datas[i].objectTimelineName;
                        flowEventNode.objectTimelineContent = dataInfoCreator.datas[i].objectTimelineContent;
                        flowEventNode.voiceName = dataInfoCreator.datas[i].voiceName;

                        // 如果需要，还可以加载音频资源
                        if (generateVoice)
                        {
                            // 这里可以添加音频资源的处理
                            string voicePath = $"Lesson/{i}_{dataInfoCreator.datas[i].eventName}/{dataInfoCreator.datas[i].voiceName}";
                            // 注意：在CommandHandler中可能无法直接加载和设置AudioClip
                        }

                        nodeGraph.flowEventNodes.Add(flowEventNode);
                        positionToNodeMap[new Vector2(x, y)] = flowEventNode;
                
                        count = 0;
                        tempFlowGraph = dataInfoCreator.datas[i].flowGraph;
                    }
                    else
                    {
                        // 相同步骤名称的节点垂直排列
                        y++;
                        count++;
                        Rect nodePos = new Rect(300 * x, 300 * y, 100, 150);
                
                        FlowEventNodeData flowEventNode = new FlowEventNodeData();
                        flowEventNode.Position = nodePos;
                        flowEventNode.GUID = Guid.NewGuid().ToString();
                        flowEventNode.NodeName = "FlowEventNode";
                
                        // 设置节点属性
                        flowEventNode.flowGraph = dataInfoCreator.datas[i].flowGraph;
                        flowEventNode.eventName = dataInfoCreator.datas[i].eventName;
                        flowEventNode.eventContent = dataInfoCreator.datas[i].eventContent;
                        flowEventNode.enterEventName = dataInfoCreator.datas[i].enterEventName;
                        flowEventNode.enterEventContent = dataInfoCreator.datas[i].enterEventContent;
                        flowEventNode.exitEventName = dataInfoCreator.datas[i].exitEventName;
                        flowEventNode.exitEventContent = dataInfoCreator.datas[i].exitEventContent;
                        flowEventNode.cameraTimelineName = dataInfoCreator.datas[i].cameraTimelineName;
                        flowEventNode.cameraTimelineContent = dataInfoCreator.datas[i].cameraTimelineContent;
                        flowEventNode.objectTimelineName = dataInfoCreator.datas[i].objectTimelineName;
                        flowEventNode.objectTimelineContent = dataInfoCreator.datas[i].objectTimelineContent;
                        flowEventNode.voiceName = dataInfoCreator.datas[i].voiceName;

                        // 处理音频资源
                        if (generateVoice)
                        {
                            // 这里可以添加音频资源的处理
                            string voicePath = $"Lesson/{i}_{dataInfoCreator.datas[i].eventName}/{dataInfoCreator.datas[i].voiceName}";
                            // 注意：在CommandHandler中可能无法直接加载和设置AudioClip
                        }

                        nodeGraph.flowEventNodes.Add(flowEventNode);
                        positionToNodeMap[new Vector2(x, y)] = flowEventNode;
                    }
                }
        
                EditorUtility.ClearProgressBar();
        
                // 步骤5: 保存数据
                EditorUtility.SetDirty(nodeGraph);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
        
                // 步骤6: 连接节点
                ConnectNodes(nodeGraph, positionToNodeMap);
                
                //步骤7：注册nodegraph
                AddGraphPool(nodeGraph);

                //步骤8：打开nodegraph
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = nodeGraph;
                
                // 返回导入结果
                return new {
                    success = true,
                    path = fullNodeGraphPath,
                    nodesCreated = GetTotalNodeCount(nodeGraph),
                    linksCreated = nodeGraph.Links.Count,
                    nodeDetails = new {
                        startNodes = nodeGraph.startNodeDatas.Count,
                        flowEventNodes = nodeGraph.flowEventNodes.Count,
                        combineNodes = nodeGraph.combineNodes.Count,
                        emptyNodes = nodeGraph.flowEventNodes.Count(n => string.IsNullOrEmpty(n.eventName))
                    },
                    dataCount = dataInfoCreator.datas.Count,
                    generatedVoice = generateVoice,
                    autoRegistered = autoRegisterAndOpen,
                    layout = new {
                        columnsCount = x + 1,
                        maxRowCount = count + 1,
                        layoutWidth = 300 * (x + 1),
                        layoutHeight = 300 * Math.Max(y + 1, count + 1)
                    },
                    lastFlowGraph = tempFlowGraph
                };
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                throw new Exception($"导入Excel数据失败: {ex.Message}");
            }
        }
        
        
        /// <summary>
        /// 将NodeGraph添加到图形池中并初始化
        /// </summary>
        /// <param name="params">包含NodeGraph名称的JSON对象</param>
        /// <returns>操作结果</returns>
        public static object AddGraphPool(JObject @params)
        {
            try
            {
                string nodeGraphName = @params["nodeGraphName"]?.ToString();
                if (string.IsNullOrEmpty(nodeGraphName))
                {
                    throw new Exception("nodeGraph name is required!");
                }

                // 从Resources/Course/NodeGraph路径加载NodeGraph资源
                string resourcePath = $"Course/NodeGraph/{nodeGraphName}";
                NodeGraph.NodeGraph nodeGraph = Resources.Load<NodeGraph.NodeGraph>(resourcePath);
                
                if (nodeGraph == null)
                {
                    throw new Exception($"Cannot find NodeGraph with name: {nodeGraphName} in path: {resourcePath}");
                }

                var eventmanager = EventManager.GetInstanceInEditor();
                
                // 检查eventmanager是否为null
                if (eventmanager == null)
                {
                    throw new Exception("EventManager实例为null，无法添加到图形池");
                }
                
                // 检查graphs字典是否为null，如果是则初始化
                if (eventmanager.graphs == null)
                {
                    eventmanager.graphs = new Dictionary<string, NodeGraph.NodeGraph>();
                }

                // 添加或更新"默认"键的NodeGraph
                eventmanager.graphs["默认"] = nodeGraph;

                eventmanager.DefaultInit();//初始化

                // 标记对象为dirty，让Unity知道对象已被修改
                UnityEditor.EditorUtility.SetDirty(eventmanager);
                
                // 如果是场景中的对象，还需要标记场景为dirty
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(eventmanager.gameObject.scene);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return new { success = true, message = $"Successfully added NodeGraph: {nodeGraphName}" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public static void AddGraphPool(NodeGraph.NodeGraph nodeGraph)
        {
            var eventmanager = EventManager.GetInstanceInEditor();

            if (eventmanager.graphs.ContainsKey("默认"))
                eventmanager.graphs["默认"] = nodeGraph;
            
            // eventmanager.currentGraph = nodeGraph;

            Debug.Log(eventmanager.graphs["默认"].name);
            eventmanager.DefaultInit();//初始化

            // 标记对象为dirty，让Unity知道对象已被修改
            UnityEditor.EditorUtility.SetDirty(eventmanager);
            
            // 如果是场景中的对象，还需要标记场景为dirty
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(eventmanager.gameObject.scene);
            }
        }
        
        
        // 连接节点的辅助方法
        private static void ConnectNodes(NodeGraph.NodeGraph nodeGraph, Dictionary<Vector2, NodeBaseData> positionToNodeMap)
        {
            // 按x坐标分组节点（构建列结构）
            Dictionary<int, Dictionary<int, NodeBaseData>> columnMap = new Dictionary<int, Dictionary<int, NodeBaseData>>();
            
            foreach (var entry in positionToNodeMap)
            {
                int x = (int)entry.Key.x;
                int y = (int)entry.Key.y;
                
                if (!columnMap.ContainsKey(x))
                {
                    columnMap[x] = new Dictionary<int, NodeBaseData>();
                }
                
                columnMap[x][y] = entry.Value;
            }
            
            // 获取所有x坐标并排序
            List<int> xCoordinates = columnMap.Keys.ToList();
            xCoordinates.Sort();
            
            // 遍历每一列（除了最后一列），建立与下一列的连接
            for (int i = 0; i < xCoordinates.Count - 1; i++)
            {
                int x = xCoordinates[i];
                int nextX = xCoordinates[i + 1];
                
                Dictionary<int, NodeBaseData> currentColumn = columnMap[x];
                Dictionary<int, NodeBaseData> nextColumn = columnMap[nextX];
                
                // 当前列节点按y坐标排序
                List<int> yCoordinates = currentColumn.Keys.ToList();
                yCoordinates.Sort();
                
                // 如果当前列只有一个节点
                if (currentColumn.Count == 1)
                {
                    // 获取当前列的唯一节点
                    NodeBaseData sourceNode = currentColumn[yCoordinates[0]];
                    
                    // 获取下一列的节点（首选y=0的节点）
                    List<int> nextYCoordinates = nextColumn.Keys.ToList();
                    nextYCoordinates.Sort();
                    NodeBaseData targetNode = nextColumn.ContainsKey(0) ? 
                        nextColumn[0] : 
                        nextColumn[nextYCoordinates[0]];
                    
                    // 连接节点
                    ConnectTwoNodes(nodeGraph, sourceNode, targetNode);
                }
                // 如果当前列有多个节点，则下一列应该有CombineNode
                else if (currentColumn.Count > 1)
                {
                    // 查找下一列中的合并节点
                    NodeBaseData combineNode = null;
                    foreach (var nodeEntry in nextColumn)
                    {
                        if (nodeEntry.Value.NodeName.Contains("CombineNode"))
                        {
                            combineNode = nodeEntry.Value;
                            break;
                        }
                    }
                    
                    if (combineNode != null)
                    {
                        // 依次连接当前列的所有节点到合并节点
                        for (int j = 0; j < yCoordinates.Count; j++)
                        {
                            NodeBaseData sourceNode = currentColumn[yCoordinates[j]];
                            ConnectTwoNodes(nodeGraph, sourceNode, combineNode, "Output1", $"Input{j + 1}");
                        }
                    }
                }
            }
        }

        // 连接两个节点
        private static void ConnectTwoNodes(NodeGraph.NodeGraph nodeGraph, NodeBaseData sourceNode, NodeBaseData targetNode, 
                                          string outputPortName = null, string inputPortName = null)
        {
            try
            {
                // 确定源节点输出端口名称
                if (string.IsNullOrEmpty(outputPortName))
                {
                    if (sourceNode.NodeName.Contains("FlowEventNode"))
                    {
                        outputPortName = "Output1";
                    }
                    else
                    {
                        outputPortName = "Output";
                    }
                }
                
                // 确定目标节点输入端口名称
                if (string.IsNullOrEmpty(inputPortName))
                {
                    inputPortName = "Input";
                }
                
                // 创建并存储连接数据
                NodeLinkData linkData = new NodeLinkData
                {
                    BaseNodeGUID = sourceNode.GUID,
                    OutputPortName = outputPortName,
                    BaseNodeType = sourceNode.NodeName,
                    TargetNodeGUID = targetNode.GUID,
                    TargetPortName = inputPortName,
                    TargetNodeType = targetNode.NodeName
                };
                
                nodeGraph.Links.Add(linkData);
            }
            catch (Exception e)
            {
                // 连接节点失败时静默处理
            }
        }
        
        
        /// <summary>
        /// 获取NodeGraph中所有的FlowEventNode节点信息
        /// </summary>
        /// <param name="params">包含节点图信息的JSON对象</param>
        /// <returns>包含FlowEventNode节点的JSON数据</returns>
        public static object GetFlowEventNodes(JObject @params)
        {
            // 获取必需参数
            string name = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
        
            // 获取可选参数，默认路径为Assets/NodeGraphTool/Test
            string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";
        
            // 构建完整的资产路径
            string assetPath = $"{path}/{name}.asset";
        
            try
            {
                // 加载NodeGraph资产
                var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
        
                if (nodeGraph == null)
                {
                    throw new Exception($"NodeGraph资产未找到: {assetPath}");
                }
        
                // 只序列化FlowEventNode节点
                var flowEventNodes = SerializeNodeList(nodeGraph.flowEventNodes);
        
                return new
                {
                    success = true,
                    name = name,
                    path = assetPath,
                    flowEventNodes = flowEventNodes,
                    nodeCount = nodeGraph.flowEventNodes.Count
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"获取FlowEventNode节点信息失败: {ex.Message}"
                };
            }
        }
        
        
        
        /// <summary>
        /// 获取NodeGraph中所有FlowEventNode节点的名称列表
        /// </summary>
        /// <param name="params">包含节点图信息的JSON对象</param>
        /// <returns>只包含FlowEventNode节点名称的JSON数据</returns>
        public static object GetFlowEventNodeNames(JObject @params)
        {
            // 获取必需参数
            string name = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
        
            // 获取可选参数，默认路径
            string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";
        
            // 构建完整的资产路径
            string assetPath = $"{path}/{name}.asset";
        
            try
            {
                // 加载NodeGraph资产
                var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
        
                if (nodeGraph == null)
                {
                    return new
                    {
                        success = false,
                        message = $"NodeGraph资产未找到: {assetPath}"
                    };
                }
        
                // 只提取FlowEventNode节点名称
                var nodeNames = nodeGraph.flowEventNodes
                    .Select(node => node.eventName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();
        
                return new
                {
                    success = true,
                    name = name,
                    path = assetPath,
                    nodeNames = nodeNames,
                    nodeCount = nodeNames.Count
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"获取FlowEventNode节点名称失败: {ex.Message}"
                };
            }
        }
        
        
        
        /// <summary>
        /// 根据事件名称获取特定FlowEventNode节点的完整信息
        /// </summary>
        /// <param name="params">包含查询参数的JSON对象</param>
        /// <returns>包含指定FlowEventNode节点完整信息的JSON数据</returns>
        public static object GetFlowEventNodeByName(JObject @params)
        {
            try
            {
                // 获取必需参数
                string nodeName = (string)@params["eventName"] ?? throw new Exception("参数'eventName'是必需的。");
                string name = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
        
                // 获取可选参数，默认路径
                string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";
        
                // 构建完整的资产路径
                string assetPath = $"{path}/{name}.asset";
        
                // 加载NodeGraph资产
                var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);
        
                if (nodeGraph == null)
                {
                    return new
                    {
                        success = false,
                        message = $"NodeGraph资产未找到: {assetPath}"
                    };
                }
        
                // 查找匹配eventName的FlowEventNode
                var flowEventNode = nodeGraph.flowEventNodes
                    .FirstOrDefault(node => node.eventName == nodeName);
        
                if (flowEventNode == null)
                {
                    return new
                    {
                        success = false,
                        message = $"未找到名为'{nodeName}'的FlowEventNode节点"
                    };
                }
        
                // 序列化找到的节点
                var serializedNode = SerializeNode(flowEventNode);
        
                return new
                {
                    success = true,
                    name = name,
                    path = assetPath,
                    eventName = nodeName,
                    nodeInfo = serializedNode
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"获取FlowEventNode节点信息失败: {ex.Message}"
                };
            }
        }
        
        
        
        /// <summary>
        /// 更新指定FlowEventNode节点的timeline资产引用
        /// </summary>
        /// <param name="params">包含更新信息的JSON对象</param>
        /// <returns>更新结果</returns>
        public static object UpdateFlowEventNodeTimelineAssets(JObject @params)
        {
            try
            {
                // 获取必需参数
                string nodeName = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
                string eventName = (string)@params["eventName"] ?? throw new Exception("参数'eventName'是必需的。");

                // 获取可选参数
                string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";
                string cameraTimelineAsset = (string)@params["cameraTimelineAsset"];
                string objectTimelineAsset = (string)@params["objectTimelineAsset"];

                // 构建完整的资产路径
                string assetPath = $"{path}/{nodeName}.asset";

                // 加载NodeGraph资产
                var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);

                if (nodeGraph == null)
                {
                    return new
                    {
                        success = false,
                        message = $"NodeGraph资产未找到: {assetPath}"
                    };
                }

                // 查找匹配eventName的FlowEventNode
                var flowEventNode = nodeGraph.flowEventNodes
                    .FirstOrDefault(node => node.eventName == eventName);

                if (flowEventNode == null)
                {
                    return new
                    {
                        success = false,
                        message = $"未找到名为'{eventName}'的FlowEventNode节点"
                    };
                }

                // 初始化timelineAssets列表（如果为null）
                if (flowEventNode.timelineAssets == null)
                {
                    flowEventNode.timelineAssets = new List<UnityEngine.Timeline.TimelineAsset>();
                }

                int updatedCount = 0;
                List<string> updatedAssets = new List<string>();

                // 更新相机Timeline资产
                if (!string.IsNullOrEmpty(cameraTimelineAsset))
                {
                    var cameraTimeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Timeline.TimelineAsset>(cameraTimelineAsset);
                    if (cameraTimeline != null)
                    {
                        // 检查是否已存在，避免重复添加
                        if (!flowEventNode.timelineAssets.Contains(cameraTimeline))
                        {
                            flowEventNode.timelineAssets.Add(cameraTimeline);
                            updatedCount++;
                            updatedAssets.Add($"相机Timeline: {cameraTimelineAsset}");
                        }
                    }
                }

                // 更新物体Timeline资产
                if (!string.IsNullOrEmpty(objectTimelineAsset))
                {
                    var objectTimeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Timeline.TimelineAsset>(objectTimelineAsset);
                    if (objectTimeline != null)
                    {
                        // 检查是否已存在，避免重复添加
                        if (!flowEventNode.timelineAssets.Contains(objectTimeline))
                        {
                            flowEventNode.timelineAssets.Add(objectTimeline);
                            updatedCount++;
                            updatedAssets.Add($"物体Timeline: {objectTimelineAsset}");
                        }
                    }
                }

                // timelineCount现在是自动计算属性，无需手动设置
                int previousTimelineCount = flowEventNode.timelineCount;
                // flowEventNode.timelineCount 现在自动等于 timelineAssets.Count
                
                // 标记为脏数据，确保保存
                EditorUtility.SetDirty(nodeGraph);

                return new
                {
                    success = true,
                    message = $"成功更新FlowEventNode '{eventName}' 的Timeline资产 | timelineCount已自动更新: {previousTimelineCount} -> {flowEventNode.timelineCount}",
                    nodeGraphPath = assetPath,
                    eventName = eventName,
                    updatedCount = updatedCount,
                    updatedAssets = updatedAssets,
                    totalTimelineAssets = flowEventNode.timelineAssets.Count,
                    timelineCountBefore = previousTimelineCount,
                    timelineCountAfter = flowEventNode.timelineCount,
                    timelineCountChanged = previousTimelineCount != flowEventNode.timelineCount
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"更新FlowEventNode Timeline资产失败: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// 保存NodeGraph文件的所有修改
        /// </summary>
        /// <param name="params">包含保存信息的JSON对象</param>
        /// <returns>保存结果</returns>
        public static object SaveNodeGraphChanges(JObject @params)
        {
            try
            {
                // 获取必需参数
                string name = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");

                // 获取可选参数
                string path = (string)@params["path"] ?? "Assets/NodeGraphTool/Test";

                // 构建完整的资产路径
                string assetPath = $"{path}/{name}.asset";

                // 加载NodeGraph资产以验证其存在
                var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph.NodeGraph>(assetPath);

                if (nodeGraph == null)
                {
                    return new
                    {
                        success = false,
                        message = $"NodeGraph资产未找到: {assetPath}"
                    };
                }

                // 确保NodeGraph被标记为脏数据
                EditorUtility.SetDirty(nodeGraph);

                // 保存资产
                AssetDatabase.SaveAssets();

                // 刷新资产数据库
                AssetDatabase.Refresh();

                // 等待资产数据库完成刷新
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                return new
                {
                    success = true,
                    message = $"成功保存NodeGraph修改",
                    nodeGraphPath = assetPath,
                    totalFlowEventNodes = nodeGraph.flowEventNodes.Count,
                    totalNodes = GetTotalNodeCount(nodeGraph),
                    saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = $"保存NodeGraph修改失败: {ex.Message}"
                };
            }
        }
    }
}
