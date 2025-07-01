using UnityEditor; // 引入Unity编辑器命名空间
using UnityEditor.UIElements; // 引入Unity编辑器UI元素命名空间
using UnityEngine; // 引入Unity引擎核心命名空间
using UnityEngine.UIElements; // 引入Unity UI元素命名空间
using System;
using System.Collections.Generic; // 引入C#系统基础命名空间
using UnityEditor.Experimental.GraphView; // 添加此命名空间引用以解决Node, Port和Edge问题
using System.Linq;  // 添加LINQ命名空间引用

namespace NodeGraph // 定义NodeGraph命名空间
{
    /// <summary>
    /// 节点图编辑器窗口类，提供节点图的可视化编辑和管理功能
    /// </summary>
    public class NodeGraphWindow : EditorWindow // 定义NodeGraphWindow类，继承自EditorWindow
    {
        /// <summary>
        /// 单例模式实例
        /// </summary>
        private static NodeGraphWindow instance = null; // 声明静态单例实例变量，初始为null

        /// <summary>
        /// 窗口默认位置和大小
        /// </summary>
        private static readonly Rect rect = new Rect(200, 150, 800, 600); // 定义窗口的默认位置和尺寸
        
        
        public static NodeGraphWindow _instance
        {
            get { return instance; } 
        }
        

        /// <summary>
        /// 数据信息创建器实例，用于从Excel文件中创建教学步骤数据
        /// </summary>
        private DataInfoCreator dataInfoCreator; // 声明DataInfoCreator实例变量，用于处理Excel数据

        /// <summary>
        /// 显示节点图窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        private static void ShowWindow(string title) // 定义显示窗口的静态方法，接收窗口标题参数
        {
            if (instance == null) // 检查单例实例是否为空
            {
                // 创建新窗口实例
                instance = GetWindow<NodeGraphWindow>(title, true); // 创建NodeGraphWindow类型的窗口实例
                instance.position = rect; // 设置窗口位置和大小为预定义的rect
            }
            else // 如果实例已存在
            {
                // 更新已有窗口
                instance.titleContent = new GUIContent(title); // 更新窗口标题
                instance.rootVisualElement.Clear(); // 清空窗口中的所有可视元素
                instance.CreateGUI(); // 重新创建GUI元素
                instance.Show(); // 显示窗口
            }
        }

        /// <summary>
        /// 窗口启用时注册键盘事件监听
        /// </summary>
        private void OnEnable() // Unity生命周期方法，在窗口启用时调用
        {
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown); // 注册键盘按下事件的回调函数
        }

        /// <summary>
        /// 定期更新Inspector，处理组合和比较设置的变化
        /// </summary>
        private void OnInspectorUpdate() // Unity编辑器回调，用于更新Inspector面板
        {
            if (instance == null) return; // 如果实例为空则返回
            NodeGraphSaveTools.UpdateCombineSettings(instance); // 更新节点组合设置
            NodeGraphSaveTools.UpdateCompareSettings(instance); // 更新节点比较设置
        }

        /// <summary>
        /// 处理键盘事件，如按下Ctrl+S保存
        /// </summary>
        /// <param name="evt">键盘事件</param>
        private void OnKeyDown(KeyDownEvent evt) // 键盘按下事件的处理方法
        {
            // 检测Ctrl+S组合键（在Mac上为Cmd+S）
            if (evt.keyCode == KeyCode.S && (evt.ctrlKey || evt.commandKey)) // 检查是否按下Ctrl+S或Cmd+S
            {
                NodeGraphSaveTools.Save(instance); // 调用保存工具保存当前节点图
                Debug.Log("Save"); // 输出保存日志

                // 防止事件传播
                evt.StopPropagation(); // 阻止事件继续传播到其他处理器
            }
        }

        /// <summary>
        /// 打开节点图窗口并加载指定数据
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="data">要加载的节点图数据</param>
        public static void OpenNodeGraphWindow(string title, NodeGraph data) // 公共静态方法，打开窗口并加载数据
        {
            ShowWindow(title); // 调用ShowWindow方法显示窗口
            instance.m_data = data; // 设置窗口的节点图数据
            NodeGraphSaveTools.Load(instance, data); // 使用保存工具加载节点图数据
        }

        /// <summary>
        /// 节点图视图实例
        /// </summary>
        private NodeGraphView m_NodeGraphView = null; // 声明节点图视图实例变量

        /// <summary>
        /// 节点图视图属性
        /// </summary>
        public NodeGraphView NodeGraphView => m_NodeGraphView; // 定义节点图视图的只读属性

        /// <summary>
        /// 节点图数据
        /// </summary>
        private NodeGraph m_data = null; // 声明节点图数据实例变量

        /// <summary>
        /// 节点图数据属性
        /// </summary>
        public NodeGraph Data => m_data; // 定义节点图数据的只读属性

        /// <summary>
        /// 节点创建窗口内容
        /// </summary>
        private NodeCreateWindowContent m_NodeCreateWindow; // 声明节点创建窗口内容实例变量

        /// <summary>
        /// 创建图形界面
        /// </summary>
        private void CreateGUI() // 创建图形界面的方法
        {
            CreateView(); // 调用创建视图方法
            CreateToolBar(); // 调用创建工具栏方法
        }

        /// <summary>
        /// 创建工具栏
        /// </summary>
        private void CreateToolBar() // 创建工具栏的方法
        {
            var _toolBar = new Toolbar(); // 创建新的工具栏实例

            // 添加保存按钮
            _toolBar.Add(new Button(() => // 添加一个带有点击事件的按钮
            {
                NodeGraphSaveTools.Save(instance); // 点击时保存节点图
                Debug.Log("Save"); // 输出保存日志
            }) { text = "Save Data" }); // 设置按钮文本为"Save Data"
            rootVisualElement.Add(_toolBar); // 将工具栏添加到根视觉元素中

            // 添加快速创建节点按钮
            _toolBar.Add(new Button(() => // 添加一个带有点击事件的按钮
            {
                m_NodeCreateWindow = ScriptableObject.CreateInstance<NodeCreateWindowContent>(); // 创建节点创建窗口内容实例
                m_NodeCreateWindow.Configure(NodeGraphWindow.GetWindow<EditorWindow>(), NodeGraphView.instance); // 配置节点创建窗口
                m_NodeCreateWindow.CreatNode("FlowEventNode"); // 创建一个流程事件节点
            })
            { text = "快速添加Node" }); // 设置按钮文本为"快速添加Node"

            // 添加数据创建工具按钮
            _toolBar.Add(new Button(() => // 添加一个带有点击事件的按钮
            {
                dataInfoCreator = DataInfoCreator.ShowWindow("创建设置"); // 显示数据信息创建器窗口
                Debug.Log(dataInfoCreator); // 输出创建器实例日志
            })
            { text = "快速创建工具" }); // 设置按钮文本为"快速创建工具"

            // 添加节点创建按钮
            _toolBar.Add(new Button(() => // 添加一个带有点击事件的按钮
            {
                // 创建所有语音资源并根据Excel数据生成节点图
                CreatAllVoice(() => { // 调用创建所有语音的方法，并传入完成后的回调
                    
                    
                    // 在创建节点之前，确保 AllUnityEvent 实例存在
                    var allUnityEvent = AllUnityEvent.GetInstanceInEditor();
                    if (allUnityEvent == null)
                    {
                        Debug.LogError("AllUnityEvent instance not found in scene!");
                        return;
                    }

                    // 在添加事件之前，确保字典已初始化
                    if (allUnityEvent.flownodeDic == null)
                    {
                        allUnityEvent.flownodeDic = new Dictionary<string, FlowEventNodeD>();
                    }
                    
                    
                    m_NodeCreateWindow = ScriptableObject.CreateInstance<NodeCreateWindowContent>(); // 创建节点创建窗口内容实例
                    m_NodeCreateWindow.Configure(NodeGraphWindow.GetWindow<EditorWindow>(), NodeGraphView.instance); // 配置节点创建窗口
                    
                    // 创建一个字典来存储节点位置和节点引用
                    Dictionary<Vector2, Node> positionToNodeMap = new Dictionary<Vector2, Node>();

                    int x = 0; // 初始化x坐标变量
                    int y = 0; // 初始化y坐标变量
                    string tempFlowGraph = ""; // 初始化临时步骤名称变量
                    int count = 0; // 初始化计数器
                    Vector2 pos; // 声明位置向量变量
                    
                    // 在所有节点前面创建一个startNode
                    pos = new Vector2(300 * x, 300 * y); // 计算起始位置
                    StartNode startNode = (StartNode)m_NodeCreateWindow.CreatNode("StartNode", pos); // 创建起始节点
                    positionToNodeMap[new Vector2(x, y)] = startNode; // 保存起始节点引用
                    

                    // 遍历数据创建节点，根据步骤名称组织布局
                    for (int i = 0; i < dataInfoCreator.datas.Count; i++) // 遍历所有步骤数据
                    {
                        EditorUtility.DisplayProgressBar("从Excel中生成节点中", "创建节点中", i / dataInfoCreator.datas.Count); // 显示进度条

                        
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
                        
                        
                        // 加载对应的音频资源
                        AudioClip clip = Resources.Load<AudioClip>("Lesson/" + i.ToString() + "_" + dataInfoCreator.datas[i].eventName + "/" + dataInfoCreator.datas[i].voiceName); // 加载对应的音频剪辑

                        // 如果步骤名称变化，创建新的列并添加合并节点
                        if (tempFlowGraph != dataInfoCreator.datas[i].flowGraph) // 检查步骤名称是否变化
                        {
                            y = 0; // 重置y坐标为0

                            if (count > 0) // 如果计数器大于0
                            {
                                x++; // x坐标加1
                                pos = new Vector2(300 * x, 300 * y); // 计算新的位置
                                CombineNode combineNode = (CombineNode) m_NodeCreateWindow.CreatNode("CombineNode", pos); // 在新位置创建合并节点
                                positionToNodeMap[new Vector2(x, y)] = combineNode; // 保存合并节点引用
                                
                                x++;
                                pos = new Vector2(300 * x, 300 * y); // 计算新的位置
                                FlowEventNode flowEventNode = (FlowEventNode)m_NodeCreateWindow.CreatNode("FlowEventNode", pos); // 在新位置创建比较节点
                                positionToNodeMap[new Vector2(x, y)] = flowEventNode; // 保存比较节点引用
                            }

                            x++; // x坐标再加1
                            pos = new Vector2(300 * x, 300 * y); // 计算新的位置
                            FlowEventNode node = (FlowEventNode)m_NodeCreateWindow.CreatNode("FlowEventNode", dataInfoCreator.datas[i], clip, pos); // 创建流程事件节点
                            positionToNodeMap[new Vector2(x, y)] = node; // 保存流程事件节点引用
                            
                            count = 0; // 重置计数器
                            tempFlowGraph = dataInfoCreator.datas[i].flowGraph; // 更新临时步骤名称
                        }
                        else // 如果步骤名称相同
                        {
                            // 相同步骤名称的节点垂直排列
                            y++; // y坐标加1
                            count++; // 计数器加1
                            pos = new Vector2(300 * x, 300 * y); // 计算新的位置
                            FlowEventNode node = (FlowEventNode)m_NodeCreateWindow.CreatNode("FlowEventNode", dataInfoCreator.datas[i], clip, pos); // 创建流程事件节点
                            positionToNodeMap[new Vector2(x, y)] = node; // 保存流程事件节点引用
                        }
                    }

                    EditorUtility.ClearProgressBar(); // 清除进度条
                    
                    // 打印整个字典内容
                    foreach (var entry in positionToNodeMap)
                    {
                        Debug.Log($"Position: {entry.Key}, Node: {entry.Value.GetType().Name}"); // 输出每个节点的位置和类型
                    }
                    
                    ConnectNodes (positionToNodeMap); // 调用连接节点方法，传入位置到节点的映射字典
                });
            })
            { text = "创建节点" }); // 设置按钮文本为"创建节点"
        }
        
        /// <summary>
        /// 连接节点图中所有节点
        /// </summary>
        /// <param name="positionToNodeMap">包含节点位置和节点引用的字典</param>
        private void ConnectNodes(Dictionary<Vector2, Node> positionToNodeMap)
        {
            // 按x坐标分组节点（构建列结构）
            Dictionary<int, Dictionary<int, Node>> columnMap = new Dictionary<int, Dictionary<int, Node>>();
            
            foreach (var entry in positionToNodeMap)
            {
                int x = (int)entry.Key.x;
                int y = (int)entry.Key.y;
                
                if (!columnMap.ContainsKey(x))
                {
                    columnMap[x] = new Dictionary<int, Node>();
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
                
                Dictionary<int, Node> currentColumn = columnMap[x];
                Dictionary<int, Node> nextColumn = columnMap[nextX];
                
                // 当前列节点按y坐标排序
                List<int> yCoordinates = currentColumn.Keys.ToList();
                yCoordinates.Sort();
                
                // 如果当前列只有一个节点
                if (currentColumn.Count == 1)
                {
                    // 获取当前列的唯一节点
                    Node sourceNode = currentColumn[yCoordinates[0]];
                    
                    // 获取下一列的节点（首选y=0的节点）
                    List<int> nextYCoordinates = nextColumn.Keys.ToList();
                    nextYCoordinates.Sort();
                    Node targetNode = nextColumn.ContainsKey(0) ? 
                        nextColumn[0] : 
                        nextColumn[nextYCoordinates[0]];
                    
                    // 连接节点
                    ConnectTwoNodes(sourceNode, targetNode);
                }
                // 如果当前列有多个节点，则下一列应该有CombineNode
                else if (currentColumn.Count > 1)
                {
                    // 查找下一列中的合并节点
                    Node combineNode = null;
                    foreach (var nodeEntry in nextColumn)
                    {
                        if (nodeEntry.Value.GetType().Name.Contains("CombineNode"))
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
                            Node sourceNode = currentColumn[yCoordinates[j]];
                            
                            ConnectTwoNodes(sourceNode, combineNode, null, "Input1");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 连接两个节点的端口
        /// </summary>
        /// <param name="sourceNode">源节点</param>
        /// <param name="targetNode">目标节点</param>
        /// <param name="outputPortName">可选的源节点输出端口名称</param>
        /// <param name="inputPortName">可选的目标节点输入端口名称</param>
        private void ConnectTwoNodes(Node sourceNode, Node targetNode, string outputPortName = null, string inputPortName = null)
        {
            try
            {
                // 确定源节点输出端口名称
                if (string.IsNullOrEmpty(outputPortName))
                {
                    if (sourceNode.GetType().Name.Contains("FlowEventNode"))
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
                
                // 获取源节点的输出端口
                Port sourcePort = null;
                foreach (var child in sourceNode.outputContainer.Children())
                {
                    if (child is Port port && port.portName == outputPortName)
                    {
                        sourcePort = port;
                        break;
                    }
                }
                
                // 获取目标节点的输入端口
                Port targetPort = null;
                foreach (var child in targetNode.inputContainer.Children())
                {
                    if (child is Port port && port.portName == inputPortName)
                    {
                        targetPort = port;
                        break;
                    }
                }
                
                if (sourcePort != null && targetPort != null)
                {
                    // 创建边并添加到图表中
                    Edge edge = sourcePort.ConnectTo(targetPort);
                    m_NodeGraphView.AddElement(edge);
                    
                    // 创建并存储连接数据
                    NodeLinkData linkData = new NodeLinkData
                    {
                        BaseNodeGUID = sourceNode.viewDataKey,
                        OutputPortName = outputPortName,
                        BaseNodeType = sourceNode.GetType().Name,
                        TargetNodeGUID = targetNode.viewDataKey,
                        TargetPortName = inputPortName,
                        TargetNodeType = targetNode.GetType().Name
                    };
                    
                    m_data.Links.Add(linkData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"连接节点失败: {e.Message}");
            }
        }
        

        /// <summary>
        /// 创建所有语音文件，完成后执行回调
        /// </summary>
        /// <param name="action">创建完成后的回调函数</param>
        private void CreatAllVoice(Action action) // 创建所有语音文件的方法
        {
            // dataInfoCreator.CreatVoice(action); // 调用数据信息创建器的创建语音方法
        }

        /// <summary>
        /// 创建节点图视图
        /// </summary>
        private void CreateView() // 创建节点图视图的方法
        {
            var styleSheet = Resources.Load<StyleSheet>("Editor Default Resources/NodeGraphView") as StyleSheet; // 加载节点图视图样式表
            m_NodeGraphView = new NodeGraphView(this, styleSheet); // 创建新的节点图视图实例
            m_NodeGraphView.StretchToParentSize(); // 设置节点图视图大小为父元素大小
            rootVisualElement.Add(m_NodeGraphView); // 将节点图视图添加到根视觉元素中
        }

        /// <summary>
        /// 窗口禁用时清理资源
        /// </summary>
        private void OnDisable() // Unity生命周期方法，在窗口禁用时调用
        {
            m_NodeGraphView = null; // 将节点图视图实例置为null
            m_data = null; // 将节点图数据实例置为null
            instance = null; // 将单例实例置为null
        }

        /// <summary>
        /// 从Excel创建节点（预留方法）
        /// </summary>
        private void CreatNodeFromExcel() // 从Excel创建节点的方法
        {
            // 预留实现
        }
        
        public void AutoSave()
        {
            NodeGraphSaveTools.Save(instance);
        }
        
    }
}

/*
 * NodeGraphWindow类总结:
 *
 * 此类是一个Unity编辑器扩展窗口，用于可视化创建和编辑教学流程节点图。
 *
 * 主要功能：
 * 1. 提供节点图的可视化编辑界面
 * 2. 集成Excel数据导入功能，通过DataInfoCreator从Excel读取步骤数据
 * 3. 支持自动语音生成，将Excel中的文本内容转换为语音文件
 * 4. 自动布局节点，相同步骤名称的节点垂直排列，不同步骤水平分布
 * 5. 支持节点图数据的保存和加载
 *
 * 工作流程：
 * 1. 用户通过"快速创建工具"按钮打开DataInfoCreator，从Excel提取步骤数据
 * 2. 用户点击"创建节点"按钮，系统生成语音文件并创建对应的流程节点
 * 3. 节点自动排列，形成可视化的教学流程图
 * 4. 用户可以编辑连接节点，构建完整的教学流程
 * 5. 通过Ctrl+S或"Save Data"按钮保存节点图
 *
 * 此类是VR教学系统的核心编辑工具，连接Excel数据源与Unity可视化节点系统。
 */