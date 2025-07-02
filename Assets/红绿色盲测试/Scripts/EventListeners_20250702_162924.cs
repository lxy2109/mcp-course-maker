using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class EventListeners_20250702_162924 : SceneSingleton<EventListeners_20250702_162924>, IEventListeners
{

    private EventUI eventUI;

    #region Events
        [Tooltip("介绍实验目的")]
        //了解实验目的及意义
        public UnityEvent enter0;

        [Tooltip("开始检查器材")]
        //检查器材完整性
        public UnityEvent enter1;

        [Tooltip("开始摆放测试卡")]
        //摆放测试卡
        public UnityEvent enter2;

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
            "检查器材完整性",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["色盲测试卡"],linkedEventNames,eventUI,"色盲测试卡");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查实验器材
        enter1.AddListener(() =>
        {
            Debug.Log($"开始检查器材 触发: 实验人员开始检查实验器材");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "摆放测试卡",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["色盲测试卡"],linkedEventNames,eventUI,"色盲测试卡");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始摆放色盲测试卡
        enter2.AddListener(() =>
        {
            Debug.Log($"开始摆放测试卡 触发: 实验人员开始摆放色盲测试卡");
            //此节点没有后续节点
            // TODO: 在这里添加自定义逻辑
        });

    }
}
