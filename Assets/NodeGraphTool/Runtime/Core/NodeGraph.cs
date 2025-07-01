/*
 * NodeGraph.cs
 * 
 * 作用：定义可序列化的节点图数据结构，用于存储和管理节点系统的所有数据
 * 
 * 核心功能：
 * 1. 存储各种类型节点的数据集合(开始节点、事件节点、状态节点等)
 * 2. 管理节点之间的连接关系
 * 3. 提供节点数据的添加、查询和清除功能
 * 4. 支持节点分组和组数据的存储
 * 
 * 此类是节点编辑器的数据模型层，作为ScriptableObject可被序列化并存储为资产文件
 * 在运行时提供对节点图结构和数据的访问，支持节点系统的持久化和加载
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace NodeGraph
{
    /// <summary>
    /// 节点图数据容器，作为ScriptableObject可在Unity编辑器中创建和编辑
    /// </summary>
    [CreateAssetMenu(fileName = "NewNodeGraph", menuName = "模板数据/NodeGraph")]
    public class NodeGraph : SerializedScriptableObject
    {
        /// <summary>
        /// 开始节点数据列表
        /// </summary>
        [SerializeField]
        public List<StartNodeData> startNodeDatas = new List<StartNodeData>();

        /// <summary>
        /// 事件节点数据列表
        /// </summary>
        public List<EventNodeData> eventNodes = new List<EventNodeData>();
        
        /// <summary>
        /// 状态节点数据列表
        /// </summary>
        public List<StateNodeData> stateNodes = new List<StateNodeData>();
        
        /// <summary>
        /// 组合节点数据列表
        /// </summary>
        public List<CombineNodeData> combineNodes= new List<CombineNodeData>();
        
        /// <summary>
        /// 比较节点数据列表
        /// </summary>
        public List<CompareNodeData> compareNodes= new List<CompareNodeData>();

        /// <summary>
        /// 流程事件节点数据列表
        /// </summary>
        public List<FlowEventNodeData> flowEventNodes = new List<FlowEventNodeData>();
        
        /// <summary>
        /// 状态结束节点数据列表
        /// </summary>
        public List<StateEndNodeData> stateEndNodeDatas = new List<StateEndNodeData>();
        
        /// <summary>
        /// 状态开始节点数据列表
        /// </summary>
        public List<StateStartNodeData> stateStartNodeDatas = new List<StateStartNodeData>();

        /// <summary>
        /// 浮点数节点数据列表
        /// </summary>
        public List<FloatNodeData> floatNodes = new List<FloatNodeData>();
        
        /// <summary>
        /// 整数节点数据列表
        /// </summary>
        public List<IntNodeData> intNodes = new List<IntNodeData>();
        
        /// <summary>
        /// Vector2节点数据列表
        /// </summary>
        public List<Vector2NodeData> vector2Nodes = new List<Vector2NodeData>();
        
        /// <summary>
        /// Vector3节点数据列表
        /// </summary>
        public List<Vector3NodeData> vector3Nodes = new List<Vector3NodeData>();
        
        /// <summary>
        /// Vector4节点数据列表
        /// </summary>
        public List<Vector4NodeData> vector4Nodes = new List<Vector4NodeData>();
        
        /// <summary>
        /// 颜色节点数据列表
        /// </summary>
        public List<ColorNodeData> colorNodes = new List<ColorNodeData>();

        /// <summary>
        /// 便签节点数据列表
        /// </summary>
        public List<StickyNodeData> stickyNodes = new List<StickyNodeData>();

        /// <summary>
        /// 节点分组数据列表
        /// </summary>
        [SerializeField] private List<GroupSavedData> groupSavedData = new List<GroupSavedData>();

        /// <summary>
        /// 节点连接数据列表
        /// </summary>
        [SerializeField] private List<NodeLinkData> links = new List<NodeLinkData>();
        
        /// <summary>
        /// 获取所有节点连接数据
        /// </summary>
        public List<NodeLinkData> Links => links;

        /// <summary>
        /// 根据类型名称将节点数据添加到对应的列表中
        /// </summary>
        /// <param name="typeName">节点类型名称</param>
        /// <param name="data">节点数据对象</param>
        /// <exception cref="Exception">如果类型名称未定义则抛出异常</exception>
        public void AddNodeToListFromTypeName(string typeName, object data)
        {
            if (String.Equals(typeName,typeof(StartNodeData).FullName))
            {
                startNodeDatas.Add((StartNodeData) data);
            }
            else if (String.Equals(typeName,typeof(EventNodeData).FullName))
            {
                eventNodes.Add((EventNodeData) data);
            }
            else if (String.Equals(typeName, typeof(StateNodeData).FullName))
            {
                stateNodes.Add((StateNodeData)data);
            }
            else if (String.Equals(typeName, typeof(CombineNodeData).FullName))
            {
                combineNodes.Add((CombineNodeData)data);
            }
            else if (String.Equals(typeName, typeof(CompareNodeData).FullName))
            {
                compareNodes.Add((CompareNodeData)data);
            }
            else if (String.Equals(typeName, typeof(FloatNodeData).FullName))
            {
                floatNodes.Add((FloatNodeData)data);
            }
            else if (String.Equals(typeName, typeof(IntNodeData).FullName))
            {
                intNodes.Add((IntNodeData)data);
            }
            else if (String.Equals(typeName, typeof(Vector2NodeData).FullName))
            {
                vector2Nodes.Add((Vector2NodeData)data);
            }
            else if (String.Equals(typeName, typeof(Vector3NodeData).FullName))
            {
                vector3Nodes.Add((Vector3NodeData)data);
            }
            else if (String.Equals(typeName, typeof(Vector4NodeData).FullName))
            {
                vector4Nodes.Add((Vector4NodeData)data);
            }
            else if (String.Equals(typeName, typeof(ColorNodeData).FullName))
            {
                colorNodes.Add((ColorNodeData)data);
            }
            else if (String.Equals(typeName, typeof(FlowEventNodeData).FullName))
            {
                flowEventNodes.Add((FlowEventNodeData)data);
            }
            else if (String.Equals(typeName, typeof(StateEndNodeData).FullName))
            {
                stateEndNodeDatas.Add((StateEndNodeData)data);
            }
            else if (String.Equals(typeName, typeof(StateStartNodeData).FullName))
            {
                stateStartNodeDatas.Add((StateStartNodeData)data);
            }
            else if (String.Equals(typeName, typeof(StickyNodeData).FullName))
            {
                stickyNodes.Add((StickyNodeData)data);
            }
            else
            {
                throw new Exception($"Node Data Type Name : {typeName} is not defined");
            }
        }

        /// <summary>
        /// 获取所有节点数据的合并列表
        /// </summary>
        /// <returns>包含所有类型节点的合并列表</returns>
        public List<NodeBaseData> GetAllNodeDatas()
        {
            var list = new List<NodeBaseData>();
            list = list.Concat(startNodeDatas.Select<StartNodeData, NodeBaseData>(node => (NodeBaseData) node).ToList()).ToList();
            list = list.Concat(eventNodes.Select<EventNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(stateNodes.Select<StateNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(combineNodes.Select<CombineNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(compareNodes.Select<CompareNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();

            list = list.Concat(flowEventNodes.Select<FlowEventNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(stateEndNodeDatas.Select<StateEndNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(stateStartNodeDatas.Select<StateStartNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();

            list = list.Concat(floatNodes.Select<FloatNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(intNodes.Select<IntNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(vector2Nodes.Select<Vector2NodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(vector3Nodes.Select<Vector3NodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(vector4Nodes.Select<Vector4NodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();
            list = list.Concat(colorNodes.Select<ColorNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();

            list = list.Concat(stickyNodes.Select<StickyNodeData, NodeBaseData>(node => (NodeBaseData)node).ToList()).ToList();

            return list;
        }

        /// <summary>
        /// 获取指定类型的节点数据列表
        /// </summary>
        /// <typeparam name="T">目标节点类型</typeparam>
        /// <returns>指定类型的节点列表</returns>
        public List<T> GetTargetNodeDatas<T>() where T : NodeBaseData
        {
            List<NodeBaseData> nodes = GetAllNodeDatas();

            var list = new List<T>();
            foreach (var node in nodes)
            {
                if (node.GetType() == typeof(T))
                {
                    list.Add((T)node);
                }
            }
            return list;
        }

        /// <summary>
        /// 清除所有节点数据和连接数据
        /// </summary>
        public void ClearAllNodeDatas()
        {
            startNodeDatas.Clear();
            eventNodes.Clear();
            stateNodes.Clear();
            combineNodes.Clear();
            compareNodes.Clear();
            floatNodes.Clear();
            intNodes.Clear();
            vector2Nodes.Clear();
            vector3Nodes.Clear();
            vector4Nodes.Clear();
            vector4Nodes.Clear(); // 注意：这里清除了两次vector4Nodes
            colorNodes.Clear();

            flowEventNodes.Clear();
            stateEndNodeDatas.Clear();
            stateStartNodeDatas.Clear();

            stickyNodes.Clear();

            links.Clear();
            groupSavedData.Clear();
        }

        /// <summary>
        /// 添加节点分组数据
        /// </summary>
        /// <param name="data">分组数据</param>
        public void AddGroup(GroupSavedData data)
        {
            groupSavedData.Add(data);
        }
        
        /// <summary>
        /// 获取所有节点分组数据
        /// </summary>
        /// <returns>分组数据列表</returns>
        public List<GroupSavedData> GetAllGroup()
        {
            List<GroupSavedData> datas=new List<GroupSavedData>();
            foreach (var data in groupSavedData)
            {
                datas.Add(data);
            }
            return datas;
        }
        
        /// <summary>
        /// 清除所有节点分组数据
        /// </summary>
        public void ClearAllGroups()
        {
            groupSavedData.Clear();
        }

        /// <summary>
        /// 当对象被启用时初始化流事件节点
        /// </summary>
        private void OnEnable()
        {
            foreach (var floweventnode in flowEventNodes)
            {
                floweventnode.Init();
            }
        }
    }

    /// <summary>
    /// 节点连接数据类，存储两个节点之间的连接信息
    /// </summary>
    [Serializable]
    public class NodeLinkData
    {
        public string BaseNodeGUID;
        
        public string OutputPortName;
        
        public string BaseNodeType;
        
        public string TargetNodeGUID;
        
        public string TargetPortName;
        
        public string TargetNodeType;
    }
    
    /// <summary>
    /// 节点分组保存数据，存储分组信息及其包含的节点
    /// </summary>
    [Serializable]
    public class GroupSavedData
    {
        /// <summary>
        /// 分组的GUID
        /// </summary>
        public string GUID;
        
        /// <summary>
        /// 分组标题
        /// </summary>
        public string title;
        
        /// <summary>
        /// 分组X坐标位置
        /// </summary>
        public float posX;
        
        /// <summary>
        /// 分组Y坐标位置
        /// </summary>
        public float posY;
        
        /// <summary>
        /// 分组宽度
        /// </summary>
        public float width;
        
        /// <summary>
        /// 分组高度
        /// </summary>
        public float height;
        
        /// <summary>
        /// 分组内节点的GUID列表
        /// </summary>
        public List<string> nodeGuids = new List<string>();

        /// <summary>
        /// 分组数据构造函数
        /// </summary>
        /// <param name="gUID">分组GUID</param>
        /// <param name="title">标题</param>
        /// <param name="posX">X坐标</param>
        /// <param name="posY">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="nodeGuids">节点GUID列表</param>
        public GroupSavedData(string gUID, string title, float posX, float posY, float width, float height, List<string> nodeGuids)
        {
            GUID = gUID;
            this.title = title;
            this.posX = posX;
            this.posY = posY;
            this.width = width;
            this.height = height;
            this.nodeGuids = nodeGuids;
        }
    }
    
    
    
}