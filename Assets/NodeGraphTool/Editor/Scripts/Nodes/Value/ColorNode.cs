using System;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;

public class ColorNode : NodeBase<ColorNode>, IValueNode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
    public Color a;
    private ColorField colorField;

    public bool isHdr;
    private Toggle boolField;

    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;
        title = NodeName.Replace("Node", "");

      

        colorField = new ColorField();
        colorField.showAlpha = true;
    
        colorField.RegisterValueChangedCallback(evt =>
        {
            a = evt.newValue;
        });
        colorField.SetValueWithoutNotify(a);
        mainContainer.Add(colorField);

        boolField = new Toggle("HDR");
        boolField.RegisterValueChangedCallback(evt =>
        {
            isHdr = evt.newValue;
            colorField.hdr = isHdr;
        });
        boolField.SetValueWithoutNotify(isHdr);
        boolField.style.alignSelf = Align.Center;
        boolField.labelElement.style.left = 120;

        titleButtonContainer.Add(boolField);

        var inputPort = GeneratePort(this, Direction.Input, typeof(Color), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);
        var outputPort = GeneratePort(this, Direction.Output, typeof(Color));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));
    }
    public override void OnLoadad(NodeGraphView view)
    {
        colorField.SetValueWithoutNotify(a);
        boolField.SetValueWithoutNotify(isHdr);
        colorField.hdr = isHdr;
    }
}

