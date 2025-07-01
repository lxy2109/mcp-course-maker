using System;
using UnityEngine;
using NodeGraph;

    [Serializable]
    public class StartNodeData: NodeBaseData
    {
        public StartNodeData()
        {
            NodeName = "NodeGraph.StartNode";
            Position = new Rect(200, 200, 150, 100);
        }
    }
