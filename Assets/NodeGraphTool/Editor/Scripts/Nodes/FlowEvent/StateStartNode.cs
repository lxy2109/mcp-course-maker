using NodeGraph;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;


public class StateStartNode : NodeBase<StateStartNode>, IEventNode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);

    public string stateName;

    private TextField statetextField;

    public override void OnCreated(NodeGraphView view)
    {
        if (GUID == "") { System.Guid.NewGuid().ToString(); }

        #region Title
        if (string.IsNullOrEmpty(stateName)) title = "章节开始";
        else title = stateName+ "  章节开始";
        titleContainer.style.backgroundColor = new Color(0.0f, 0.4f, 0.1f, 1);
        #endregion
        


        statetextField = new TextField("章节名称", -1, false, false, default);
        statetextField.RegisterValueChangedCallback(evt =>
        {
            stateName = evt.newValue;
            title = stateName + "  章节开始";
        });
        statetextField.SetValueWithoutNotify(stateName);
        mainContainer.Add(statetextField);




        var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = GeneratePort(this, Direction.Output, typeof(string), Port.Capacity.Single);
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(200, 200), _defaultNodeSize));
    }

    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;

        statetextField.SetValueWithoutNotify(stateName);
    }
}
