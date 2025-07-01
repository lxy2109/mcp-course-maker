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
    
    [LabelText("flowGraph 实验步骤(FlowGraph)实验逻辑部分")] 
    public string flowGraph;
    
    [LabelText("eventName 实验动作(EventName)具体动作事件")]
    public string eventName;
    
    //阶段文案
    [LabelText("eventContent 引导文本(阶段文案 UI引导提示)")]
    [TextArea(1, 10)]
    public string eventContent;
    
    [LabelText("手部提示(需要点击的物品名称)")]
    public string handTip;
    public int itemCount = 0;
    [SerializeField]
    [LabelText("高亮可交互物体ID")]
    public List<GameObjectID> selectableObectsID=new List<GameObjectID>();
    [LabelText("高亮可交互物体")]
    public List<GameObject> selectableObects = new List<GameObject>();
    
    [LabelText("虚拟交互Enter(进入事件时进行的Event名称)")]
    public string enterEventName;
    
    [LabelText("交互内容Enter(进入事件时进行的Event名内容)")]
    public string enterEventContent;
    
    [LabelText("虚拟交互Exit(离开事件时进行的Event名称)")]
    public string exitEventName;
    
    [LabelText("交互内容Exit(离开事件时进行的Event名内容)")]
    public string exitEventContent;
    
    [LabelText("配音脚本名称(进入事件时播放的音频名称)")]
    public string voiceName;
    [LabelText("配音脚本内容(进入事件时播放的音频文案内容)")]
    public string voiceContent;
    
    [LabelText("进入事件时播放的音频")]
    public AudioClip inAudioClip;
    
    [LabelText("镜头timeline名称")]
    public string cameraTimelineName;
    [LabelText("镜头timeline内容")]
    public string cameraTimelineContent;
    [LabelText("物体timeline名称")]
    public string objectTimelineName;
    [LabelText("物体timeline内容")]
    public string objectTimelineContent;
    public int timelineCount = 0;
    [LabelText("播放timeline")]
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
    /// 调用前进行初始化
    /// </summary>
    public void Init()
    {
        //根据场景物品ID进行初始化
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
