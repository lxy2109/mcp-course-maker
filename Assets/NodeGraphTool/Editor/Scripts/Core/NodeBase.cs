/*
 * NodeBase.cs
 *
 * 作用：节点图系统的核心基类，定义了所有节点的共同行为和属性
 *
 * 主要功能：
 * 1. 为每个节点提供唯一标识符(GUID)和类型名称
 * 2. 定义节点生命周期接口(创建和加载)
 * 3. 提供创建端口的通用方法
 *
 * 使用方式：
 * - 所有自定义节点需要继承此基类并实现抽象方法
 * - 通过泛型参数T指定具体的节点类型
 *
 * 此基类是整个节点图系统的基础，确保了所有节点有统一的接口和唯一标识
 */

using System;
using UnityEditor.Experimental.GraphView;

namespace NodeGraph
{
    /// <summary>
    /// 节点基类，所有自定义节点都应继承此类
    /// </summary>
    /// <typeparam name="T">继承此基类的具体节点类型</typeparam>
    public abstract class NodeBase<T> : Node
    {
        /// <summary>
        /// 节点的全局唯一标识符
        /// </summary>
        public string GUID;
        
        /// <summary>
        /// 节点名称，默认为类型的完全限定名
        /// </summary>
        public string NodeName = "NodeBase";

        /// <summary>
        /// 构造函数，初始化GUID和节点名称
        /// </summary>
        public NodeBase()
        {
            GUID = System.Guid.NewGuid().ToString();
            NodeName = typeof(T).FullName;
        }

        /// <summary>
        /// 节点创建时调用的抽象方法，子类必须实现
        /// </summary>
        /// <param name="view">节点所属的图形视图</param>
        public abstract void OnCreated(NodeGraphView view);
        // public abstract void OnCreated(NodeGraphView view, NodeBaseData nodeBaseData);

        /// <summary>
        /// 节点加载时调用的抽象方法，子类必须实现
        /// </summary>
        /// <param name="view">节点所属的图形视图</param>
        public abstract void OnLoadad(NodeGraphView view);

        //void Update

        /// <summary>
        /// 生成节点端口的辅助方法
        /// </summary>
        /// <param name="node">要为其创建端口的节点</param>
        /// <param name="portDir">端口方向(输入/输出)</param>
        /// <param name="type">端口数据类型</param>
        /// <param name="capacity">端口容量(单连接/多连接)</param>
        /// <returns>创建的端口对象</returns>
        protected Port GeneratePort(Node node, Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDir, capacity, type);
        }
    }
}