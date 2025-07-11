using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class EventListeners_20250711_162655 : SceneSingleton<EventListeners_20250711_162655>, IEventListeners
{

    private EventUI eventUI;

    #region Events
        [Tooltip("介绍实验目的")]
        //了解实验目的及意义
        public UnityEvent enter0;

        [Tooltip("开始检查器具齐全")]
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

        [Tooltip("检查紫外可见光分光仪后置事件")]
        //检查紫外可见光分光仪后置
        public UnityEvent enter5;

        [Tooltip("开始连接仪器电源")]
        //连接仪器电源
        public UnityEvent enter6;

        [Tooltip("开始按下电源按钮")]
        //按下电源按钮
        public UnityEvent enter7;

        [Tooltip("开始按下吸光度按钮")]
        //按下吸光度按钮
        public UnityEvent enter8;

        [Tooltip("开始设置波长值")]
        //设置波长值
        public UnityEvent enter9;

        [Tooltip("设置紫外可见光分光仪后置事件")]
        //设置紫外可见光分光仪后置
        public UnityEvent enter10;

        [Tooltip("开始打开样品室")]
        //打开样品室
        public UnityEvent enter11;

        [Tooltip("开始放入参比溶液1")]
        //放入参比溶液1
        public UnityEvent enter12;

        [Tooltip("开始放入参比溶液2")]
        //放入参比溶液2
        public UnityEvent enter13;

        [Tooltip("开始放入参比溶液3")]
        //放入参比溶液3
        public UnityEvent enter14;

        [Tooltip("开始关闭样品室")]
        //关闭样品室
        public UnityEvent enter15;

        [Tooltip("开始按下调零按钮")]
        //按下调零按钮
        public UnityEvent enter16;

        [Tooltip("按下调零按钮Exit")]
        //按下调零按钮
        public UnityEvent exit16;

        [Tooltip("准备启动紫外可见光分光仪后置事件")]
        //准备启动紫外可见光分光仪后置
        public UnityEvent enter17;

        [Tooltip("开始拉动样品杆")]
        //拉动样品杆
        public UnityEvent enter18;

        [Tooltip("拉动样品杆Exit")]
        //拉动样品杆
        public UnityEvent exit18;

        [Tooltip("开始再次拉动样品杆")]
        //再次拉动样品杆
        public UnityEvent enter19;

        [Tooltip("再次拉动样品杆Exit")]
        //再次拉动样品杆
        public UnityEvent exit19;

        [Tooltip("开始第三次拉动样品杆")]
        //第三次拉动样品杆
        public UnityEvent enter20;

        [Tooltip("第三次拉动样品杆Exit")]
        //第三次拉动样品杆
        public UnityEvent exit20;

        [Tooltip("开始实验后置事件")]
        //开始实验后置
        public UnityEvent enter21;

        [Tooltip("开始记录测量值")]
        //记录测量值
        public UnityEvent enter22;

        [Tooltip("开始关闭电源开关")]
        //关闭电源开关
        public UnityEvent enter23;

        [Tooltip("开始再次打开样品室")]
        //再次打开样品室
        public UnityEvent enter24;

        [Tooltip("开始移除三个样品溶液")]
        //移除三个样品溶液
        public UnityEvent enter25;

        [Tooltip("开始擦拭样品室")]
        //擦拭样品室
        public UnityEvent enter26;

        [Tooltip("开始扔掉擦拭过的棉球")]
        //扔掉擦拭过的棉球
        public UnityEvent enter27;

        [Tooltip("清理紫外可见光分光仪后置事件")]
        //清理紫外可见光分光仪后置
        public UnityEvent enter28;

        [Tooltip("开始再次关闭样品室")]
        //再次关闭样品室
        public UnityEvent enter29;

        [Tooltip("开始倒掉比色皿溶液")]
        //倒掉比色皿溶液
        public UnityEvent enter30;

        [Tooltip("开始洗净比色皿")]
        //洗净比色皿
        public UnityEvent enter31;

        [Tooltip("清洗比色皿后置事件")]
        //清洗比色皿后置
        public UnityEvent enter32;

        [Tooltip("开始完成实验")]
        //完成实验
        public UnityEvent enter33;

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

        // 实验人员开始检查实验台上的所有器具
        enter1.AddListener(() =>
        {
            Debug.Log($"开始检查器具齐全 触发: 实验人员开始检查实验台上的所有器具");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查紫外可见光分光仪外观
        enter2.AddListener(() =>
        {
            Debug.Log($"开始检查仪器外观 触发: 实验人员开始检查紫外可见光分光仪外观");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查分光仪电源连接
        enter3.AddListener(() =>
        {
            Debug.Log($"开始检查仪器电源 触发: 实验人员开始检查分光仪电源连接");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始检查样品室
        enter4.AddListener(() =>
        {
            Debug.Log($"开始检查仪器样品室 触发: 实验人员开始检查样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter5.AddListener(() =>
        {
            Debug.Log($"检查紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始连接电源线
        enter6.AddListener(() =>
        {
            Debug.Log($"开始连接仪器电源 触发: 实验人员开始连接电源线");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始按下电源按钮
        enter7.AddListener(() =>
        {
            Debug.Log($"开始按下电源按钮 触发: 实验人员开始按下电源按钮");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始按下吸光度按钮
        enter8.AddListener(() =>
        {
            Debug.Log($"开始按下吸光度按钮 触发: 实验人员开始按下吸光度按钮");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始设置波长
        enter9.AddListener(() =>
        {
            Debug.Log($"开始设置波长值 触发: 实验人员开始设置波长");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter10.AddListener(() =>
        {
            Debug.Log($"设置紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "打开样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始打开样品室
        enter11.AddListener(() =>
        {
            Debug.Log($"开始打开样品室 触发: 实验人员开始打开样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液1",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始放入比色皿1
        enter12.AddListener(() =>
        {
            Debug.Log($"开始放入参比溶液1 触发: 实验人员开始放入比色皿1");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液2",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿2"],linkedEventNames,eventUI,"比色皿2");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始放入比色皿2
        enter13.AddListener(() =>
        {
            Debug.Log($"开始放入参比溶液2 触发: 实验人员开始放入比色皿2");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液3",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿3"],linkedEventNames,eventUI,"比色皿3");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始放入比色皿3
        enter14.AddListener(() =>
        {
            Debug.Log($"开始放入参比溶液3 触发: 实验人员开始放入比色皿3");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭样品室",
            "按下调零按钮",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始关闭样品室
        enter15.AddListener(() =>
        {
            Debug.Log($"开始关闭样品室 触发: 实验人员开始关闭样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭样品室",
            "按下调零按钮",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始按下调零按钮
        enter16.AddListener(() =>
        {
            Debug.Log($"开始按下调零按钮 触发: 实验人员开始按下调零按钮");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭样品室",
            "按下调零按钮",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 数值变化：波长值由540变为0
        exit16.AddListener(() =>
        {
            EventManager.instance.baseEventService.AnimateTextValue(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],540,0);
        });

        // 
        enter17.AddListener(() =>
        {
            Debug.Log($"准备启动紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
            "第三次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始拉动样品杆
        enter18.AddListener(() =>
        {
            Debug.Log($"开始拉动样品杆 触发: 实验人员开始拉动样品杆");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
            "第三次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 数值变化：吸光度由0变为0.125
        exit18.AddListener(() =>
        {
            EventManager.instance.baseEventService.AnimateTextValue(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],0,0);
        });

        // 实验人员再次拉动样品杆
        enter19.AddListener(() =>
        {
            Debug.Log($"开始再次拉动样品杆 触发: 实验人员再次拉动样品杆");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
            "第三次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 数值变化：吸光度由0.125变为0.250
        exit19.AddListener(() =>
        {
            EventManager.instance.baseEventService.AnimateTextValue(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],0,125);
        });

        // 实验人员第三次拉动样品杆
        enter20.AddListener(() =>
        {
            Debug.Log($"开始第三次拉动样品杆 触发: 实验人员第三次拉动样品杆");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
            "第三次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 数值变化：吸光度由0.250变为0.375
        exit20.AddListener(() =>
        {
            EventManager.instance.baseEventService.AnimateTextValue(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],0,250);
        });

        // 
        enter21.AddListener(() =>
        {
            Debug.Log($"开始实验后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "记录测量值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["实验记录本"],linkedEventNames,eventUI,"实验记录本");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始记录测量数据
        enter22.AddListener(() =>
        {
            Debug.Log($"开始记录测量值 触发: 实验人员开始记录测量数据");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭电源开关",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始关闭电源
        enter23.AddListener(() =>
        {
            Debug.Log($"开始关闭电源开关 触发: 实验人员开始关闭电源");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "再次打开样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始打开样品室取出样品
        enter24.AddListener(() =>
        {
            Debug.Log($"开始再次打开样品室 触发: 实验人员开始打开样品室取出样品");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "移除三个样品溶液",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始取出所有比色皿
        enter25.AddListener(() =>
        {
            Debug.Log($"开始移除三个样品溶液 触发: 实验人员开始取出所有比色皿");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "擦拭样品室",
            "扔掉擦拭过的棉球",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["擦拭棉球"],linkedEventNames,eventUI,"擦拭棉球");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始清洁样品室
        enter26.AddListener(() =>
        {
            Debug.Log($"开始擦拭样品室 触发: 实验人员开始清洁样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "擦拭样品室",
            "扔掉擦拭过的棉球",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["擦拭棉球"],linkedEventNames,eventUI,"擦拭棉球");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始处理用过的棉球
        enter27.AddListener(() =>
        {
            Debug.Log($"开始扔掉擦拭过的棉球 触发: 实验人员开始处理用过的棉球");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "擦拭样品室",
            "扔掉擦拭过的棉球",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["擦拭棉球"],linkedEventNames,eventUI,"擦拭棉球");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter28.AddListener(() =>
        {
            Debug.Log($"清理紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "再次关闭样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员再次关闭样品室
        enter29.AddListener(() =>
        {
            Debug.Log($"开始再次关闭样品室 触发: 实验人员再次关闭样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始处理比色皿中的溶液
        enter30.AddListener(() =>
        {
            Debug.Log($"开始倒掉比色皿溶液 触发: 实验人员开始处理比色皿中的溶液");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员开始清洗比色皿
        enter31.AddListener(() =>
        {
            Debug.Log($"开始洗净比色皿 触发: 实验人员开始清洗比色皿");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter32.AddListener(() =>
        {
            Debug.Log($"清洗比色皿后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "完成实验",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["实验记录本"],linkedEventNames,eventUI,"实验记录本");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员完成所有实验操作
        enter33.AddListener(() =>
        {
            Debug.Log($"开始完成实验 触发: 实验人员完成所有实验操作");
            //此节点没有后续节点
            // TODO: 在这里添加自定义逻辑
        });

    }
}
