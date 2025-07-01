using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NodeGraph;
using Sirenix.OdinInspector;
using Sirenix.Serialization;


[SerializeField]
public class CombineGroup
{
    public List<FlowEventNodeData> inNodes;
    public FlowEventNodeData outNode;
    public CombineNodeData combineNodeData;
    public bool[] eventsisDone;
}
[SerializeField]
public class StateGroup
{
    public List<FlowEventNodeData> eventNodes;
    public string stateName;

    public StateGroup(List<FlowEventNodeData> eventNodes, string stateName)
    {
        this.eventNodes = eventNodes;
        this.stateName = stateName;
    }
}

public class BaseGraphLoader 
{

    public static List<StateGroup> GetAllStateGroups(NodeGraph.NodeGraph nodeGraph)
    {
        List<StateGroup> stateGroups = new List<StateGroup>();
        var statenodeDatas = nodeGraph.GetTargetNodeDatas<StateStartNodeData>();
        foreach (var statenodeData in statenodeDatas)
        {
            stateGroups.Add(GetNodeGroupinStateNode(nodeGraph, statenodeData.stateName));
        }



        return stateGroups;

    }


    /// <summary>
    /// 获取State Start End区间所有FlowEvent
    /// </summary>
    /// <param name="nodeGraph">Graph</param>
    /// <param name="stateNodeName">State Start Name</param>
    /// <returns></returns>
    public static StateGroup GetNodeGroupinStateNode(NodeGraph.NodeGraph nodeGraph, string stateNodeName)
    {
        var statenodeDatas = nodeGraph.GetTargetNodeDatas<StateStartNodeData>();
        var stateStartNode = statenodeDatas.Find(x => x.stateName == stateNodeName);
        if (stateStartNode == null)
        {
            Debug.Log("Can't FInd Node");
            return null;
        }
        List<FlowEventNodeData> nodeDatas = new List<FlowEventNodeData>();
        List<NodeBaseData> nodeList = new List<NodeBaseData>();
        nodeList = GetNodeListInState(nodeGraph, stateStartNode, ref nodeList);

        var combineGroup = GetAllCombineGroups(nodeGraph);

        foreach (NodeBaseData nodeData in nodeList)
        {
            if (nodeData.GetType() == typeof(FlowEventNodeData))
            {
                if (!nodeDatas.Contains((FlowEventNodeData)nodeData))
                {
                    nodeDatas.Add((FlowEventNodeData)nodeData);
                }
          
            }
            else if (nodeData.GetType() == typeof(CombineNodeData))
            {
                var group = combineGroup.Find(x => x.combineNodeData == nodeData);
                if (group != null)
                {
                    for (int i = 0; i < group.inNodes.Count; i++)
                    {
                        if (!nodeDatas.Contains(group.inNodes[i]))
                        {
                            nodeDatas.Add(group.inNodes[i]);
                        }
                    
                    }
                }
                else
                {
                    continue;
                }
            }
            else { continue; }
        }


        StateGroup stateGroup = new StateGroup(nodeDatas,stateNodeName);

        return stateGroup;
    }



    /// <summary>
    /// 获取 合并 组
    /// </summary>
    /// <param name="nodeGraph">Graph</param>
    /// <returns></returns>
    public static List<CombineGroup> GetAllCombineGroups(NodeGraph.NodeGraph nodeGraph)
    {
        List<CombineGroup> combineGroups = new List<CombineGroup>();
        List<string> guids = new List<string>();


        var nodeDatas = nodeGraph.GetAllNodeDatas();

        //Find All Combine
        foreach (var link in nodeGraph.Links)
        {
            if (typeof(CombineNodeData).Name.Contains(link.BaseNodeType))
            {
                if(!guids.Contains(link.BaseNodeGUID))guids.Add(link.BaseNodeGUID);
            }
            else if (typeof(CombineNodeData).Name.Contains(link.TargetNodeType))
            {
                if (!guids.Contains(link.TargetNodeGUID)) guids.Add(link.TargetNodeGUID);
            }
        }

        for (int i = 0; i < guids.Count; i++)
        {
            CombineNodeData data = (CombineNodeData)nodeDatas.Find(t => t.GUID == guids[i]);
            CombineGroup combineGroup = new CombineGroup();
            combineGroup.inNodes = new List<FlowEventNodeData>();
            combineGroup.combineNodeData = data;
            //尾部包含合并节点
            var inTempLinks = nodeGraph.Links.FindAll(id => id.TargetNodeGUID == guids[i]);
            foreach (var item in inTempLinks)
            {
                FlowEventNodeData tempIN = nodeDatas.Find(t => t.GUID == item.BaseNodeGUID) as FlowEventNodeData;
                combineGroup.inNodes.Add(tempIN);   
            }
            //头部包含合并节点
            var outTempLink = nodeGraph.Links.Find(id => id.BaseNodeGUID == guids[i]);

            FlowEventNodeData tempOut =(FlowEventNodeData) GetTargetNodeFront<FlowEventNodeData>(nodeGraph, data);

           // FlowEventNodeData tempOut = nodeDatas.Find(t => t.GUID == outTempLink.BaseNodeGUID) as FlowEventNodeData;
            combineGroup.outNode = tempOut;
            combineGroup.eventsisDone = new bool[data.inputCount];

            combineGroups.Add(combineGroup);

        }

        return combineGroups;
    }




    /// <summary>
    /// 获取FlowEventNode序列
    /// 主线优先按序号排列 支线在列表尾部
    /// </summary>
    /// <param name="nodeGraph"></param>
    /// <returns></returns>
    public static List<FlowEventNodeData> GetFlowEventNodeDatas(NodeGraph.NodeGraph nodeGraph)
    {
        StartNodeData startNodeData;
        startNodeData = (StartNodeData)nodeGraph.GetAllNodeDatas().Find(node => node.GetType() == typeof(StartNodeData));

        if(startNodeData==null)return null;

        List<FlowEventNodeData> nodesList = new List<FlowEventNodeData>();
        nodesList=GetFlowEventNodeDatasList(nodeGraph, startNodeData,ref nodesList);

        //for (int i = 0; i < nodesList.Count; i++)
        //{
        //    Debug.Log(nodesList[i].eventName);
        //}


        foreach (var node in nodeGraph.GetAllNodeDatas())
        {
            if (node.GetType() == typeof(FlowEventNodeData))
            {
                if (!nodesList.Contains((FlowEventNodeData)node))
                {
                    nodesList.Add((FlowEventNodeData)node);
                }
            }
        }


      

        return nodesList;
    }

    /// <summary>
    /// 获取FlowEventNode主线
    /// </summary>
    /// <param name="nodeGraph"></param>
    /// <param name="baseData"></param>
    /// <param name="nodesList"></param>
    /// <returns></returns>
    public static List<FlowEventNodeData> GetFlowEventNodeDatasList(NodeGraph.NodeGraph nodeGraph, NodeBaseData baseData, ref List<FlowEventNodeData> nodesList)
    {
        if (nodesList == null)
        {
            nodesList = new List<FlowEventNodeData>();
        }
        FlowEventNodeData tempOut = (FlowEventNodeData)GetTargetNodeFront<FlowEventNodeData>(nodeGraph, baseData);

        if (tempOut != null)
        {
            nodesList.Add(tempOut);
            return GetFlowEventNodeDatasList(nodeGraph, tempOut, ref nodesList);
        }
        else
        {
            return nodesList;
        }

    }

    public static List<NodeBaseData> GetNodeListInState(NodeGraph.NodeGraph nodeGraph, NodeBaseData baseData, ref List<NodeBaseData> nodesList)
    {
        if (nodesList == null)
        {
            nodesList = new List<NodeBaseData>();
        }
    
        NodeBaseData tempOut = GetTargetNodeFront<NodeBaseData>(nodeGraph, baseData);

        if (tempOut != null&&tempOut.GetType()!=typeof(StateEndNodeData))
        {
            nodesList.Add(tempOut);
            return GetNodeListInState(nodeGraph, tempOut, ref nodesList);
        }
        else
        {
            return nodesList;
        }
    }



    /// <summary>
    /// 获取指定Node序列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nodeGraph"></param>
    /// <returns></returns>
    public static List<T> GetNodesList<T>(NodeGraph.NodeGraph nodeGraph) where T : NodeBaseData
    { 
        List<T> nodesList = new List<T>();
        List<string> guids = new List<string>();

        var nodeDatas = nodeGraph.GetAllNodeDatas();

        //Get GUIDS
        //Debug.Log(nodeGraph.Links.Count);
        foreach (var link in nodeGraph.Links)
        {
          //  Debug.Log(link.BaseNodeType);
            if (typeof(T).Name.Contains(link.BaseNodeType))
            {
               // Debug.Log(link.BaseNodeGUID);
                if(!guids.Contains(link.BaseNodeGUID))guids.Add(link.BaseNodeGUID);
            }
            if (typeof(T).Name.Contains(link.TargetNodeType))
            {
                //Debug.Log(link.TargetNodeGUID);
                if (!guids.Contains(link.TargetNodeGUID)) guids.Add(link.TargetNodeGUID);
            }
        }

        for (int i = 0; i < guids.Count; i++)
        { 
            nodesList.Add((T)nodeDatas.Find(node => node.GUID==guids[i]));
        }


        return nodesList;
    }

    /// <summary>
    /// 向前获取目标节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nodeGraph">graph</param>
    /// <param name="baseNodeData">基础节点</param>
    /// <returns></returns>
    public static NodeBaseData GetTargetNodeFront<T>(NodeGraph.NodeGraph nodeGraph,NodeBaseData baseNodeData) where T : NodeBaseData
    {
        foreach (var link in nodeGraph.Links)
        {
            // Base
            if (baseNodeData.GUID == link.BaseNodeGUID)
            {
                if(typeof(T).Name.Contains("NodeBase"))
                {
                    return (T)nodeGraph.GetAllNodeDatas().Find(node => node.GUID == link.TargetNodeGUID);
                }

                if (typeof(T).Name.Contains(link.TargetNodeType))
                {
                    return (T)nodeGraph.GetAllNodeDatas().Find(node => node.GUID == link.TargetNodeGUID);
                }
                else
                {                 
                    var tempNode = nodeGraph.GetAllNodeDatas().Find(node => node.GUID == link.TargetNodeGUID);
                    var tempType=tempNode.GetType();
                    return GetTargetNodeFront<T>(nodeGraph, tempNode);
                }
            }

        }
        //Debug.Log("Null");
        return null;
    }


    /// <summary>
    /// 向前获取目标节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nodeGraph"></param>
    /// <param name="baseNodeData">基础节点</param>
    /// <returns></returns>
    public static NodeBaseData GetTargetNodeBack<T>(NodeGraph.NodeGraph nodeGraph, NodeBaseData targetNodeData) where T : NodeBaseData
    {
        foreach (var link in nodeGraph.Links)
        {
            // Base
            if (targetNodeData.GUID == link.TargetNodeGUID)
            {
                if (typeof(T).Name.Contains(link.BaseNodeGUID))
                {
                    return (T)nodeGraph.GetAllNodeDatas().Find(node => node.GUID == link.BaseNodeGUID);
                }
                else
                {
                    var tempNode = nodeGraph.GetAllNodeDatas().Find(node => node.GUID == link.BaseNodeGUID);
                    var tempType = tempNode.GetType();
                    return GetTargetNodeFront<T>(nodeGraph, tempNode);
                }
            }

        }
        Debug.Log("Null");
        return null;
    }


}
