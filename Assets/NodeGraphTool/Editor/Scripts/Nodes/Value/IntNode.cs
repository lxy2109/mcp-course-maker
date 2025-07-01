using System;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;

public class IntNode : NodeBase<IntNode>, IValueNode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
    public int a;
    private IntegerField intField;
    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;
        title = NodeName.Replace("Node", "");

        intField = new IntegerField();
        intField.RegisterValueChangedCallback(evt =>
        {
            a = evt.newValue;
        });
        intField.SetValueWithoutNotify(a);
        mainContainer.Add(intField);

        var inputPort = GeneratePort(this, Direction.Input, typeof(int), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);
        var outputPort = GeneratePort(this, Direction.Output, typeof(int));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));
    }
    public override void OnLoadad(NodeGraphView view)
    {
        intField.SetValueWithoutNotify(a);
    }
}

