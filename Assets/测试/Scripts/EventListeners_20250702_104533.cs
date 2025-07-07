using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class EventListeners_20250702_104533 : SceneSingleton<EventListeners_20250702_104533>, IEventListeners
{

    private EventUI eventUI;

    #region Events
        [Tooltip("介绍实验目的")]
        //了解实验目的及意义
        public UnityEvent enter0;

        [Tooltip("开始检查器具")]
        //检查器具齐全
        public UnityEvent enter1;

        [Tooltip("开始检查仪器外观")]
        //检查仪器外观
        public UnityEvent enter2;

        [Tooltip("开始检查仪器电源")]
        //检查仪器电源
        public UnityEvent enter3;

        [Tooltip("开始检查仪器样品室")]
        //检查仪器样品室
        public UnityEvent enter4;

    #endregion

    private void Awake()
    {
        eventUI = GameObject.Find("Canvas").transform.GetChild(2).GetComponent<EventUI>();
        RegisterEvents();
    }

    public void RegisterEvents()
    {
        // 讲解实验目的和意义，说明实验内容和学习目标。
        enter0.AddListener(() =>
        {
            Debug.Log($"介绍实验目的 触发: 讲解实验目的和意义，说明实验内容和学习目标。");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查器具齐全",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["实验桌"],linkedEventNames,eventUI,"实验桌");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查实验桌上的所有器具
        enter1.AddListener(() =>
        {
            Debug.Log($"开始检查器具 触发: 实验人员开始检查实验桌上的所有器具");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查仪器外观
        enter2.AddListener(() =>
        {
            Debug.Log($"开始检查仪器外观 触发: 实验人员开始检查仪器外观");
            //此节点没有后续节点
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查仪器电源是否插入
        enter3.AddListener(() =>
        {
            Debug.Log($"开始检查仪器电源 触发: 实验人员开始检查仪器电源是否插入");
            //此节点没有后续节点
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查仪器样品室
        enter4.AddListener(() =>
        {
            Debug.Log($"开始检查仪器样品室 触发: 实验人员开始检查仪器样品室");
            //此节点没有后续节点
            // TODO: 在这里添加自定义逻辑
        });

    }
}
