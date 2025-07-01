using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using NodeGraph;


    public class StartNode :NodeBase<StartNode>, IBaseNode
    {
        public readonly Vector2 _defaultNodeSize = new Vector2(150,200);

        public override void OnCreated(NodeGraphView view)
        {
            if (GUID == "") { System.Guid.NewGuid().ToString(); }

            title = "Start";
            var outputPort = GeneratePort(this, Direction.Output, typeof(string));
            outputPort.portName = "Output";
            outputContainer.Add(outputPort);
        
            RefreshExpandedState();
            RefreshPorts();
            SetPosition(new Rect(new Vector2(200,200),_defaultNodeSize));
        }

        public override void OnLoadad(NodeGraphView view)
        {
        }
    }
