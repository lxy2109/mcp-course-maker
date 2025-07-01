using System;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;

public class Vector3Node : NodeBase<Vector3Node>, IValueNode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
    public Vector3 a;
    private Vector3Field vector3Field;
    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;
        title = NodeName.Replace("Node", "");

        vector3Field = new Vector3Field();
        vector3Field.RegisterValueChangedCallback(evt =>
        {
            a = evt.newValue;
        });
        vector3Field.SetValueWithoutNotify(a);
        mainContainer.Add(vector3Field);

        var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);
        var outputPort = GeneratePort(this, Direction.Output, typeof(string));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));
    }
    public override void OnLoadad(NodeGraphView view)
    {
        vector3Field.SetValueWithoutNotify(a);
    }
}


