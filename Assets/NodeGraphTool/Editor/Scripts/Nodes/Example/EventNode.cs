using System;
using NodeGraph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Timeline;

public class EventNode : NodeBase<EventNode>, INode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
    public string description;
    public string eventName;
    public GameObject obj;
    public TimelineAsset timelineAsset;
    public float weight;


    private TextField textField;
    private TextField eventtextField;
    private ObjectField gameobjectField;
    private ObjectField timelineassetField;
    private FloatField weightField;

    private int outputCount = 1;


    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;

        #region Title
        if (string.IsNullOrEmpty(eventName)) title = NodeName;
        else title = eventName;
        titleContainer.style.backgroundColor = new Color(0.5f, 0.4f, 0.1f, 1);
        #endregion

        #region Debug
        Foldout debugFold =new Foldout();
        debugFold.text = "Debug";
        debugFold.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);


        var guidLable = new Label(GUID);
        //guidLable.style.color = new Color(0.6f, 0.2f, 0.2f, 1);
        guidLable.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        debugFold.Add(guidLable);
        

         textField = new TextField("Description", -1, true, false, default);
        textField.RegisterValueChangedCallback(evt =>
        {
            description = evt.newValue;
        });
        textField.SetValueWithoutNotify(name);
        textField.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        //  extensionContainer.Add(textField);
        debugFold.Add(textField);

        extensionContainer.Add(debugFold);
        #endregion

  
        

        eventtextField = new TextField("Event Name", -1, false, false, default);
        eventtextField.RegisterValueChangedCallback(evt =>
        {
            eventName = evt.newValue;
            title = eventName;
        });
        eventtextField.SetValueWithoutNotify(eventName);
        mainContainer.Add(eventtextField);



        weightField = new FloatField("Weight", 1);
        weightField.RegisterValueChangedCallback(evt =>
        {
            weight = evt.newValue;
        });
        weightField.SetValueWithoutNotify(weight);
        mainContainer.Add(weightField);


        gameobjectField = new ObjectField(" Prefab");
        gameobjectField.objectType = typeof(GameObject);
        gameobjectField.RegisterValueChangedCallback(evt =>
        {
            obj = (GameObject)evt.newValue as GameObject;
        });
        gameobjectField.SetValueWithoutNotify(gameobjectField.value);
        mainContainer.Add(gameobjectField);

        timelineassetField = new ObjectField("TImeline Asset");
        timelineassetField.objectType = typeof(TimelineAsset);
        timelineassetField.RegisterValueChangedCallback(evt =>
        {
            timelineAsset = (TimelineAsset)evt.newValue as TimelineAsset;
        });
        timelineassetField.SetValueWithoutNotify(timelineassetField.value);
        mainContainer.Add(timelineassetField);

  


        var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

      



        for (int i = 0; i < outputCount; i++)
        {
            var outputPort = GeneratePort(this, Direction.Output, typeof(string));
            outputPort.portName = "Output"+(i+1).ToString();
            outputContainer.Add(outputPort);
        }

        var dataPort = GeneratePort(this, Direction.Output, typeof(float), Port.Capacity.Single);
        dataPort.portName = "Weight";
        outputContainer.Add(dataPort);


        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));
    }

    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;
        textField.SetValueWithoutNotify(description);
        eventtextField.SetValueWithoutNotify(eventName);
        gameobjectField.SetValueWithoutNotify(obj ? obj : null);
        timelineassetField.SetValueWithoutNotify(timelineAsset ? timelineAsset : null);

    }


}
