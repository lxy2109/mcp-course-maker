using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using NodeGraph;
using System.Reflection;
using System.IO;
using UnityEngine.Timeline;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityMCP.Editor.Commands
{
    public static class NodeCommandHandler
    {
        private static NodeCreateWindowContent m_NodeCreateWindow;


        public static object CreateNodeGraph(JObject @params)//创建一个NodeGraph并打开它
        {
            try
            {
                // ��JSON�����л�ȡ�ļ���
                string fileName = @params["fileName"]?.ToString();
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "NewNodeGraph"; //
                }

                // ��֤�ļ����Ƿ�Ϸ�
                fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

                // ����NodeGraphʵ��
                NodeGraph.NodeGraph data = ScriptableObject.CreateInstance<NodeGraph.NodeGraph>();

                // ��ȡ��ǰѡ��·��
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (string.IsNullOrEmpty(path))
                {
                    path = "Assets";
                }
                else if (!Directory.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }

                // ȷ��Ŀ���ļ��д���
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fullPath = $"{path}/{fileName}.asset";
#if UNITY_EDITOR
                fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                // �����ͱ����ʲ�
                AssetDatabase.CreateAsset(data, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = data;
                
#endif
                // ���ش����ɹ�����Ϣ
                return new
                {
                    success = true,
                    path = fullPath,
                    asset = data
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create NodeGraph: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }

        }

        public static object CreateStartNode(JObject @params)
        {
            try
            {
                m_NodeCreateWindow = ScriptableObject.CreateInstance<NodeCreateWindowContent>();
                m_NodeCreateWindow.Configure(NodeGraphWindow.GetWindow<EditorWindow>(), NodeGraphView.instance);
                var node =  m_NodeCreateWindow.CreatNode("StartNode") as StartNode;

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找第一个FlowEventNode
                var flowNode = view.nodes.ToList().FirstOrDefault(n => n.GetType().Name == "FlowEventNode" && n.GetType().GetField("eventName")?.GetValue(n) is string en && !string.IsNullOrEmpty(en));
                if (flowNode != null)
                {
                    var flowEventName = flowNode.GetType().GetField("eventName")?.GetValue(flowNode) as string;
                    // 查找端口
                    var outputPort = node.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "Output");
                    var inputPort = flowNode.inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "Input");

                    if (outputPort != null && inputPort != null)
                    {
                        // 创建连线
                        var edge = new Edge
                        {
                            output = outputPort,
                            input = inputPort
                        };
                        edge.input.Connect(edge);
                        edge.output.Connect(edge);
                        view.Add(edge);
                    }
                }

                NodeGraphWindow._instance.AutoSave();
                
                return new
                {
                    success = true,
                    nodeType = "StartNode"
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create StartNode: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        public static object CreateCombineNode(JObject @params)
        {
            try
            {
                string description = @params["Description"]?.ToString();
                // 解析 description，提取输入 eventName 和输出 eventName
                List<string> otherEventNames = new List<string>();
                string outputTargetEventName = null;

                if (!string.IsNullOrEmpty(description))
                {
                    // 格式：用于等待A,B,C完成，输出到D
                    if (description.StartsWith("用于等待") && description.Contains("完成"))
                    {
                        int start = 4;
                        int end = description.IndexOf("完成");
                        string eventList = description.Substring(start, end - start);
                        otherEventNames = eventList.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(e => e.Trim()).ToList();
                        int outputIdx = description.IndexOf("输出到");
                        if (outputIdx > 0)
                        {
                            outputTargetEventName = description.Substring(outputIdx + 3).Trim();
                        }
                    }
                }

                int inputCount = otherEventNames.Count > 0 ? otherEventNames.Count : 2;
                m_NodeCreateWindow = ScriptableObject.CreateInstance<NodeCreateWindowContent>();
                m_NodeCreateWindow.Configure(NodeGraphWindow.GetWindow<EditorWindow>(), NodeGraphView.instance);
                // m_NodeCreateWindow.CreatCombineNode("CombineNode", description, inputCount);


                // 直接调用AutoConnectCombineNode进行自动连接
                AutoConnectCombineNode(new JObject());

                NodeGraphWindow._instance.AutoSave();
                return new
                {
                    success = true,
                    nodeType = "CombineNode",
                    description = description
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create CombineNode: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        public static object CreateFlowEventNode(JObject @params)
        {
            try
            {
                // 1. 解析参数
                
                string eventname = @params["eventname"]?.ToString();
                string partcontext = @params["partcontext"]?.ToString();
                string startevent = @params["startevent"]?.ToString();
                string endevent = @params["endevent"]?.ToString();
                string clipName = @params["clip"]?.ToString();
                int actionInt = @params["action"]?.ToObject<int>() ?? 0;
                var action = (EventEndAction)actionInt;

                // 2. 查找AudioClip
                AudioClip clip = null;
                if (!string.IsNullOrEmpty(clipName))
                {
                    string[] guids = AssetDatabase.FindAssets($"{clipName} t:AudioClip");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }

                // 3. 查找GameObject列表
                List<GameObject> heighLight = new List<GameObject>();
                if (@params["heighLight"] is JArray heighLightArray)
                {
                    foreach (var item in heighLightArray)
                    {
                        string goName = item.ToString();
#if UNITY_EDITOR
                        string[] guids = AssetDatabase.FindAssets($"{goName} t:GameObject");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (go != null) heighLight.Add(go);
                        }
#endif
                    }
                }

                // 4. 查找TimelineAsset列表
                List<TimelineAsset> timeLine = new List<TimelineAsset>();
                if (@params["timeLine"] is JArray timeLineArray)
                {
                    foreach (var item in timeLineArray)
                    {
                        string tlName = item.ToString();
#if UNITY_EDITOR
                        string[] guids = AssetDatabase.FindAssets($"{tlName} t:TimelineAsset");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            var tl = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
                            if (tl != null) timeLine.Add(tl);
                        }
#endif
                    }
                }

                // 5. 调用你的创建方法
                m_NodeCreateWindow = ScriptableObject.CreateInstance<NodeCreateWindowContent>();
                m_NodeCreateWindow.Configure(NodeGraphWindow.GetWindow<EditorWindow>(), NodeGraphView.instance);

                // m_NodeCreateWindow.CreatFlowEventNode(
                //     "FlowEventNode", eventname, heighLight, timeLine, partcontext, clip, startevent, endevent, action
                // );

                NodeGraphWindow._instance.AutoSave();

                return new
                {
                    success = true,
                    nodeType = "FlowEventNode"
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create FlowEventNode: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        public static object ConnectNodes(JObject @params)//连接floweventnode
        {
            try
            {
                string outputNodeEventname = @params["outputNodeEventname"]?.ToString();
                string outputPortName = @params["outputPortName"]?.ToString();
                string inputNodeEventname = @params["inputNodeEventname"]?.ToString();
                string inputPortName = @params["inputPortName"]?.ToString();

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找节点
                var outputNode = view.nodes.ToList().FirstOrDefault(n =>
                {
                    var guidField = n.GetType().GetField("eventName");
                    return guidField != null && (string)guidField.GetValue(n) == outputNodeEventname;
                });
                var inputNode = view.nodes.ToList().FirstOrDefault(n =>
                {
                    var guidField = n.GetType().GetField("eventName");
                    return guidField != null && (string)guidField.GetValue(n) == inputNodeEventname;
                });

                if (outputNode == null || inputNode == null)
                    throw new Exception("Cannot find nodes by eventname!");

                // 查找端口
                var outputPort = outputNode.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == outputPortName);
                var inputPort = inputNode.inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == inputPortName);

                if (outputPort == null || inputPort == null)
                    throw new Exception("Cannot find ports by name!");

                // 创建连线
                var edge = new Edge
                {
                    output = outputPort,
                    input = inputPort
                };
                edge.input.Connect(edge);
                edge.output.Connect(edge);
                view.Add(edge);

                return new { success = true };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect nodes: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }

        public static object GetAllNodesInfo(JObject @params)//获取所有节点信息
        {
            try
            {
                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                var nodes = view.nodes.ToList();
                var nodeInfos = new List<object>();
                foreach (var node in nodes)
                {
                    var nodeType = node.GetType();
                    var guidField = nodeType.GetField("GUID");
                    var eventNameField = nodeType.GetField("eventName");
                    var descriptionField = nodeType.GetField("content");
                    string guid = guidField != null ? (string)guidField.GetValue(node) : null;
                    string eventName = eventNameField != null ? (string)eventNameField.GetValue(node) : null;
                    string description = descriptionField != null ? (string)descriptionField.GetValue(node) : null;
                    string typeName = nodeType.Name;

                    // 获取端口信息
                    var inputPorts = node.inputContainer.Query<Port>().ToList()
                        .Select(p => p.portName).ToList();
                    var outputPorts = node.outputContainer.Query<Port>().ToList()
                        .Select(p => p.portName).ToList();

                    nodeInfos.Add(new
                    {
                        guid,
                        eventName,
                        type = typeName,
                        inputPorts,
                        outputPorts,
                        description
                    });
                }
                return new { success = true, nodes = nodeInfos };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get all nodes info: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }

        public static object SaveNodeGraph()//一键保存
        {
            try
            {
                NodeGraphWindow._instance.AutoSave();
                return new
                {
                    success = true,
                    nodeType = "Save "
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        public static object AutoConnectCombineNode(JObject @params)
        {
            try
            {
                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                var nodes = view.nodes.ToList();

                foreach (var node in nodes)
                {
                    var nodeType = node.GetType();
                    if (nodeType.Name != "CombineNode") continue;

                    var descriptionField = nodeType.GetField("description");
                    string description = descriptionField != null ? (string)descriptionField.GetValue(node) : null;
                    if (string.IsNullOrEmpty(description)) continue;

                    // 解析description，提取输入eventname和输出eventname
                    // 格式：用于等待A,B完成，输出到C
                    var inputEventNames = new List<string>();
                    string outputTargetEventName = null;
                    if (description.StartsWith("用于等待") && description.Contains("完成"))
                    {
                        int start = 4;
                        int end = description.IndexOf("完成");
                        string eventList = description.Substring(start, end - start);
                        inputEventNames = eventList.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(e => e.Trim()).ToList();
                        // 检查是否有输出目标
                        int outputIdx = description.IndexOf("输出到");
                        if (outputIdx > 0)
                        {
                            outputTargetEventName = description.Substring(outputIdx + 3).Trim();
                        }
                    }

                    // 依次查找FlowEventNode并连线（输入）
                    int inputIndex = 1;
                    foreach (var eventName in inputEventNames)
                    {
                        var flowNode = nodes.FirstOrDefault(n =>
                        {
                            var enField = n.GetType().GetField("eventName");
                            return enField != null && (string)enField.GetValue(n) == eventName;
                        });
                        if (flowNode == null) continue;
                        var outputPort = flowNode.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "Output1");
                        var inputPort = node.inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == $"Input{inputIndex}");
                        if (outputPort != null && inputPort != null)
                        {
                            var edge = new Edge
                            {
                                output = outputPort,
                                input = inputPort
                            };
                            edge.input.Connect(edge);
                            edge.output.Connect(edge);
                            view.Add(edge);
                        }
                        inputIndex++;
                    }

                    // 新增：自动将CombineNode的Output连到目标eventname节点的Input
                    if (!string.IsNullOrEmpty(outputTargetEventName))
                    {
                        var targetNode = nodes.FirstOrDefault(n =>
                        {
                            var enField = n.GetType().GetField("eventName");
                            return enField != null && (string)enField.GetValue(n) == outputTargetEventName;
                        });
                        if (targetNode != null)
                        {
                            var combineOutputPort = node.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "Output");
                            var targetInputPort = targetNode.inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == "Input");
                            if (combineOutputPort != null && targetInputPort != null)
                            {
                                var edge = new Edge
                                {
                                    output = combineOutputPort,
                                    input = targetInputPort
                                };
                                edge.input.Connect(edge);
                                edge.output.Connect(edge);
                                view.Add(edge);
                            }
                        }
                    }
                }

                return new { success = true };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to auto connect CombineNode: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }//根据描述自动连接combinenode与其他的东西

        //给指定的节点添加高亮物体
        public static object AddHighlightObjectsToFlowEventNode(JObject @params)
        {
            try
            {
                string eventName = @params["eventName"]?.ToString();
                if (string.IsNullOrEmpty(eventName))
                {
                    throw new Exception("Event name is required!");
                }

                // 获取要添加的物体名称列表
                List<string> objectNames = new List<string>();
                if (@params["objectNames"] is JArray objectNamesArray)
                {
                    foreach (var item in objectNamesArray)
                    {
                        objectNames.Add(item.ToString());
                    }
                }

                if (objectNames.Count == 0)
                {
                    throw new Exception("No object names provided!");
                }

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找目标 FlowEventNode
                var targetNode = view.nodes.ToList().FirstOrDefault(n =>
                {
                    var enField = n.GetType().GetField("eventName");
                    return enField != null && (string)enField.GetValue(n) == eventName;
                });

                if (targetNode == null)
                    throw new Exception($"Cannot find FlowEventNode with event name: {eventName}");

                // 获取节点的 selectableObects 字段
                var selectableObectsField = targetNode.GetType().GetField("selectableObects");
                if (selectableObectsField == null)
                    throw new Exception("Cannot find selectableObects field in FlowEventNode");

                var selectableObects = selectableObectsField.GetValue(targetNode) as List<GameObject>;
                if (selectableObects == null)
                {
                    selectableObects = new List<GameObject>();
                    selectableObectsField.SetValue(targetNode, selectableObects);
                }

                // 获取节点的 selectableObectsID 字段
                var selectableObectsIDField = targetNode.GetType().GetField("selectableObectsID");
                if (selectableObectsIDField == null)
                    throw new Exception("Cannot find selectableObectsID field in FlowEventNode");

                var selectableObectsID = selectableObectsIDField.GetValue(targetNode) as List<GameObjectID>;
                if (selectableObectsID == null)
                {
                    selectableObectsID = new List<GameObjectID>();
                    selectableObectsIDField.SetValue(targetNode, selectableObectsID);
                }

                // 查找并添加物体
                foreach (var objectName in objectNames)
                {
                    string[] guids = AssetDatabase.FindAssets($"{objectName} t:GameObject");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (go != null)
                        {
                            selectableObects.Add(go);
                            GameObjectID goID = new GameObjectID
                            {
                                ID = go.GetInstanceID(),
                                name = go.name
                            };
                            selectableObectsID.Add(goID);
                        }
                    }
                }

                // 刷新节点视图
                targetNode.RefreshExpandedState();
                targetNode.RefreshPorts();
                NodeGraphWindow._instance.AutoSave();
                return new
                {
                    success = true,
                    message = $"Successfully added {objectNames.Count} objects to FlowEventNode: {eventName}"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add highlight objects: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }


        //给指定的节点添加音频（支持模糊查找）
        public static object AddAudioToFlowEventNode(JObject @params)
        {
            try
            {
                string eventName = @params["eventName"]?.ToString();
                string audioName = @params["audioName"]?.ToString();

                if (string.IsNullOrEmpty(eventName))//防止传入错误
                {
                    throw new Exception("Event name is required!");
                }

                if (string.IsNullOrEmpty(audioName))
                {
                    throw new Exception("Audio name is required!");
                }

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找目标 FlowEventNode
                var targetNode = view.nodes.ToList().FirstOrDefault(n =>
                {
                    var enField = n.GetType().GetField("eventName");
                    return enField != null && (string)enField.GetValue(n) == eventName;
                });

                if (targetNode == null)
                    throw new Exception($"Cannot find FlowEventNode with event name: {eventName}");

                // 在指定路径下模糊查找音频文件
                string audioDir = "Assets/Resources/Course/Audio";
                string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioDir });
                string foundPath = null;
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (fileName.Contains(audioName))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(foundPath))
                {
                    throw new Exception($"Cannot find audio file containing: {audioName}");
                }

                AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(foundPath);
                if (audioClip == null)
                {
                    throw new Exception($"Cannot load audio file: {foundPath}");
                }

                // 获取节点的 inAudioClip 字段
                var inAudioClipField = targetNode.GetType().GetField("inAudioClip");
                if (inAudioClipField == null)
                    throw new Exception("Cannot find inAudioClip field in FlowEventNode");

                // 设置音频
                inAudioClipField.SetValue(targetNode, audioClip);

                // 刷新节点视图
                targetNode.RefreshExpandedState();
                targetNode.RefreshPorts();
                NodeGraphWindow._instance.AutoSave();
                return new
                {
                    success = true,
                    message = $"Successfully added audio {System.IO.Path.GetFileName(foundPath)} to FlowEventNode: {eventName}"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add audio: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        //给指定的节点添加timeline（支持批量添加，timeline名称模糊查找）
        public static object AddTimelinesToFlowEventNode(JObject @params)
        {
            try
            {
                string eventName = @params["eventName"]?.ToString();
                if (string.IsNullOrEmpty(eventName))
                {
                    throw new Exception("Event name is required!");
                }

                // 获取要添加的timeline名称列表
                List<string> timelineNames = new List<string>();
                if (@params["timelineNames"] is JArray timelineNamesArray)
                {
                    foreach (var item in timelineNamesArray)
                    {
                        timelineNames.Add(item.ToString());
                    }
                }

                if (timelineNames.Count == 0)
                {
                    throw new Exception("No timeline names provided!");
                }

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找目标 FlowEventNode
                var targetNode = view.nodes.ToList().FirstOrDefault(n =>
                {
                    var enField = n.GetType().GetField("eventName");
                    return enField != null && (string)enField.GetValue(n) == eventName;
                });

                if (targetNode == null)
                    throw new Exception($"Cannot find FlowEventNode with event name: {eventName}");

                // 获取节点的 timelineAssets 字段
                var timelineAssetsField = targetNode.GetType().GetField("timelineAssets");
                if (timelineAssetsField == null)
                    throw new Exception("Cannot find timelineAssets field in FlowEventNode");

                var timelineAssets = timelineAssetsField.GetValue(targetNode) as List<UnityEngine.Timeline.TimelineAsset>;
                if (timelineAssets == null)
                {
                    timelineAssets = new List<UnityEngine.Timeline.TimelineAsset>();
                    timelineAssetsField.SetValue(targetNode, timelineAssets);
                }

                // 在指定路径下模糊查找timeline文件并添加
                string timelineDir = "Assets/Resources/Course/Timeline";
                string[] guids = AssetDatabase.FindAssets("t:TimelineAsset", new[] { timelineDir });
                List<string> addedTimelines = new List<string>();
                foreach (var timelineName in timelineNames)
                {
                    string foundPath = null;
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        if (fileName.Contains(timelineName))
                        {
                            foundPath = path;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(foundPath))
                    {
                        var timelineAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Timeline.TimelineAsset>(foundPath);
                        if (timelineAsset != null)
                        {
                            timelineAssets.Add(timelineAsset);
                            addedTimelines.Add(System.IO.Path.GetFileName(foundPath));
                        }
                    }
                }

                // 刷新节点视图
                targetNode.RefreshExpandedState();
                targetNode.RefreshPorts();
                NodeGraphWindow._instance.AutoSave();
                return new
                {
                    success = true,
                    message = $"Successfully added {addedTimelines.Count} timelines to FlowEventNode: {eventName}",
                    added = addedTimelines
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add timelines: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        // 设置指定FlowEventNode的endActionEnum
        public static object SetFlowEventNodeEndAction(JObject @params)
        {
            try
            {
                string eventName = @params["eventName"]?.ToString();
                if (string.IsNullOrEmpty(eventName))
                {
                    throw new Exception("Event name is required!");
                }
                var endActionObj = @params["endAction"];
                if (endActionObj == null)
                {
                    throw new Exception("endAction is required!");
                }

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找目标 FlowEventNode
                var targetNode = view.nodes.ToList().FirstOrDefault(n =>
                {
                    var enField = n.GetType().GetField("eventName");
                    return enField != null && (string)enField.GetValue(n) == eventName;
                });

                if (targetNode == null)
                    throw new Exception($"Cannot find FlowEventNode with event name: {eventName}");

                // 获取endActionEnum字段
                var endActionField = targetNode.GetType().GetField("endActionEnum");
                if (endActionField == null)
                    throw new Exception("Cannot find endActionEnum field in FlowEventNode");

                // 设置endActionEnum
                Type enumType = endActionField.FieldType;
                object enumValue = null;
                if (endActionObj.Type == JTokenType.Integer)
                {
                    enumValue = Enum.ToObject(enumType, endActionObj.ToObject<int>());
                }
                else if (endActionObj.Type == JTokenType.String)
                {
                    enumValue = Enum.Parse(enumType, endActionObj.ToString());
                }
                else
                {
                    throw new Exception("endAction must be int or string");
                }
                endActionField.SetValue(targetNode, enumValue);

                // 同步UI控件
                var endActionEnumField = targetNode.GetType().GetField("endActionEnumField");
                if (endActionEnumField != null)
                {
                    var enumFieldObj = endActionEnumField.GetValue(targetNode);
                    if (enumFieldObj != null)
                    {
                        var setValueMethod = enumFieldObj.GetType().GetMethod("SetValueWithoutNotify");
                        if (setValueMethod != null)
                        {
                            setValueMethod.Invoke(enumFieldObj, new object[] { enumValue });
                        }
                    }
                }

                NodeGraphWindow._instance.AutoSave();
                return new { success = true, message = $"Set endActionEnum for {eventName} to {enumValue}" };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set endActionEnum: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// 查找空的CombineNode并添加描述和自动连接
        /// </summary>
        /// <param name="description">要添加的描述，格式：用于等待A,B,C完成，输出到D</param>
        public static object FillEmptyCombineNode(JObject @params)
        {
            try
            {
                string description = @params["Description"]?.ToString();
                if (string.IsNullOrEmpty(description))
                {
                    throw new Exception("Description is required!");
                }

                var view = NodeGraphView.instance;
                if (view == null)
                    throw new Exception("NodeGraphView instance is null!");

                // 查找所有CombineNode
                var nodes = view.nodes.ToList();
                var emptyCombineNode = nodes.FirstOrDefault(n =>
                {
                    var nodeType = n.GetType();
                    if (nodeType.Name != "CombineNode") return false;

                    var descriptionField = nodeType.GetField("description");
                    string nodeDescription = descriptionField != null ? (string)descriptionField.GetValue(n) : null;
                    return string.IsNullOrEmpty(nodeDescription);
                });

                if (emptyCombineNode == null)
                {
                    throw new Exception("No empty CombineNode found!");
                }

                List<string> otherEventNames = new List<string>();
                string outputTargetEventName = null;

                if (!string.IsNullOrEmpty(description))
                {
                    // 格式：用于等待A,B,C完成，输出到D
                    if (description.StartsWith("用于等待") && description.Contains("完成"))
                    {
                        int start = 4;
                        int end = description.IndexOf("完成");
                        string eventList = description.Substring(start, end - start);
                        otherEventNames = eventList.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(e => e.Trim()).ToList();
                        int outputIdx = description.IndexOf("输出到");
                        if (outputIdx > 0)
                        {
                            outputTargetEventName = description.Substring(outputIdx + 3).Trim();
                        }
                    }
                }

                int inputCount = otherEventNames.Count > 0 ? otherEventNames.Count : 2;

                CombineNode node =  emptyCombineNode as CombineNode;
                node.description = description;
                node.inputCount = inputCount;

                AutoConnectCombineNode(new JObject());

                NodeGraphWindow._instance.AutoSave();
                return new
                {
                    success = true,
                    message = $"Successfully filled empty CombineNode with description: {description}"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to fill empty CombineNode: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }
    }
}