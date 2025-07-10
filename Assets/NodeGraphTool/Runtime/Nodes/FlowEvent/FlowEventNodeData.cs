using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NodeGraph;
using UnityEngine.Timeline;
using Sirenix.OdinInspector;
using UnityEngine.Events;


public enum EventEndAction
{   
    Hold,
    HoldForCombine,
    NextEvent,
}

[Serializable]
public class GameObjectID
{
    public string name;
    public int ID;
}

public class FlowEventNodeData : NodeBaseData
{
    
    [LabelText("description节点描述")]
    [TextArea(1, 10)]
    public string description;

    //事件名称 需要唯一
    
    [LabelText("flowGraph 实验步骤(FlowGraph)实验逻辑流程")] 
    public string flowGraph;
    
    [LabelText("eventName 实验动作(EventName)具体动作事件")]
    public string eventName;
    
    //阶段内容
    [LabelText("eventContent 事件文本(阶段内容 UI界面显示)")]
    [TextArea(1, 10)]
    public string eventContent;
    
    [LabelText("手部提示(需要操作的物品提示)")]
    public string handTip;
    public int itemCount = 0;
    [SerializeField]
    [LabelText("可交互选择物体ID")]
    public List<GameObjectID> selectableObectsID=new List<GameObjectID>();
    [LabelText("可交互选择物体")]
    public List<GameObject> selectableObects = new List<GameObject>();
    
    [LabelText("额外交互Enter(进入事件时触发的Event事件)")]
    public string enterEventName;
    
    [LabelText("事件内容Enter(进入事件时触发的Event事件内容)")]
    public string enterEventContent;
    
    [LabelText("额外交互Exit(离开事件时触发的Event事件)")]
    public string exitEventName;
    
    [LabelText("事件内容Exit(离开事件时触发的Event事件内容)")]
    public string exitEventContent;
    
    [LabelText("语音脚本名称(进入事件时播放的音频名称)")]
    public string voiceName;
    [LabelText("语音脚本内容(进入事件时播放的音频的内容描述)")]
    public string voiceContent;
    
    [LabelText("进入事件时播放的音频")]
    public AudioClip inAudioClip;
    
    [LabelText("摄像头timeline名称")]
    public string cameraTimelineName;
    [LabelText("摄像头timeline内容")]
    public string cameraTimelineContent;
    [LabelText("物体timeline名称")]
    public string objectTimelineName;
    [LabelText("物体timeline内容")]
    public string objectTimelineContent;
    [LabelText("Timeline数量（自动计算）")]
    public int timelineCount => timelineAssets?.Count ?? 0;
    [LabelText("相关timeline")]
    public List<TimelineAsset> timelineAssets;
    
    [LabelText("事件结束后行为")]
    public EventEndAction endActionEnum;

    
    
    
    

    //key=name value=id
    
    
    

    

    //private  FlowEventNodeData(FlowEventNodeData target)
    //{
    //    this.eventName = target.eventName;
    //    this.selectableObects = target.selectableObects;
    //    this.timelineAssets = target.timelineAssets;
    //    this.content = target.content;
    //    this.endActionEnum = target.endActionEnum;
    //    this.inEvent = target.inEvent;
    //    this.outEvent = target.outEvent;
    //}



    /// <summary>
    /// 运行前需要出现的初始化
    /// </summary>
    public void Init()
    {
        //根据产品信息ID列表出初始化
        selectableObects.Clear();
        if (selectableObectsID == null) return;
        for (int i = 0; i < selectableObectsID.Count; i++)
        {
            if (selectableObectsID[i].ID == 0) continue;
            else
            {
                GameObject temp = GameObject.Find(selectableObectsID[i].name);
                // temp.GetHashCode();
                if (temp != null)
                {
                    if (temp.GetInstanceID() != selectableObectsID[i].ID)
                    {
                        selectableObectsID[i].ID = temp.GetInstanceID();
                    }
                    selectableObects.Add(temp);
                }
                   
                else continue;
            }
        }
    }


}
