using System;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;


public class FloatNode : NodeBase<FloatNode>, IValueNode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
    public float a;
    private FloatField floatField;
    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;
        title = NodeName.Replace("Node","");

        floatField = new FloatField();
        floatField.RegisterValueChangedCallback(evt =>
        {
            a = evt.newValue;
        });
        floatField.SetValueWithoutNotify(a);
       mainContainer.Add(floatField);

        var inputPort = GeneratePort(this, Direction.Input, typeof(float), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);
        var outputPort = GeneratePort(this, Direction.Output, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));
    }
    public override void OnLoadad(NodeGraphView view)
    {
        floatField.SetValueWithoutNotify(a);
    }
}
