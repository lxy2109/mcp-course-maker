using System;
using System.Collections;
using System.Collections.Generic;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;

public class CombineNode : NodeBase<CombineNode>, INode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);

    public string description;
    //public List<bool> eventisDone=new List<bool>();


   // private List<Toggle> toggles = new List<Toggle>();
    private TextField textField;

    public int inputCount = 2;

    

    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;
        title = "Combine";

        //debug
        //var guidLable = new Label(GUID);
        ////guidLable.style.color = new Color(0.6f, 0.2f, 0.2f, 1);
        //guidLable.style.backgroundColor = new Color(0.2f, 0.5f, 0.2f, 1);
        //mainContainer.Add(guidLable);
        //

        textField = new TextField("Description", -1, true, false, default);
        textField.RegisterValueChangedCallback(evt =>
        {
            description = evt.newValue;
        });
        textField.SetValueWithoutNotify(name);
        textField.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        mainContainer.Add(textField);

       


        for (int i = 0; i < inputCount; i++)
        {
            var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Multi);
            inputPort.portName = "Input" + (i + 1).ToString();
            inputContainer.Add(inputPort);
        }

        var outputPort = GeneratePort(this, Direction.Output, typeof(string));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        #region Add InputPort 
        Action addoutput = () =>
        {
            inputCount++;
            var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
            inputPort.portName = "Input"+ inputCount.ToString();
            inputContainer.Add(inputPort);
            RefreshExpandedState();
            RefreshPorts();

        };
        var addOPBtn = new Button(addoutput) { text = "+" };

        Action reduceoutput = () =>
        {
            if (inputCount > 2)
            {
                inputCount--;
                inputContainer.Remove(inputContainer[inputContainer.childCount - 1]);
            }
            RefreshExpandedState();
            RefreshPorts();
        };

        var reduceOPBtn = new Button(reduceoutput) { text = "-" };
        reduceOPBtn.style.maxWidth = 20;
        reduceOPBtn.style.alignSelf = Align.Center;
        reduceOPBtn.style.maxHeight = 20;
        addOPBtn.style.maxWidth = 20;
        addOPBtn.style.alignSelf = Align.Center;
        addOPBtn.style.maxHeight = 20;

        titleButtonContainer.Add(reduceOPBtn);
        titleButtonContainer.Add(addOPBtn);

        #endregion
        RefreshExpandedState();
        RefreshPorts();

    }


    

    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;
        textField.SetValueWithoutNotify(description);
    }
}
