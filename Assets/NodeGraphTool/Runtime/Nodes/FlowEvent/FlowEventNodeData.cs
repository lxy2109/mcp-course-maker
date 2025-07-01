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
    
    [LabelText("description�ڵ�����")]
    [TextArea(1, 10)]
    public string description;

    //�¼����� ��ҪΨһ
    
    [LabelText("flowGraph ʵ�鲽��(FlowGraph)ʵ���߼�����")] 
    public string flowGraph;
    
    [LabelText("eventName ʵ�鶯��(EventName)���嶯���¼�")]
    public string eventName;
    
    //�׶��İ�
    [LabelText("eventContent �����ı�(�׶��İ� UI������ʾ)")]
    [TextArea(1, 10)]
    public string eventContent;
    
    [LabelText("�ֲ���ʾ(��Ҫ�������Ʒ����)")]
    public string handTip;
    public int itemCount = 0;
    [SerializeField]
    [LabelText("�����ɽ�������ID")]
    public List<GameObjectID> selectableObectsID=new List<GameObjectID>();
    [LabelText("�����ɽ�������")]
    public List<GameObject> selectableObects = new List<GameObject>();
    
    [LabelText("���⽻��Enter(�����¼�ʱ���е�Event����)")]
    public string enterEventName;
    
    [LabelText("��������Enter(�����¼�ʱ���е�Event������)")]
    public string enterEventContent;
    
    [LabelText("���⽻��Exit(�뿪�¼�ʱ���е�Event����)")]
    public string exitEventName;
    
    [LabelText("��������Exit(�뿪�¼�ʱ���е�Event������)")]
    public string exitEventContent;
    
    [LabelText("�����ű�����(�����¼�ʱ���ŵ���Ƶ����)")]
    public string voiceName;
    [LabelText("�����ű�����(�����¼�ʱ���ŵ���Ƶ�İ�����)")]
    public string voiceContent;
    
    [LabelText("�����¼�ʱ���ŵ���Ƶ")]
    public AudioClip inAudioClip;
    
    [LabelText("��ͷtimeline����")]
    public string cameraTimelineName;
    [LabelText("��ͷtimeline����")]
    public string cameraTimelineContent;
    [LabelText("����timeline����")]
    public string objectTimelineName;
    [LabelText("����timeline����")]
    public string objectTimelineContent;
    public int timelineCount = 0;
    [LabelText("����timeline")]
    public List<TimelineAsset> timelineAssets;
    
    [LabelText("�¼���������Ϊ")]
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
    /// ����ǰ���г�ʼ��
    /// </summary>
    public void Init()
    {
        //���ݳ�����ƷID���г�ʼ��
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
