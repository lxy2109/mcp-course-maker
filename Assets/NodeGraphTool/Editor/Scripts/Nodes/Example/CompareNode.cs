using System;
using System.Collections;
using System.Collections.Generic;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;
public class CompareNode : NodeBase<CompareNode>, INode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
  //  public string description;
    public Compare compare=Compare.Less;

    //private TextField textField;
    private EnumField compareField;


    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;
        title = "Compare";


        var lableContent = new Label("");
        lableContent.style.backgroundColor = new Color(0.3f, 0.2f, 0.1f, 1);
        lableContent.style .alignSelf=Align.Center;
        switch (compare)
        {
            case Compare.Less:
                lableContent.text = "a < b";
                break;
            case Compare.LessOrEqual:
                lableContent.text = "a ≤ b";
                break;
            case Compare.Equal:
                lableContent.text = "a = b";
                break;
            case Compare.GreaterOrEqual:
                lableContent.text = "a ≥ b";
                break;
            case Compare.Greater:
                lableContent.text = "a > b";
                break;
            case Compare.NotEqual:
                lableContent.text = "a ≠ b";
                break;

        }


        compareField = new EnumField(compare);

        compareField.RegisterValueChangedCallback(evt =>
        {
            compare =(Compare)evt.newValue;
            switch (compare)
            {
                case Compare.Less:
                    lableContent.text = "a < b";
                    break;
                case Compare.LessOrEqual:
                    lableContent.text = "a ≤ b";
                    break;
                case Compare.Equal:
                    lableContent.text = "a = b";
                    break;
                case Compare.GreaterOrEqual:
                    lableContent.text = "a ≥ b";
                    break;
                case Compare.Greater:
                    lableContent.text = "a > b";
                    break;
                case Compare.NotEqual:
                    lableContent.text = "a ≠ b";
                    break;
                  
            }
        });
        extensionContainer.style.backgroundColor = new Color(0.3f, 0.2f, 0.1f, 1);
        extensionContainer.Add(lableContent);
        compareField.SetValueWithoutNotify(compareField.value);
        mainContainer.Add(compareField);



        var dataA = GeneratePort(this, Direction.Input, typeof(float), Port.Capacity.Single);
        dataA.portName = "a";
        inputContainer.Add(dataA);
        var dataB = GeneratePort(this, Direction.Input, typeof(float), Port.Capacity.Single);
        dataB.portName = "b";
        inputContainer.Add(dataB);

        var inputPortTrue = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPortTrue.portName = "True Event";
        inputContainer.Add(inputPortTrue);
        var inputPortFalse = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPortFalse.portName = "False Event";
        inputContainer.Add(inputPortFalse);


        var outputPortTrue = GeneratePort(this, Direction.Output, typeof(string));
        outputPortTrue.portName = "True";
        outputContainer.Add(outputPortTrue);

        var outputPortFalse= GeneratePort(this, Direction.Output, typeof(string));
        outputPortFalse.portName = "False";
        outputContainer.Add(outputPortFalse);

        RefreshExpandedState();
        RefreshPorts();
    }

    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;
        compareField.SetValueWithoutNotify(compare);
    }
}
