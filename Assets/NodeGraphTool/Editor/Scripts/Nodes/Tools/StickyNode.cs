using NodeGraph;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

public class StickyNode : NodeBase<StickyNode>, IEventNode
{
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 150);

    public string description;
    private TextField contenttextField;

    public override void OnCreated(NodeGraphView view)
    {
        if (GUID == "") { System.Guid.NewGuid().ToString(); }

        #region Title
        title = "备注";
        titleContainer.style.backgroundColor = new Color(0.5f, 0.4f, 0.1f, 1);
        #endregion



        contenttextField = new TextField()
        {
            multiline = true,
            style = { whiteSpace = WhiteSpace.NoWrap, minHeight = 150, flexDirection = FlexDirection.Row }
        };
        contenttextField.style.backgroundColor = new Color(0.5f, 0.4f, 0.1f, 1);
        contenttextField.style.color = new Color(0.5f, 0.4f, 0.1f, 1);
        contenttextField.style.borderLeftColor = new Color(0.5f, 0.4f, 0.1f, 1);
        mainContainer.style.backgroundColor = new Color(0.5f, 0.4f, 0.1f, 1);

        contenttextField.multiline = true;
        contenttextField.style.maxWidth = 150;
        contenttextField.style.minWidth = 150;
        contenttextField.RegisterValueChangedCallback(evt =>
        {
            description = evt.newValue;
        });
        contenttextField.SetValueWithoutNotify(description);
        mainContainer.Add(contenttextField);

        RefreshExpandedState();
        SetPosition(new Rect(new Vector2(200, 200), _defaultNodeSize));
    }

    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;

        contenttextField.SetValueWithoutNotify(description);
    }
}
