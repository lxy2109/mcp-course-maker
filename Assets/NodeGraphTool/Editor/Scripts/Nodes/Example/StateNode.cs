using System;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Timeline;
public class StateNode : NodeBase<StateNode>, INode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);

    public string description;
    public string stateName;
    public GameObject mainUI;
    public Sprite activeSprite;

    private TextField textField;
    private TextField statetextField;
    private ObjectField mainUIField;
    private ObjectField spriteField;

    public int eventCount = 1;

    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;

        #region Title
        if (string.IsNullOrEmpty(stateName)) title = NodeName;
        else title = stateName;
        titleContainer.style.backgroundColor = new Color(0.1f, 0.4f, 0.4f, 1);
        #endregion

        #region Debug
        Foldout debugFold = new Foldout();
        debugFold.text = "Debug";
        debugFold.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);


        var guidLable = new Label(GUID);
        guidLable.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        debugFold.Add(guidLable);

        textField = new TextField("Description", -1, true, false, default);
        textField.RegisterValueChangedCallback(evt =>
        {
            description = evt.newValue;
        });
        textField.SetValueWithoutNotify(name);
        textField.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        debugFold.Add(textField);

        statetextField = new TextField("State Name", -1, false, false, default);
        statetextField.RegisterValueChangedCallback(evt =>
        {
            stateName = evt.newValue;
            title = stateName;
        });
        extensionContainer.Add(debugFold);
        #endregion

        statetextField.SetValueWithoutNotify(stateName);
        mainContainer.Add(statetextField);


        mainUIField = new ObjectField(" Main UI");
        mainUIField.objectType = typeof(GameObject);
        mainUIField.RegisterValueChangedCallback(evt =>
        {
            mainUI = (GameObject)evt.newValue as GameObject;
        });
        mainUIField.SetValueWithoutNotify(mainUIField.value);
        mainContainer.Add(mainUIField);

        spriteField = new ObjectField(" Active Sprite");
        spriteField.objectType = typeof(Sprite);
        spriteField.RegisterValueChangedCallback(evt =>
        {
            activeSprite = (Sprite)evt.newValue as Sprite;
        });
        spriteField.SetValueWithoutNotify(spriteField.value);
        mainContainer.Add(spriteField);

        var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);


        for (int i = 0; i < eventCount; i++)
        {
            var outputPort = GeneratePort(this, Direction.Output, typeof(string));
            outputPort.portName = "Output" + (i + 1).ToString();
            outputContainer.Add(outputPort);
        }

        #region Add OutputPort 
        Action addoutput = () =>
        {
            eventCount++;
            var outputPort = GeneratePort(this, Direction.Output, typeof(string));
            outputPort.portName = "Output" + (eventCount).ToString();
            outputContainer.Add(outputPort);
            RefreshExpandedState();
            RefreshPorts();

        };
        var addOPBtn = new Button(addoutput) { text = "+" };

        Action reduceoutput = () =>
        {
            if (eventCount > 1)
            {
                eventCount--;
                outputContainer.Remove(outputContainer[outputContainer.childCount - 1]);
            }

            RefreshExpandedState();
            RefreshPorts();
        };

        var reduceOPBtn = new Button(reduceoutput) { text = "-" };
        //var inlineBtn = new VisualElement();
        reduceOPBtn.style.maxWidth = 20;
        reduceOPBtn.style.alignSelf = Align.Center;
        reduceOPBtn.style.maxHeight = 20;
        addOPBtn.style.maxWidth = 20;
        addOPBtn.style.alignSelf = Align.Center;
        addOPBtn.style.maxHeight = 20;

        //titleButtonContainer.Add(reduceOPBtn);
        //titleButtonContainer.Add(addOPBtn);

        titleButtonContainer.Add(reduceOPBtn);
        titleButtonContainer.Add(addOPBtn);

        #endregion

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));


    }
    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;
        statetextField.SetValueWithoutNotify(stateName);
        textField.SetValueWithoutNotify(description);
        mainUIField.SetValueWithoutNotify(mainUI ? mainUI : null);
        spriteField.SetValueWithoutNotify(activeSprite ? activeSprite : null);
       // outputCount=
    }



}
