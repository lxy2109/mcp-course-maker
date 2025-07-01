using NodeGraph;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

/// <summary>
/// 流程事件节点类，用于表示工作流中的一个事件步骤
/// 继承自NodeBase<FlowEventNode>基类，实现IEventNode接口
/// </summary>
public class FlowEventNode : NodeBase<FlowEventNode>, IEventNode
{
    
    public readonly Vector2 _defaultNodeSize = new Vector2(150, 200);
    
    public string description;
    
    public string flowGraph;
    
    public string eventName;
    
    public string eventContent;
    
    public int itemCount = 0;
    
    public List<GameObjectID> selectableObectsID = new List<GameObjectID>();
    
    public List<GameObject> selectableObects;
    
    public string enterEventName;
    
    public string enterEventContent;
    
    public string exitEventName;
    
    public string exitEventContent;
    
    public AudioClip inAudioClip;
    
    public string cameraTimelineName;
    
    public string cameraTimelineContent;
    
    public string objectTimelineName;
    
    public string objectTimelineContent;
    
    public int timelineCount = 0;
    
    public List<TimelineAsset> timelineAssets;
    
    public EventEndAction endActionEnum;
    
    
    private List<ObjectField> objectFields = new List<ObjectField>();
    private List<ObjectField> timelineFields = new List<ObjectField>();
    private TextField textField;
    private TextField flowGraphField; // 流程图名称输入框
    private TextField eventNameField; // 事件名称输入框
    private TextField contenttextField;
    public ObjectField audioclipField;
    private TextField inEventField;
    private TextField outEventField;
    public EnumField endActionEnumField;
    private ScrollView objscrollView;
    private ScrollView timelinescrollView;
    private int outputCount = 1;
    private HelpBox objectshelpBox;
    private HelpBox timelineshelpBox;
    private Foldout timelineFoldOut;
    private Foldout objFoldOut;
    private Foldout objectGroupFold;
    private Foldout valuetGroupFold;
    
    private TextField cameraTimelineNameField;
    private TextField cameraTimelineContentField;
    private TextField objectTimelineNameField;
    private TextField objectTimelineContentField;

    /// <summary>
    /// 创建节点时调用的方法，设置节点的UI和数据
    /// </summary>
    /// <param name="view">节点所属的图形视图</param>
    public override void OnCreated(NodeGraphView view)
    {
        if (view == null) return;

        // 初始化列表，确保不为空
        if (selectableObects == null)
            selectableObects = new List<GameObject>();
        if (timelineAssets == null)
            timelineAssets = new List<TimelineAsset>();
        if (selectableObectsID == null)
            selectableObectsID = new List<GameObjectID>();
            
        #region Title
        // 设置节点标题和背景颜色
        if (string.IsNullOrEmpty(eventName)) title = NodeName;
        else title = eventName;
        titleContainer.style.backgroundColor = new Color(0.5f, 0.4f, 0.1f, 1);
        #endregion

        #region Debug
        // 创建调试折叠面板
        Foldout debugFold = new Foldout();
        debugFold.text = "Debug";
        debugFold.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);

        // 显示节点GUID
        var guidLable = new Label(GUID);
        guidLable.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        debugFold.Add(guidLable);

        // 添加描述文本输入框
        textField = new TextField("Description", -1, true, false, default);
        textField.RegisterValueChangedCallback(evt =>
        {
            description = evt.newValue;
        });
        textField.SetValueWithoutNotify(name);
        textField.style.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1);
        debugFold.Add(textField);

        extensionContainer.Add(debugFold);
        debugFold.SetValueWithoutNotify(false); // 默认折叠
        #endregion
        
        // 添加flowGraph的信息
        flowGraphField = new TextField("流程图名称", -1, false, false, default);
        flowGraphField.value = flowGraph;
        flowGraphField.RegisterValueChangedCallback(evt =>
        {
            flowGraph = evt.newValue;
        });
        flowGraphField.SetValueWithoutNotify(flowGraph);
        mainContainer.Add(flowGraphField);

        // 创建事件名称输入字段
        eventNameField = new TextField("事件名称", -1, false, false, default);
        eventNameField.value = eventName;
        Debug.Log(eventName);
        eventNameField.RegisterValueChangedCallback(evt =>
        {
            eventName = evt.newValue;
            title = eventName; // 更新节点标题
        });
        eventNameField.SetValueWithoutNotify(eventName);
        mainContainer.Add(eventNameField);
        
        

        #region Object
        // 创建可配置对象组折叠面板
        objectGroupFold = new Foldout();
        objectGroupFold.text = "可配置Object";
        objectGroupFold.style.backgroundColor = new Color(0.05f, 0.15f, 0.2f, 0.25f);

        #region Object Field
        // 创建可交互物体折叠面板
        objFoldOut = new Foldout();
        objFoldOut.text = "初始高亮可交互物体";
        VisualElement objsWindow = new VisualElement();

        // 创建可滚动视图
        objscrollView = new ScrollView();
        objscrollView.style.maxHeight = 75;
        objsWindow.style.flexDirection = FlexDirection.Row;
        
        // 添加物体的操作
        Action addSelectObjs = () =>
        {
            // 检查是否有空值
            if (itemCount >= 1 && selectableObects[itemCount - 1] == null)
            {
                if (!objFoldOut.Contains(objectshelpBox))
                {
                    // 添加错误提示框
                    objectshelpBox = new HelpBox();
                    objectshelpBox.text = "存在空值";
                    objectshelpBox.messageType = HelpBoxMessageType.Error;
                    objFoldOut.Add(objectshelpBox);
                }
                return;
            }
            else
            {
                if (objFoldOut.Contains(objectshelpBox)) { objFoldOut.Remove(objectshelpBox); }
            }
            
            // 添加新的空物体引用
            selectableObects.Add(null);
            GameObjectID temp = new GameObjectID();
            selectableObectsID.Add(temp);
            itemCount++;
            
            // 创建对应的ObjectField
            ObjectField tempobjField = new ObjectField(" Element" + itemCount.ToString());
            tempobjField.objectType = typeof(GameObject);
            tempobjField.RegisterValueChangedCallback(evt =>
            {
                selectableObects[itemCount - 1] = (GameObject)evt.newValue as GameObject;
                // 更新ID和名称用于序列化
                selectableObectsID[itemCount - 1].ID = selectableObects[itemCount - 1].GetInstanceID();
                selectableObectsID[itemCount - 1].name = selectableObects[itemCount - 1].name;
                
                // 检查是否所有字段都已填充，如果是则移除错误提示
                if (itemCount >= 1)
                {
                    for (int i = 0; i < selectableObects.Count - 1; i++)
                    {
                        if (selectableObects[i] == null) return;
                    }
                    if (objFoldOut.Contains(objectshelpBox))
                    { objFoldOut.Remove(objectshelpBox); }
                }
            });
            tempobjField.SetValueWithoutNotify(selectableObects[itemCount - 1]);
            objscrollView.Add(tempobjField);
            objectFields.Add(tempobjField);
        };
        objFoldOut.Add(objscrollView);

        // 添加物体的按钮
        var addObjBtn = new Button(addSelectObjs) { text = "+" };
        objsWindow.Add(addObjBtn);
        objsWindow.style.flexDirection = FlexDirection.Row;

        // 移除物体的操作
        Action removeSelectObjs = () =>
        {
            if (objectFields.Count <= 0) return;
            itemCount--;
            objectFields.Remove(objectFields[objectFields.Count - 1]);
            selectableObects.Remove(selectableObects[selectableObects.Count - 1]);
            selectableObectsID.Remove(selectableObectsID[selectableObectsID.Count - 1]);
            objscrollView.Remove(objscrollView[objscrollView.childCount - 1]);

            // 检查是否所有字段都已填充
            for (int i = 0; i < selectableObects.Count; i++)
            {
                if (selectableObects[i] == null) return;
            }
            // 没有空值则移除错误提示
            if (objFoldOut.Contains(objectshelpBox))
            { objFoldOut.Remove(objectshelpBox); }
        };
        var removeObjBtn = new Button(removeSelectObjs) { text = "-" };
        objsWindow.Add(removeObjBtn);

        objFoldOut.Add(objsWindow);
        objectGroupFold.Add(objFoldOut);
        #endregion
        
        #region Timeline Field
        // 创建Timeline资产折叠面板
        
        // 创建Timeline配置区域
        Foldout timelineConfigFold = new Foldout();
        timelineConfigFold.text = "Timeline配置";
        timelineConfigFold.style.backgroundColor = new Color(0.05f, 0.15f, 0.2f, 0.25f);
        
        // 相机Timeline名称输入框
        // 相机Timeline名称输入框
        TextField cameraTimelineNameField = new TextField("相机Timeline名称")
        {
            multiline = true,
            style = { 
                whiteSpace = WhiteSpace.Normal, 
                maxWidth = 300,
                minHeight = 30 
            }
        };
        cameraTimelineNameField.value = cameraTimelineName;
        cameraTimelineNameField.RegisterValueChangedCallback(evt =>
        {
            cameraTimelineName = evt.newValue;
        });
        cameraTimelineNameField.SetValueWithoutNotify(cameraTimelineName);
        timelineConfigFold.Add(cameraTimelineNameField);
        
        // 相机Timeline内容输入框(多行)
        TextField cameraTimelineContentField = new TextField("相机Timeline内容")
        {
            multiline = true,
            style = { 
                whiteSpace = WhiteSpace.Normal, 
                maxWidth = 300,
                minHeight = 60 
            }
        };
        cameraTimelineContentField.value = cameraTimelineContent;
        cameraTimelineContentField.RegisterValueChangedCallback(evt =>
        {
            cameraTimelineContent = evt.newValue;
        });
        cameraTimelineContentField.SetValueWithoutNotify(cameraTimelineContent);
        timelineConfigFold.Add(cameraTimelineContentField);
        
        // 物体Timeline名称输入框
        TextField objectTimelineNameField = new TextField("物体Timeline名称")
        {
            multiline = true,
            style = { 
                whiteSpace = WhiteSpace.Normal, 
                maxWidth = 300,
                minHeight = 30 
            }
        };
        objectTimelineNameField.value = objectTimelineName;
        objectTimelineNameField.RegisterValueChangedCallback(evt =>
        {
            objectTimelineName = evt.newValue;
        });
        objectTimelineNameField.SetValueWithoutNotify(objectTimelineName);
        timelineConfigFold.Add(objectTimelineNameField);
        
        // 物体Timeline内容输入框(多行)
        TextField objectTimelineContentField = new TextField("物体Timeline内容")
        {
            multiline = true,
            style = { 
                whiteSpace = WhiteSpace.Normal, 
                maxWidth = 300,
                minHeight = 60 
            }
        };
        objectTimelineContentField.value = objectTimelineContent;
        objectTimelineContentField.RegisterValueChangedCallback(evt =>
        {
            objectTimelineContent = evt.newValue;
        });
        objectTimelineContentField.SetValueWithoutNotify(objectTimelineContent);
        timelineConfigFold.Add(objectTimelineContentField);
        
        // 将Timeline配置添加到对象组折叠面板
        objectGroupFold.Add(timelineConfigFold);
        
        timelineFoldOut = new Foldout();
        timelineFoldOut.text = "播放的timeline";
        VisualElement timelineWindow = new VisualElement();

        // 创建可滚动视图
        timelinescrollView = new ScrollView();
        timelinescrollView.style.maxHeight = 75;
        timelinescrollView.style.flexDirection = FlexDirection.Row;
        
        // 添加Timeline的操作
        Action addTimeline = () =>
        {
            // 检查是否有空值
            if (timelineCount >= 1 && timelineAssets[timelineCount - 1] == null)
            {
                if (!timelineFoldOut.Contains(timelineshelpBox))
                {
                    // 添加错误提示框
                    timelineshelpBox = new HelpBox();
                    timelineshelpBox.text = "存在空值";
                    timelineshelpBox.messageType = HelpBoxMessageType.Error;
                    timelineFoldOut.Add(timelineshelpBox);
                }
                return;
            }
            else
            {
                if (timelineFoldOut.Contains(timelineshelpBox)) { timelineFoldOut.Remove(timelineshelpBox); }
            }
            
            // 添加新的Timeline引用
            timelineAssets.Add(null);
            timelineCount++;
            
            // 创建对应的ObjectField
            ObjectField temptimelineField = new ObjectField(" Timeline" + timelineCount.ToString());
            temptimelineField.objectType = typeof(TimelineAsset);
            temptimelineField.RegisterValueChangedCallback(evt =>
            {
                timelineAssets[timelineCount - 1] = (TimelineAsset)evt.newValue as TimelineAsset;
                
                // 检查是否所有字段都已填充
                if (timelineCount >= 1)
                {
                    for (int i = 0; i < timelineAssets.Count - 1; i++)
                    {
                        if (timelineAssets[i] == null) return;
                    }
                    if (timelineFoldOut.Contains(timelineshelpBox))
                    { timelineFoldOut.Remove(timelineshelpBox); }
                }
            });
            temptimelineField.SetValueWithoutNotify(timelineAssets[timelineCount - 1]);
            timelinescrollView.Add(temptimelineField);
            timelineFields.Add(temptimelineField);
        };
        timelineFoldOut.Add(timelinescrollView);

        // 添加Timeline的按钮
        var addTimelineBtn = new Button(addTimeline) { text = "+" };
        timelineWindow.Add(addTimelineBtn);
        timelineWindow.style.flexDirection = FlexDirection.Row;

        // 移除Timeline的操作
        Action removeTimeline = () =>
        {
            if (timelineFields.Count <= 0) return;
            timelineCount--;
            timelineFields.Remove(timelineFields[timelineFields.Count - 1]);
            timelineAssets.Remove(timelineAssets[timelineAssets.Count - 1]);
            timelinescrollView.Remove(timelinescrollView[timelinescrollView.childCount - 1]);

            // 检查是否所有字段都已填充
            for (int i = 0; i < selectableObects.Count; i++)
            {
                if (selectableObects[i] == null) return;
            }
            // 没有空值则移除错误提示
            if (timelineFoldOut.Contains(timelineshelpBox))
            { timelineFoldOut.Remove(timelineshelpBox); }
        };
        var removeTimelineBtn = new Button(removeTimeline) { text = "-" };
        timelineWindow.Add(removeTimelineBtn);

        timelineFoldOut.Add(timelineWindow);
        objectGroupFold.Add(timelineFoldOut);
        #endregion
        
        mainContainer.Add(objectGroupFold);
        objectGroupFold.SetValueWithoutNotify(false); // 默认折叠
        #endregion

        #region Content Fold
        // 创建内容折叠面板
        valuetGroupFold = new Foldout();
        valuetGroupFold.text = "内容";
        valuetGroupFold.style.backgroundColor = new Color(0.05f, 0.15f, 0.2f, 0.25f);

        // 创建多行文本输入框用于事件文案
        contenttextField = new TextField("阶段文案")
        {
            multiline = true,
            style = { whiteSpace = WhiteSpace.Normal, minHeight = 40, flexDirection = FlexDirection.Row }
        };

        contenttextField.multiline = true;
        contenttextField.style.maxWidth = 300;

        contenttextField.value = eventContent;
        contenttextField.RegisterValueChangedCallback(evt =>
        {
            eventContent = evt.newValue;
        });
        contenttextField.SetValueWithoutNotify(contenttextField.value);
        valuetGroupFold.Add(contenttextField);

        // 创建音频剪辑选择字段
        audioclipField = new ObjectField("进入事件时播放的音频");
        audioclipField.objectType = typeof(AudioClip);
        audioclipField.value = inAudioClip;
        audioclipField.RegisterValueChangedCallback(evt =>
        {
            inAudioClip = (AudioClip)evt.newValue as AudioClip;
        });
        audioclipField.SetValueWithoutNotify(inAudioClip);
        valuetGroupFold.Add(audioclipField);

        // 创建进入事件名称输入框
        inEventField = new TextField("进入事件时进行的Event名称", -1, false, false, default);
        inEventField.value = enterEventName;
        inEventField.RegisterValueChangedCallback(evt =>
        {
            enterEventName = evt.newValue;
        });
        inEventField.SetValueWithoutNotify(inEventField.value);
        valuetGroupFold.Add(inEventField);

        // 创建离开事件名称输入框
        outEventField = new TextField("离开事件时进行的Event名称", -1, false, false, default);
        outEventField.value = exitEventName;
        outEventField.RegisterValueChangedCallback(evt =>
        {
            exitEventName = evt.newValue;
        });
        outEventField.SetValueWithoutNotify(outEventField.value);
        valuetGroupFold.Add(outEventField);

        // 创建事件结束行为枚举选择框
        endActionEnumField = new EnumField("事件结束后行为", endActionEnum);
        endActionEnumField.RegisterValueChangedCallback(evt =>
        {
            endActionEnum = (EventEndAction)evt.newValue;
        });
        endActionEnumField.SetValueWithoutNotify(endActionEnumField.value);
        valuetGroupFold.Add(endActionEnumField);

        mainContainer.Add(valuetGroupFold);
        valuetGroupFold.SetValueWithoutNotify(false); // 默认折叠
        #endregion

        #region port
        // 添加输入端口
        var inputPort = GeneratePort(this, Direction.Input, typeof(string), Port.Capacity.Single);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // 添加输出端口
        for (int i = 0; i < outputCount; i++)
        {
            var outputPort = GeneratePort(this, Direction.Output, typeof(string));
            outputPort.portName = "Output" + (i + 1).ToString();
            outputContainer.Add(outputPort);
        }
        #endregion
        
        // 刷新节点状态和端口
        RefreshExpandedState();
        RefreshPorts();

        // 设置节点位置和大小
        SetPosition(new Rect(new Vector2(0, 0), _defaultNodeSize));
    }

    /// <summary>
    /// 加载已有节点时调用的方法，恢复节点的UI状态
    /// </summary>
    /// <param name="view">节点所属的图形视图</param>
    public override void OnLoadad(NodeGraphView view)
    {
        if (view == null) return;
        
        // 在OnLoadad方法中添加以下代码以恢复Timeline配置字段的值
        if (cameraTimelineNameField != null)
        {
            cameraTimelineNameField.SetValueWithoutNotify(cameraTimelineName);
            cameraTimelineContentField.SetValueWithoutNotify(cameraTimelineContent);
            objectTimelineNameField.SetValueWithoutNotify(objectTimelineName);
            objectTimelineContentField.SetValueWithoutNotify(objectTimelineContent);
        }
        
        // 恢复所有字段的值
        textField.SetValueWithoutNotify(description);
        eventNameField.SetValueWithoutNotify(eventName);
        contenttextField.SetValueWithoutNotify(eventContent);
        inEventField.SetValueWithoutNotify(enterEventName);
        outEventField.SetValueWithoutNotify(exitEventName);
        endActionEnumField.SetValueWithoutNotify(endActionEnum);
        audioclipField.SetValueWithoutNotify(inAudioClip);

        // 根据场景物品ID进行初始化
        selectableObects.Clear();
        for (int i = 0; i < selectableObectsID.Count; i++)
        {
            if (selectableObectsID[i].ID == 0) continue;
            else
            {
                // 根据名称查找对象
                GameObject temp = GameObject.Find(selectableObectsID[i].name);
                if (temp != null)
                {
                    // 检查实例ID是否匹配，不匹配则更新
                    if (temp.GetInstanceID() != selectableObectsID[i].ID)
                    {
                        selectableObectsID[i].ID = temp.GetInstanceID();
                    }
                    selectableObects.Add(temp);
                }
                else continue;
            }
        }

        // 为每个可交互物体创建ObjectField
        for (int i = 0; i < selectableObects.Count; i++)
        {
            ObjectField tempobjField = new ObjectField(" Element" + (i + 1).ToString());
            tempobjField.objectType = typeof(GameObject);
            tempobjField.SetValueWithoutNotify(selectableObects[i]);
            objscrollView.Add(tempobjField);
            objectFields.Add(tempobjField);
        }
        
        // 为每个Timeline资产创建ObjectField
        for (int i = 0; i < timelineAssets.Count; i++)
        {
            ObjectField tempobjField = new ObjectField(" Timeline" + (i + 1).ToString());
            tempobjField.objectType = typeof(TimelineAsset);
            tempobjField.SetValueWithoutNotify(timelineAssets[i]);
            timelinescrollView.Add(tempobjField);
            timelineFields.Add(tempobjField);
        }
    }
}