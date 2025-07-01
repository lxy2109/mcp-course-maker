using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class UnityEventListeners : SceneSingleton<UnityEventListeners>,IEventListeners
{

    private EventUI eventUI;

    #region Events
        [Tooltip("佩戴手套")]
        //佩戴一次性试验手套
        public UnityEvent enter0;

        [Tooltip("检查外观")]
        //检查仪器外观
        public UnityEvent enter1;

        [Tooltip("检查电源")]
        //检查仪器电源
        public UnityEvent enter2;

        [Tooltip("检查样品室")]
        //检查仪器样品室
        public UnityEvent enter3;

        [Tooltip("检查紫外可见光分光仪后置事件")]
        //检查紫外可见光分光仪后置
        public UnityEvent enter4;

        [Tooltip("连接电源")]
        //连接仪器电源
        public UnityEvent enter5;

        [Tooltip("按下电源按钮")]
        //按下电源按钮
        public UnityEvent enter6;

        [Tooltip("按下吸光度按钮")]
        //按下吸光度按钮
        public UnityEvent enter7;

        [Tooltip("设置波长")]
        //设置波长值
        public UnityEvent enter8;

        [Tooltip("设置紫外可见光分光仪后置事件")]
        //设置紫外可见光分光仪后置
        public UnityEvent enter9;

        [Tooltip("再次打开样品室")]
        //打开样品室
        public UnityEvent enter10;

        [Tooltip("放入比色皿1")]
        //放入参比溶液1
        public UnityEvent enter11;

        [Tooltip("放入比色皿2")]
        //放入参比溶液2
        public UnityEvent enter12;

        [Tooltip("放入比色皿3")]
        //放入参比溶液3
        public UnityEvent enter13;

        [Tooltip("放入参比溶液后置事件")]
        //放入参比溶液后置
        public UnityEvent enter14;

        [Tooltip("关闭样品室")]
        //关闭样品室
        public UnityEvent enter15;

        [Tooltip("按下调零按钮")]
        //按下调零按钮
        public UnityEvent enter16;

        [Tooltip("按下调零按钮Exit")]
        //按下调零按钮
        public UnityEvent exit16;

        [Tooltip("准备启动紫外可见光分光仪后置事件")]
        //准备启动紫外可见光分光仪后置
        public UnityEvent enter17;

        [Tooltip("拉动样品杆")]
        //拉动样品杆
        public UnityEvent enter18;

        [Tooltip("拉动样品杆Exit")]
        //拉动样品杆
        public UnityEvent exit18;

        [Tooltip("再次拉动样品杆")]
        //再次拉动样品杆
        public UnityEvent enter19;

        [Tooltip("再次拉动样品杆Exit")]
        //再次拉动样品杆
        public UnityEvent exit19;

        [Tooltip("开始实验后置事件")]
        //开始实验后置
        public UnityEvent enter20;

        [Tooltip("记录数据")]
        //记录测量值
        public UnityEvent enter21;

        [Tooltip("关闭电源")]
        //关闭电源开关
        public UnityEvent enter22;

        [Tooltip("再次打开样品室")]
        //打开样品室
        public UnityEvent enter23;

        [Tooltip("移除比色皿3")]
        //移除三个样品溶液
        public UnityEvent enter24;

        [Tooltip("移除比色皿3")]
        //移除三个样品溶液
        public UnityEvent enter25;

        [Tooltip("移除比色皿3")]
        //移除三个样品溶液
        public UnityEvent enter26;

        [Tooltip("关闭紫外可见光分光仪后置事件")]
        //关闭紫外可见光分光仪后置
        public UnityEvent enter27;

        [Tooltip("擦拭样品室")]
        //擦拭样品室
        public UnityEvent enter28;

        [Tooltip("扔掉棉球")]
        //扔掉擦拭过的棉球
        public UnityEvent enter29;

        [Tooltip("清理紫外可见光分光仪后置事件")]
        //清理紫外可见光分光仪后置
        public UnityEvent enter30;

        [Tooltip("关闭样品室2")]
        //再次关闭样品室
        public UnityEvent enter31;

        [Tooltip("倒掉比色皿2溶液")]
        //倒掉比色皿溶液
        public UnityEvent enter32;

        [Tooltip("倒掉比色皿2溶液")]
        //倒掉比色皿溶液
        public UnityEvent enter33;

        [Tooltip("清洗比色皿")]
        //洗净比色皿
        public UnityEvent enter34;

        [Tooltip("清洗比色皿后置事件")]
        //清洗比色皿后置
        public UnityEvent enter35;

        [Tooltip("实验结束")]
        //完成实验
        public UnityEvent enter36;

        [Tooltip("介绍实验目的")]
        //了解实验目的及意义
        public UnityEvent enter38;

        [Tooltip("检查器具")]
        //检查器具齐全
        public UnityEvent enter39;

    #endregion

    private void Awake()
    {
        eventUI = GameObject.Find("Canvas").transform.GetChild(1).GetComponent<EventUI>();
        RegisterEvents();
    }

    public  void RegisterEvents()
    {
        // 实验人员佩戴一次性试验手套
        enter0.AddListener(() =>
        {
            Debug.Log($"佩戴手套 触发: 实验人员佩戴一次性试验手套");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员检查仪器外观
        enter1.AddListener(() =>
        {
            Debug.Log($"检查外观 触发: 实验人员检查仪器外观");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员检查电源线连接情况
        enter2.AddListener(() =>
        {
            Debug.Log($"检查电源 触发: 实验人员检查电源线连接情况");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查仪器外观",
            "检查仪器电源",
            "检查仪器样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["电源线"],linkedEventNames,eventUI,"电源线");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员打开样品室盖并检查
        enter3.AddListener(() =>
        {
            Debug.Log($"检查样品室 触发: 实验人员打开样品室盖并检查");
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
        enter4.AddListener(() =>
        {
            Debug.Log($"检查紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["电源线"],linkedEventNames,eventUI,"电源线");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员插入电源线
        enter5.AddListener(() =>
        {
            Debug.Log($"连接电源 触发: 实验人员插入电源线");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "连接仪器电源",
            "按下电源按钮",
            "按下吸光度按钮",
            "设置波长值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["电源线"],linkedEventNames,eventUI,"电源线");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员按下分光仪电源按钮
        enter6.AddListener(() =>
        {
            Debug.Log($"按下电源按钮 触发: 实验人员按下分光仪电源按钮");
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

        // 实验人员按下吸光度按钮
        enter7.AddListener(() =>
        {
            Debug.Log($"按下吸光度按钮 触发: 实验人员按下吸光度按钮");
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

        // 实验人员旋转旋钮设置波长
        enter8.AddListener(() =>
        {
            Debug.Log($"设置波长 触发: 实验人员旋转旋钮设置波长");
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
        enter9.AddListener(() =>
        {
            Debug.Log($"设置紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "打开样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员再次打开样品室盖
        enter10.AddListener(() =>
        {
            Debug.Log($"再次打开样品室 触发: 实验人员再次打开样品室盖");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液1",
            "放入参比溶液2",
            "放入参比溶液3",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员将比色皿1放入样品室
        enter11.AddListener(() =>
        {
            Debug.Log($"放入比色皿1 触发: 实验人员将比色皿1放入样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液1",
            "放入参比溶液2",
            "放入参比溶液3",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员将比色皿2放入样品室
        enter12.AddListener(() =>
        {
            Debug.Log($"放入比色皿2 触发: 实验人员将比色皿2放入样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液1",
            "放入参比溶液2",
            "放入参比溶液3",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿2"],linkedEventNames,eventUI,"比色皿2");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员将比色皿3放入样品室
        enter13.AddListener(() =>
        {
            Debug.Log($"放入比色皿3 触发: 实验人员将比色皿3放入样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液1",
            "放入参比溶液2",
            "放入参比溶液3",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿3"],linkedEventNames,eventUI,"比色皿3");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter14.AddListener(() =>
        {
            Debug.Log($"放入参比溶液后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭样品室",
            "按下调零按钮",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员关闭样品室盖
        enter15.AddListener(() =>
        {
            Debug.Log($"关闭样品室 触发: 实验人员关闭样品室盖");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭样品室",
            "按下调零按钮",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员按下调零按钮
        enter16.AddListener(() =>
        {
            Debug.Log($"按下调零按钮 触发: 实验人员按下调零按钮");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭样品室",
            "按下调零按钮",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 波长值由1000变为0
        exit16.AddListener(() =>
        {
        });

        // 
        enter17.AddListener(() =>
        {
            Debug.Log($"准备启动紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员拉动样品杆
        enter18.AddListener(() =>
        {
            Debug.Log($"拉动样品杆 触发: 实验人员拉动样品杆");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 波长值由0变为480
        exit18.AddListener(() =>
        {
        });

        // 实验人员再次拉动样品杆
        enter19.AddListener(() =>
        {
            Debug.Log($"再次拉动样品杆 触发: 实验人员再次拉动样品杆");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "拉动样品杆",
            "再次拉动样品杆",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 波长值由480变为540
        exit19.AddListener(() =>
        {
        });

        // 
        enter20.AddListener(() =>
        {
            Debug.Log($"开始实验后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "记录测量值",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["笔记本"],linkedEventNames,eventUI,"笔记本");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员在笔记本上记录数据
        enter21.AddListener(() =>
        {
            Debug.Log($"记录数据 触发: 实验人员在笔记本上记录数据");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭电源开关",
            "打开样品室",
            "移除三个样品溶液",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员关闭分光仪电源
        enter22.AddListener(() =>
        {
            Debug.Log($"关闭电源 触发: 实验人员关闭分光仪电源");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭电源开关",
            "打开样品室",
            "移除三个样品溶液",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员再次打开样品室盖
        enter23.AddListener(() =>
        {
            Debug.Log($"再次打开样品室 触发: 实验人员再次打开样品室盖");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "放入参比溶液1",
            "放入参比溶液2",
            "放入参比溶液3",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿1"],linkedEventNames,eventUI,"比色皿1");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员移除比色皿3
        enter24.AddListener(() =>
        {
            Debug.Log($"移除比色皿3 触发: 实验人员移除比色皿3");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭电源开关",
            "打开样品室",
            "移除三个样品溶液",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿3"],linkedEventNames,eventUI,"比色皿3");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员移除比色皿3
        enter25.AddListener(() =>
        {
            Debug.Log($"移除比色皿3 触发: 实验人员移除比色皿3");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭电源开关",
            "打开样品室",
            "移除三个样品溶液",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿3"],linkedEventNames,eventUI,"比色皿3");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员移除比色皿3
        enter26.AddListener(() =>
        {
            Debug.Log($"移除比色皿3 触发: 实验人员移除比色皿3");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "关闭电源开关",
            "打开样品室",
            "移除三个样品溶液",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿3"],linkedEventNames,eventUI,"比色皿3");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter27.AddListener(() =>
        {
            Debug.Log($"关闭紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "擦拭样品室",
            "扔掉擦拭过的棉球",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["擦拭棉球"],linkedEventNames,eventUI,"擦拭棉球");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员用棉球擦拭样品室
        enter28.AddListener(() =>
        {
            Debug.Log($"擦拭样品室 触发: 实验人员用棉球擦拭样品室");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "擦拭样品室",
            "扔掉擦拭过的棉球",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["擦拭棉球"],linkedEventNames,eventUI,"擦拭棉球");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员将棉球扔进废液烧杯
        enter29.AddListener(() =>
        {
            Debug.Log($"扔掉棉球 触发: 实验人员将棉球扔进废液烧杯");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "擦拭样品室",
            "扔掉擦拭过的棉球",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["擦拭棉球"],linkedEventNames,eventUI,"擦拭棉球");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter30.AddListener(() =>
        {
            Debug.Log($"清理紫外可见光分光仪后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "再次关闭样品室",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["紫外可见光分光仪"],linkedEventNames,eventUI,"紫外可见光分光仪");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员关闭样品室盖
        enter31.AddListener(() =>
        {
            Debug.Log($"关闭样品室2 触发: 实验人员关闭样品室盖");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿2"],linkedEventNames,eventUI,"比色皿2");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员将比色皿2中的溶液倒入废液烧杯
        enter32.AddListener(() =>
        {
            Debug.Log($"倒掉比色皿2溶液 触发: 实验人员将比色皿2中的溶液倒入废液烧杯");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿2"],linkedEventNames,eventUI,"比色皿2");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员将比色皿2中的溶液倒入废液烧杯
        enter33.AddListener(() =>
        {
            Debug.Log($"倒掉比色皿2溶液 触发: 实验人员将比色皿2中的溶液倒入废液烧杯");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["比色皿2"],linkedEventNames,eventUI,"比色皿2");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员用洗瓶冲洗比色皿
        enter34.AddListener(() =>
        {
            Debug.Log($"清洗比色皿 触发: 实验人员用洗瓶冲洗比色皿");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "倒掉比色皿溶液",
            "洗净比色皿",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["塑料洗瓶"],linkedEventNames,eventUI,"塑料洗瓶");
            // TODO: 在这里添加自定义逻辑
        });

        // 
        enter35.AddListener(() =>
        {
            Debug.Log($"清洗比色皿后置事件 触发: ");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "完成实验",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["实验桌"],linkedEventNames,eventUI,"实验桌");
            // TODO: 在这里添加自定义逻辑
        });

        // 实验人员整理实验桌
        enter36.AddListener(() =>
        {
            Debug.Log($"实验结束 触发: 实验人员整理实验桌");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject[""],linkedEventNames,eventUI,"");
            // TODO: 在这里添加自定义逻辑
        });

        // 讲解实验目的和意义
        enter38.AddListener(() =>
        {
            Debug.Log($"介绍实验目的 触发: 讲解实验目的和意义");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "检查器具齐全",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["实验桌"],linkedEventNames,eventUI,"实验桌");
            // TODO: 在这里添加自定义逻辑
        });

        // 检查实验桌上器具
        enter39.AddListener(() =>
        {
            Debug.Log($"检查器具 触发: 检查实验桌上器具");
            // 处理链接事件
            List<string> linkedEventNames = new List<string>(){
            "佩戴一次性试验手套",
              };
             EventManager.instance.baseEventService.HoldForClickedEvent(GameObjectPool.instance.allNeedObject["一次性试验手套"],linkedEventNames,eventUI,"一次性试验手套");
            // TODO: 在这里添加自定义逻辑
        });

    }
}
