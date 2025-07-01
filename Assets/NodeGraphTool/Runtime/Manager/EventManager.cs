using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using System;
using Sirenix.Utilities;
using InspectorExtra;
using DG.Tweening;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine.Events;
using System.Linq;
using UnityEditor.Timeline.Actions;

#if UNITY_EDITOR
#endif

/// <summary>
/// 事件管理器 - 场景单例类
/// 用于管理基于NodeGraph的事件系统，控制事件流程、UI显示和音频播放
/// </summary>
public class EventManager : SceneSingleton<EventManager>
{
    /// <summary>
    /// Graph池，存储所有可用的事件图表
    /// 默认包含一个Key为"默认"的Graph
    /// </summary>
    [TabGroup("设置")]
    [Header("Graph 池，默认包含一个Key为 默认 的Graph")]
    [Header("请将 默认 的graph替换为自己的graph")]
    public Dictionary<string, NodeGraph.NodeGraph> graphs = new Dictionary<string, NodeGraph.NodeGraph>();

    /// <summary>
    /// 是否启用高级设置
    /// </summary>
    [TabGroup("设置")]
    [LabelText("高级设置")]
    public bool advance = false;

    /// <summary>
    /// 是否在启用时自动加载第一个事件
    /// </summary>
    [ShowIf("advance", true)]
    [TabGroup("设置")]
    [LabelText("开始时自动加载第一个事件")]
    public bool autoLoadonEnable = false;


    #region UI
    /// <summary>
    /// 事件文本内容UI组件
    /// </summary>
    [SerializeField]
    [TabGroup("UI")]
    [LabelText("Event 文字内容UI")]
    public TextMeshProUGUI contentUI;
    
    private Transform showUI;

    /// <summary>
    /// 事件音频播放组件
    /// </summary>
    [SerializeField]
    [TabGroup("UI")]
    [LabelText("音频播放")]
    public AudioSource eventAudio;
    #endregion

    #region Graph
    /// <summary>
    /// 当前活动的事件图表
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
#if UNITY_EDITOR
    [FieldName("当前的事件图")]
#endif
    public NodeGraph.NodeGraph currentGraph;

    /// <summary>
    /// 是否使用默认设置
    /// 默认设置为将Key为"默认"的Graph作为当前图，并在开始时执行
    /// </summary>
    [ShowIf("advance",true)]
    [TabGroup("设置")]
    [LabelText("使用默认设置")]
    [Tooltip("默认设置为将个Key为 默认 的Graph作为当前加载图，并在开始执行默认Graph")]
    public bool isDefaultSetting = true;
    #endregion

    #region Nodes
    /// <summary>
    /// 所有流事件节点数据列表
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
    private List<FlowEventNodeData> flowEventNodeDatas = new List<FlowEventNodeData>();

    /// <summary>
    /// 记录每个事件是否已触发
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
    private List<bool>isEventTrigger = new List<bool>();

    /// <summary>
    /// 当前流事件索引
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
    public int floweventIndex = 0;

    /// <summary>
    /// 当前活动的流事件节点数据
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
    private FlowEventNodeData currentFlowEvent;

    /// <summary>
    /// 组合事件组列表
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
    private List<CombineGroup> combineGroups = new List<CombineGroup>();

    /// <summary>
    /// 状态组列表
    /// </summary>
    [TabGroup("Debug")]
    [SerializeField]
    private List<StateGroup> stateGroups = new List<StateGroup>();
    #endregion
    

    [SerializeField]
    [TabGroup("UI")]
    [LabelText("高亮圆锥体")]
    private GameObject highlightCone;   
    private Vector3 targetpos;
    private Vector3 originalpos;

    // 新增：UI动画服务类
    private UIEventService uiService;
    private TimelineService timelineService;  
    private HighlightService highlightService;
    public BaseEventService baseEventService;

    /// <summary>
    /// 启动时初始化
    /// </summary>
    private void Start()
    {
        // Debug.LogFormat("<color=blue>【事件管理器】启动</color>");
        if (isDefaultSetting)
        {
            // Debug.LogFormat("<color=blue>【事件管理器】使用默认设置进行初始化</color>");
            DefaultInit();
        }
        else
        {
            // Debug.LogFormat("<color=blue>【事件管理器】使用自定义设置，跳过自动初始化</color>");
        }
        showUI = GameObject.Find("Canvas").transform.GetChild(3);
        targetpos = GameObject.Find("Canvas").transform.GetChild(5).transform.position;
        originalpos = GameObject.Find("Canvas").transform.GetChild(4).transform.position;

        uiService = new UIEventService(showUI, targetpos, originalpos);
        timelineService = new TimelineService();
        highlightService = new HighlightService(highlightCone);
        baseEventService = new BaseEventService();
        highlightService.SetCoroutineHost(this);

        // 注册事件
        uiService.OnUIAnimationComplete += () => Debug.Log("UI动画完成");
        timelineService.OnTimelineComplete += (data) => Debug.Log($"Timeline完成: {data.eventName}");

        
        AddEvents();
    }

    /// <summary>
    /// 使用默认设置进行初始化
    /// </summary>
    public void DefaultInit()
    {
        Clear();
        Init();
        if (autoLoadonEnable)
        {
            LoadFlowEvent(0);
        }  
    }

    /// <summary>
    /// 获取当前图表中所有状态组
    /// </summary>
    [TabGroup("Debug")]
    [Button]
    public void GetAllStateGroups()
    {
        // Debug.LogFormat("<color=blue>【事件管理器】获取所有状态组</color>");
        
        if (currentGraph == null)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：当前Graph为空，无法获取状态组</color>");
            return;
        }
        
        stateGroups = BaseGraphLoader.GetAllStateGroups(currentGraph);
        // Debug.LogFormat("<color=blue>【事件管理器】获取到 {0} 个状态组</color>", stateGroups.Count);
    }

    /// <summary>
    /// 根据状态名获取状态组
    /// </summary>
    /// <param name="stateName">状态名称</param>
    /// <returns>对应的状态组</returns>
    [TabGroup("Debug")]
    [Button]
    public StateGroup GetStateGroup(string stateName)
    {   
        if (currentGraph == null)
        {
            return null;
        }

        var group = BaseGraphLoader.GetNodeGroupinStateNode(currentGraph, stateName);
        
        return group;
    }

    /// <summary>
    /// 初始化事件管理器，加载图表和事件数据。
    /// </summary>
    [TabGroup("Debug")]
    [Button("初始化")]
    public void Init()
    {
        Clear();
        if (isDefaultSetting)
        {
            if (graphs.ContainsKey("默认"))
            {
                if (graphs["默认"] != null) currentGraph = graphs["默认"];
            }
        }
        flowEventNodeDatas = BaseGraphLoader.GetFlowEventNodeDatas(currentGraph);
        for (int i = 0; i < flowEventNodeDatas.Count; i++)//wtw
        {
            isEventTrigger.Add(false);
        }
        combineGroups=BaseGraphLoader.GetAllCombineGroups(currentGraph);
        GetAllStateGroups();
    }

    /// <summary>
    /// 清除当前事件管理器的所有数据。
    /// </summary>
    [TabGroup("Debug")]
    [Button("清除")]
    public void Clear()
    {
        currentGraph = null;
        if(flowEventNodeDatas != null)
            flowEventNodeDatas.Clear();
        isEventTrigger.Clear();
        combineGroups=null;
    }
    
    /// <summary>
    /// 设置下一个事件索引。
    /// </summary>
    public void SetNetEventIndex()
    {
        floweventIndex++;
        // Debug.LogFormat("<color=green>【事件管理器】设置下一个事件索引为: {0}</color>", floweventIndex);
    }

    /// <summary>
    /// 获取当前事件名称。
    /// </summary>
    /// <returns>当前事件名称，如果没有则返回"Default"</returns>
    public string GetCurrentEventName()
    {
        if (currentFlowEvent == null)
        {
            // Debug.LogFormat("<color=green>【事件管理器】当前事件为空，返回Default</color>");
            return "Default";
        }

        // Debug.LogFormat("<color=green>【事件管理器】当前事件名称: {0}</color>", currentFlowEvent.eventName);
        return currentFlowEvent.eventName;
    }

    /// <summary>
    /// 通过索引加载流事件节点，执行UI、音频、事件、动画等流程。
    /// </summary>
    /// <param name="index">要加载的事件节点索引</param>
    [TabGroup("Debug")]
    [Button]
    public virtual void LoadFlowEvent(int index)
    {      
        // 检查索引是否有效
        if (flowEventNodeDatas == null || flowEventNodeDatas.Count == 0)
        {       
            return;
        }
        // 检查索引是否有效
        if (index < 0 || index >= flowEventNodeDatas.Count)
        {         
            return;
        }
        
        // 检查事件是否已触发，避免重复触发
        if (index >= 0 && index < isEventTrigger.Count)
        {
            if (isEventTrigger[index])
            {
              
                return;
            }
            else
            {
                isEventTrigger[index] = true;
                
            }
        }

        floweventIndex = index;
       
        currentFlowEvent = Copy(flowEventNodeDatas[index]);
       
        // 更新GameObjectPool中的对象
        if (GameObjectPool.instance)
        {           
            GameObjectPool.instance.UpdatePool();
        }
        else
        {
             Debug.LogFormat("<color=yellow>【事件管理器】GameObjectPool实例不存在，跳过更新池</color>");
        }

        // 切换节点前只终止并重置UI动画和高亮动画
        uiService.StopUIAnimationAndReset();

        if (highlightService != null)
        {
            highlightService.HideHighlight();
        }
        // 播放事件音频，先停止前一个音频
        if (eventAudio)
        {
            eventAudio.Stop();
            if (currentFlowEvent != null && currentFlowEvent.inAudioClip)
            {
                eventAudio.PlayOneShot(currentFlowEvent.inAudioClip);
            }
        }


        // 执行事件进场行为
        if (currentFlowEvent != null && !string.IsNullOrEmpty(currentFlowEvent.enterEventName))
        {
            if (EventInvokerManager.instance)
            {
                EventInvokerManager.instance.InvokeEvent(currentFlowEvent.enterEventName);
            }
        }


        if (currentFlowEvent != null && currentFlowEvent.eventName.Contains("后置"))
        {
            // 后置节点不切换UI显示
        }
        else
        {
            ShowStartContent();
        }

        if (currentFlowEvent != null && !currentFlowEvent.eventName.Contains("后置"))
        {
            uiService.PlayUIAnimation();
        }
        PlayTimelines(currentFlowEvent);
    }

    /// <summary>
    /// 通过事件名称加载流事件节点，执行UI、音频、事件、动画等流程。
    /// </summary>
    /// <param name="eventName">要加载的事件节点名称</param>
    [TabGroup("Debug")]
    [Button]
    public virtual void LoadFlowEvent(string eventName)
    {
        // Debug.LogFormat("<color=green>【事件管理器】尝试通过名称加载事件: {0}</color>", eventName);
        
        // 检查事件数据是否有效
        if (flowEventNodeDatas == null || flowEventNodeDatas.Count == 0)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：流事件数据列表为空</color>");
            return;
        }
        
        // 查找事件索引
        int index = flowEventNodeDatas.FindIndex(t => t.eventName == eventName);
        // Debug.LogFormat("<color=green>【事件管理器】事件名称 {0} 对应的索引: {1}</color>", eventName, index);

        if (index == -1)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：找不到名为 {0} 的事件</color>", eventName);
            return;
        }

        // 检查事件是否已触发
        if (index >= 0 && index < isEventTrigger.Count)
        {
            if (isEventTrigger[index])
            {
                // Debug.LogFormat("<color=yellow>【事件管理器】事件 {0} 已触发过，跳过</color>", eventName);
                return;
            }
            else
            {
                isEventTrigger[index] = true;
                // Debug.LogFormat("<color=green>【事件管理器】标记事件 {0} 为已触发</color>", eventName);
            }
        }

        // Debug.LogFormat("<color=green>【事件管理器】通过名称加载事件: {0}</color>", eventName);
        floweventIndex = index;
        // Debug.LogFormat("<color=green>【事件管理器】设置当前事件索引为: {0}</color>", floweventIndex);
        
        // 拷贝FlowEvent数据
        // Debug.LogFormat("<color=gray>【事件管理器】开始复制事件数据: {0}</color>", flowEventNodeDatas[index].eventName);
        currentFlowEvent = Copy(flowEventNodeDatas[index]);
        // Debug.LogFormat("<color=gray>【事件管理器】事件数据复制完成</color>");

        // 切换节点前只终止并重置UI动画和高亮动画
        if(!eventName.Contains("后置"))    
            uiService.StopUIAnimationAndReset();

        if (highlightService != null)       
            highlightService.HideHighlight();
        
        // 播放事件音频，先停止前一个音频
        if (eventAudio)
        {
            eventAudio.Stop();
            if (currentFlowEvent != null && currentFlowEvent.inAudioClip)
            {
                eventAudio.PlayOneShot(currentFlowEvent.inAudioClip);
            }
        }
        // 执行事件进场行为
        if (currentFlowEvent != null && !string.IsNullOrEmpty(currentFlowEvent.enterEventName))
        {
            if (EventInvokerManager.instance)
            {
                EventInvokerManager.instance.InvokeEvent(currentFlowEvent.enterEventName);
            }
        }
        if (currentFlowEvent != null && currentFlowEvent.eventName.Contains("后置"))
        {
            // 后置节点不切换UI显示
        }
        else
        {
            ShowStartContent();
        }
        if (currentFlowEvent != null && !currentFlowEvent.eventName.Contains("后置"))
        {
            uiService.PlayUIAnimation();
        }
        PlayTimelines(currentFlowEvent);
    }

    /// <summary>
    /// 显示事件开始内容到UI。
    /// </summary>
    protected virtual void ShowStartContent()
    {
        if (contentUI == null)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：contentUI为空，无法显示事件内容</color>");
            return;
        }
        
        if (currentFlowEvent == null)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：当前事件为空，无法显示内容</color>");
            return;
        }
        
        // Debug.LogFormat("<color=yellow>【事件管理器】设置UI文本内容: {0}</color>", currentFlowEvent.eventContent);
        contentUI.text = currentFlowEvent.eventContent;
    }


    /// <summary>
    /// 事件结束时的处理逻辑，根据不同结束类型返回异步操作。
    /// </summary>
    /// <param name="data">当前事件节点数据</param>
    /// <returns>异步操作的委托</returns>
    protected virtual Func<Task> EndEvent(FlowEventNodeData data)
    {
        if (data == null)
            return null;
        switch (data.endActionEnum)
        {
            case EventEndAction.Hold:
                return async () =>
                {
                    if (!string.IsNullOrEmpty(data.exitEventName))
                    {
                        if (EventInvokerManager.instance != null)
                        {
                            EventInvokerManager.instance.InvokeEvent(data.exitEventName);
                            highlightService.ShowHighlight(data.selectableObects != null && data.selectableObects.Count > 0 ? data.selectableObects[0] : null);
                        }
                    }
                    else
                    {
                        highlightService.ShowHighlight(data.selectableObects != null && data.selectableObects.Count > 0 ? data.selectableObects[0] : null);
                    }
                };
            case EventEndAction.NextEvent:
                return async () =>
                {
                    if (!string.IsNullOrEmpty(data.exitEventName))
                    {
                        if (EventInvokerManager.instance != null)
                        {
                            EventInvokerManager.instance.InvokeEvent(data.exitEventName);
                        }
                        highlightService.ShowHighlight(data.selectableObects != null && data.selectableObects.Count > 0 ? data.selectableObects[0] : null);
                    }
                    SetNetEventIndex();
                    // 检查下一个节点是否需要等待UI动画
                    var nextNode = flowEventNodeDatas != null && floweventIndex < flowEventNodeDatas.Count ? flowEventNodeDatas[floweventIndex] : null;
                    bool needWaitUI = nextNode != null && !nextNode.eventName.Contains("后置") && (nextNode.timelineAssets == null || nextNode.timelineAssets.Count == 0);
                    if (needWaitUI && uiService != null)
                        await uiService.PlayUIAnimationAsync();
                    LoadFlowEvent(floweventIndex);
                };
            case EventEndAction.HoldForCombine:
                return async () =>
                {
                    RefreshCombine(data);
                    if (!string.IsNullOrEmpty(data.exitEventName))
                    {
                        if (EventInvokerManager.instance != null)
                        {
                            EventInvokerManager.instance.InvokeEvent(data.exitEventName);
                        }
                    }
                    highlightService.ShowHighlight(data.selectableObects != null && data.selectableObects.Count > 0 ? data.selectableObects[0] : null);
                    // 检查是否需要等待UI动画
                    var nextNode = flowEventNodeDatas != null && floweventIndex < flowEventNodeDatas.Count ? flowEventNodeDatas[floweventIndex] : null;
                    bool needWaitUI = nextNode != null && !nextNode.eventName.Contains("后置") && (nextNode.timelineAssets == null || nextNode.timelineAssets.Count == 0);
                    if (needWaitUI && uiService != null)
                        await uiService.PlayUIAnimationAsync();
                };
            default:
                return null;
        }
    }

    /// <summary>
    /// 刷新组合事件状态，检查组合条件是否满足。
    /// </summary>
    /// <param name="data">当前事件节点数据</param>
    protected virtual void RefreshCombine(FlowEventNodeData data)
    {
        // Debug.LogFormat("<color=purple>【事件管理器】开始刷新组合事件状态</color>");
        
        if (data == null)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：事件数据为空，无法刷新组合状态</color>");
            return;
        }
        
        // Debug.LogFormat("<color=purple>【事件管理器】当前事件: {0}</color>", data.eventName);
        
        if (combineGroups == null || combineGroups.Count == 0)
        {
            // Debug.LogFormat("<color=purple>【事件管理器】没有可用的组合组，结束刷新</color>");
            return;
        }
        
        // 查找包含当前事件的所有组合组
        // Debug.LogFormat("<color=purple>【事件管理器】开始查找包含事件 {0} 的组合组</color>", data.eventName);
        var groups = combineGroups.FindAll(t => t.inNodes.Exists(x => x.eventName == data.eventName));
        
        if (groups.IsNullOrEmpty())
        {
            // Debug.LogFormat("<color=purple>【事件管理器】没有找到包含事件 {0} 的组合组</color>", data.eventName);
            return;
        }
        
        // Debug.LogFormat("<color=purple>【事件管理器】找到 {0} 个包含当前事件的组合组</color>", groups.Count);
        
        foreach (var group in groups)
        {
            // Debug.LogFormat("<color=purple>【事件管理器】处理组合组，输入节点数: {0}</color>", group.inNodes.Count);
            
            if (group.eventsisDone == null || group.eventsisDone.Length == 0)
            {
                // Debug.LogFormat("<color=red>【事件管理器】错误：组合组的eventsisDone数组为空或长度为0</color>");
                continue;
            }
            
            // 获取当前事件在inNodes中的索引
            int eventIndex = group.inNodes.FindIndex(x => x.eventName == data.eventName);
            if (eventIndex >= 0 && eventIndex < group.eventsisDone.Length)
            {
                // Debug.LogFormat("<color=purple>【事件管理器】当前事件在组合组中的索引: {0}</color>", eventIndex);
            }
            
            for (int i = 0; i < group.eventsisDone.Length; i++)
            {
                // Debug.LogFormat("<color=purple>【事件管理器】检查组合事件状态[{0}]: {1}</color>", i, group.eventsisDone[i] ? "已完成" : "未完成");
                
                if (!group.eventsisDone[i])
                {
                    // Debug.LogFormat("<color=purple>【事件管理器】将组合事件状态[{0}]标记为已完成</color>", i);
                    group.eventsisDone[i] = true;
                    
                    if (i != group.eventsisDone.Length - 1)
                    {
                        // Debug.LogFormat("<color=purple>【事件管理器】不是最后一个事件，继续等待其他事件完成</color>");
                        return; // 如果不是最后一个事件，直接返回
                    }
                }
            }
            
            // 检查组合组是否全部完成
            bool allCompleted = true;
            for (int i = 0; i < group.eventsisDone.Length; i++)
            {
                if (!group.eventsisDone[i])
                {
                    allCompleted = false;
                    break;
                }
            }
            
            // Debug.LogFormat("<color=purple>【事件管理器】组合组事件状态检查: {0}</color>", allCompleted ? "全部完成" : "未全部完成");
            
            // 所有事件都完成，触发组合事件的输出节点
            if (allCompleted)
            {
                if (group.outNode != null)
                {
                    // Debug.LogFormat("<color=purple>【事件管理器】所有组合事件已完成，触发输出节点: {0}</color>", group.outNode.eventName);
                    LoadFlowEvent(group.outNode.eventName);
                }
                else
                {
                    // Debug.LogFormat("<color=red>【事件管理器】错误：组合组的输出节点为空</color>");
                }
            }
        }
        
        // Debug.LogFormat("<color=purple>【事件管理器】组合事件状态刷新完成</color>");
    }

    /// <summary>
    /// 复制流事件节点数据。
    /// </summary>
    /// <param name="source">源事件节点数据</param>
    /// <returns>复制的事件节点数据</returns>
    protected virtual FlowEventNodeData Copy(FlowEventNodeData source)
    {
        // Debug.LogFormat("<color=cyan>【事件管理器】开始复制事件数据: {0}</color>", source.eventName);
        
        if (source == null)
        {
            // Debug.LogFormat("<color=red>【事件管理器】错误：源事件数据为空，无法复制</color>");
            return null;
        }
        
        FlowEventNodeData target = new FlowEventNodeData();
        // Debug.LogFormat("<color=cyan>【事件管理器】创建新的FlowEventNodeData对象</color>");
        
        // 复制基本属性
        target.eventName = source.eventName;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制eventName: {0}</color>", source.eventName);
        
        // 复制可选择对象
        target.selectableObects = source.selectableObects;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制selectableObects列表，包含{0}个对象</color>", 
        //     source.selectableObects?.Count ?? 0);
        
        // 复制时间线资产
        target.timelineAssets = source.timelineAssets;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制timelineAssets列表，包含{0}个Timeline资产</color>", 
        //     source.timelineAssets?.Count ?? 0);
        
        // 复制内容文本
        target.eventContent = source.eventContent;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制content文本: \"{0}\"</color>", 
        //     string.IsNullOrEmpty(source.eventContent) ? "[空]" : source.eventContent);
        
        // 复制结束动作枚举
        target.endActionEnum = source.endActionEnum;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制endActionEnum: {0}</color>", source.endActionEnum);
        
        // 复制进场事件
        target.enterEventName = source.enterEventName;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制inEvent: {0}</color>", 
        //     string.IsNullOrEmpty(source.enterEventName) ? "[空]" : source.enterEventName);
        
        // 复制离场事件
        target.exitEventName = source.exitEventName;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制outEvent: {0}</color>", 
        //     string.IsNullOrEmpty(source.exitEventName) ? "[空]" : source.exitEventName);
        
        // 复制音频片段
        target.inAudioClip = source.inAudioClip;
        // Debug.LogFormat("<color=cyan>【事件管理器】复制inAudioClip: {0}</color>", 
        //     source.inAudioClip != null ? source.inAudioClip.name : "[空]");
        
        Debug.LogFormat("<color=cyan>【事件管理器】事件数据复制完成: {0}</color>", source.eventName);
        return target;
    }
        
    /// <summary>
    /// 播放Timeline动画，结束后执行事件结束逻辑。
    /// </summary>
    /// <param name="data">当前事件节点数据</param>
    protected virtual async void PlayTimelines(FlowEventNodeData data)
    {
        if (!TimelineManager.instance || data.timelineAssets == null || data.timelineAssets.Count == 0)
        {
            var action = EndEvent(data);
            if (action != null)
                await action();
            return;
        }
        TimelineManager.instance.PlayTimelines(data.timelineAssets.ToArray(), async () =>
        {
            // Timeline结束后，等待UI动画完整回到原位
            if (uiService != null)
                await uiService.PlayUIAnimationAsync();
            var action = EndEvent(data);
            if (action != null)
                await action();
        });
    }


    

    /// <summary>
    /// 查找所有实现了IEventListeners接口的组件并注册事件。
    /// </summary>
    public static void AddEvents()
    { 
        // 查找所有实现了IEventListeners接口的组件
        var eventListeners = FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb is IEventListeners)
            .Cast<IEventListeners>()
            .ToList();

        if (eventListeners.Count == 0)
        {
            throw new Exception("Cannot find any IEventListeners instance in scene!");
        }
        var dic = EventInvokerManager.GetInstanceInEditor().GetDic();
        int addCount = 0;

        foreach (var listener in eventListeners)
        {
            var listenerObj = listener as MonoBehaviour;
            var fields = listenerObj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(UnityEvent))
                {
                    var tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
                    string key = tooltipAttr != null ? tooltipAttr.tooltip : field.Name;
                    UnityEvent evt = (UnityEvent)field.GetValue(listenerObj);
                    if (evt != null && !dic.ContainsKey(key))
                    {
                        dic.Add(key, evt);
                        addCount++;
                       // Debug.Log($"添加事件: {key} from {listenerObj.GetType().Name}");
                    }
                }
            }
        }

        Debug.Log($"总共添加了 {addCount} 个事件，来自 {eventListeners.Count} 个监听器");
    }


    /// <summary>
    /// 重置整个实验到初始状态并重新开始第一个事件
    /// </summary>
    [TabGroup("Debug")]
    [Button("重置并重新开始实验")]
    public void ResetAndRestartExperiment()
    {
        Debug.LogFormat("<color=red>【事件管理器】开始重置并重新开始实验</color>");

        // 1. 重置事件管理器状态
        ResetEventManagerState();

        // 2. 重置物件池状态
        if (GameObjectPool.instance != null)
        {
            GameObjectPool.instance.ResetGameObjectPool();
        }
        else
        {
            Debug.LogFormat("<color=yellow>【事件管理器】GameObjectPool实例不存在，跳过物件池重置</color>");
        }

        // 3. 重置UI和高亮状态
        ResetUIAndHighlightState();

        // 4. 重新初始化事件系统
        if (isDefaultSetting)
        {
            // 重新初始化但不自动加载第一个事件
            Clear();
            Init();
        }

        // 5. 加载第一个事件，开始新的实验流程
        LoadFlowEvent(0);

        Debug.LogFormat("<color=green>【事件管理器】实验重置完成，已重新开始第一个事件</color>");
    }

    /// <summary>
    /// 重置事件管理器内部状态
    /// </summary>
    private void ResetEventManagerState()
    {
        Debug.LogFormat("<color=blue>【事件管理器】重置事件管理器状态</color>");

        // 重置事件触发状态
        for (int i = 0; i < isEventTrigger.Count; i++)
        {
            isEventTrigger[i] = false;
        }

        // 重置当前事件索引
        floweventIndex = 0;

        // 重置当前事件数据
        currentFlowEvent = null;

        // 重置组合组状态
        if (combineGroups != null)
        {
            foreach (var group in combineGroups)
            {
                if (group.eventsisDone != null)
                {
                    for (int i = 0; i < group.eventsisDone.Length; i++)
                    {
                        group.eventsisDone[i] = false;
                    }
                }
            }
        }

        Debug.LogFormat("<color=blue>【事件管理器】事件管理器状态重置完成</color>");
    }

    /// <summary>
    /// 重置UI和高亮状态
    /// </summary>
    private void ResetUIAndHighlightState()
    {
        Debug.LogFormat("<color=blue>【事件管理器】重置UI和高亮状态</color>");

        // 停止并重置UI动画
        if (uiService != null)
        {
            uiService.StopUIAnimationAndReset();
        }

        // 隐藏高亮
        if (highlightService != null)
        {
            highlightService.HideHighlight();
        }

        // 重置UI文本内容
        if (contentUI != null)
        {
            contentUI.text = "";
        }

        // 停止音频播放
        if (eventAudio != null)
        {
            eventAudio.Stop();
        }

        Debug.LogFormat("<color=blue>【事件管理器】UI和高亮状态重置完成</color>");
    }


    #region 辅助类

    /// <summary>
    /// UI动画服务类，负责UI动画的播放、异步等待、终止与数值动画。
    /// </summary>
    private class UIEventService
    {
        private Transform showUI;
        private Vector3 targetpos;
        private Vector3 originalpos;
        private bool isAnimating = false;
        private DG.Tweening.Sequence currentFlySequence;
        private TaskCompletionSource<bool> animationTcs;
        public event Action OnUIAnimationComplete;
        public UIEventService(Transform showUI, Vector3 targetpos, Vector3 originalpos)
        {
            this.showUI = showUI;
            this.targetpos = targetpos;
            this.originalpos = originalpos;
        }

        /// <summary>
        /// 播放UI动画。
        /// </summary>
        /// <param name="onComplete">动画完成回调</param>
        public void PlayUIAnimation(Action onComplete = null)
        {
            if (isAnimating)
            {
                // 如果动画正在进行，直接忽略新的动画请求
                return;
            }
            PlayUIAnimationInternal(onComplete);
        }

        /// <summary>
        /// 异步等待UI动画完成。
        /// </summary>
        public async Task PlayUIAnimationAsync()
        {
            if (isAnimating && animationTcs != null)
            {
                await animationTcs.Task;
                return;
            }
            animationTcs = new TaskCompletionSource<bool>();
            PlayUIAnimation(() => animationTcs.TrySetResult(true));
            await animationTcs.Task;
        }

        /// <summary>
        /// UI动画的内部实现，负责具体的动画流程。
        /// </summary>
        /// <param name="onComplete">动画完成回调</param>
        private void PlayUIAnimationInternal(Action onComplete)
        {
            isAnimating = true;
            currentFlySequence = DG.Tweening.DOTween.Sequence();
            currentFlySequence.Append(showUI.DOMove(targetpos, 2).SetEase(DG.Tweening.Ease.OutQuad));
            currentFlySequence.AppendInterval(2);
            currentFlySequence.Append(showUI.DOMove(originalpos, 2).SetEase(DG.Tweening.Ease.InQuad));
            currentFlySequence.OnComplete(() =>
            {
                isAnimating = false;
                onComplete?.Invoke();
                OnUIAnimationComplete?.Invoke();
                animationTcs?.TrySetResult(true);
            });
            currentFlySequence.Play();
        }

        /// <summary>
        /// 停止UI动画，不重置位置。
        /// </summary>
        public void StopUIAnimation()
        {
            if (currentFlySequence != null)
            {
                currentFlySequence.Kill();
                currentFlySequence = null;
            }
            isAnimating = false;
            animationTcs?.TrySetResult(true);
        }

        /// <summary>
        /// 停止UI动画并重置到起点。
        /// </summary>
        public void StopUIAnimationAndReset()
        {
            if (currentFlySequence != null)
            {
                currentFlySequence.Kill();
                currentFlySequence = null;
            }
            if (showUI != null)
                showUI.position = originalpos; // 立即回到起点
            isAnimating = false;
            animationTcs?.TrySetResult(true);
        }
    }

    /// <summary>
    /// Timeline服务类，负责Timeline的播放与回调。
    /// </summary>
    private class TimelineService
    {
        public event Action<FlowEventNodeData> OnTimelineComplete;

        /// <summary>
        /// 判断事件节点是否有Timeline。
        /// </summary>
        /// <param name="data">事件节点数据</param>
        /// <returns>是否有Timeline</returns>
        public bool HasTimeline(FlowEventNodeData data)
        {
            return data.timelineAssets != null && data.timelineAssets.Count > 0;
        }

        /// <summary>
        /// 播放Timeline并在完成后回调。
        /// </summary>
        /// <param name="data">事件节点数据</param>
        /// <param name="onComplete">完成回调</param>
        public void Play(FlowEventNodeData data, Action onComplete = null)
        {
            if (TimelineManager.instance && HasTimeline(data))
            {
                TimelineManager.instance.PlayTimelines(data.timelineAssets.ToArray(), () =>
                {
                    onComplete?.Invoke();
                    OnTimelineComplete?.Invoke(data);
                });
            }
            else
            {
                onComplete?.Invoke();
                OnTimelineComplete?.Invoke(data);
            }
        }
    }

    /// <summary>
    /// 高亮服务类，负责高亮物体的显示、动画、协程管理。
    /// </summary>
    private class HighlightService
    {
        
        private GameObject highlightCone;
        private Sequence floatSequence;
        private Coroutine highlightCoroutine;
        private GameObject lastHighlightTarget = null;
        private MonoBehaviour coroutineHost;

        public HighlightService(GameObject highlightCone)
        {
            this.highlightCone = highlightCone;
            if (highlightCone != null)
                highlightCone.SetActive(false); // 初始化时隐藏
        }

        public void SetCoroutineHost(MonoBehaviour host)
        {
            this.coroutineHost = host;
        }

        /// <summary>
        /// 显示高亮动画。
        /// </summary>
        /// <param name="target">需要高亮的目标物体</param>
        public void ShowHighlight(GameObject target)
        {
            if (target == null) return;
            if (lastHighlightTarget == target && highlightCoroutine != null)
            {
                // 已在高亮，无需重复
                return;
            }
            lastHighlightTarget = target;
            if (coroutineHost != null)
            {
                if (highlightCoroutine != null)
                {
                    coroutineHost.StopCoroutine(highlightCoroutine);
                }
                highlightCoroutine = coroutineHost.StartCoroutine(ShowHighlightCoroutine(target));
            }
        }

        /// <summary>
        /// 隐藏高亮动画。
        /// </summary>
        public void HideHighlight()
        {
            lastHighlightTarget = null;
            if (coroutineHost != null && highlightCoroutine != null)
            {
                coroutineHost.StopCoroutine(highlightCoroutine);
                highlightCoroutine = null;
            }
            if (highlightCone != null)
                highlightCone.SetActive(false);
            if (floatSequence != null)
                floatSequence.Kill();
        }

        /// <summary>
        /// 高亮动画的协程，负责高亮物体的浮动和自动隐藏。
        /// </summary>
        /// <param name="target">需要高亮的目标物体</param>
        /// <returns>协程IEnumerator</returns>
        private System.Collections.IEnumerator ShowHighlightCoroutine(GameObject target)
        {
            if (highlightCone == null) yield break;
            highlightCone.SetActive(true);
            float height = 0f;
            if (target.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                height = meshRenderer.bounds.max.y;
            }
            Vector3 targetPosition = new Vector3(
                target.transform.position.x,
                height + 0.5f,
                target.transform.position.z
            );
            highlightCone.transform.position = targetPosition;
            if (floatSequence != null)
            {
                floatSequence.Kill();
            }
            float floatHeight = 0.5f;
            float floatDuration = 1f;
            floatSequence = DG.Tweening.DOTween.Sequence();
            floatSequence.Append(highlightCone.transform.DOMoveY(targetPosition.y + floatHeight, floatDuration)
                .SetEase(DG.Tweening.Ease.InOutQuad));
            floatSequence.Append(highlightCone.transform.DOMoveY(targetPosition.y, floatDuration)
                .SetEase(DG.Tweening.Ease.InOutQuad));
            floatSequence.SetLoops(-1);
            floatSequence.Play();
            yield return new WaitForSeconds(4f); // 动画持续时间
            highlightCone.SetActive(false);      // 动画结束后隐藏
            if (floatSequence != null)
            {
                floatSequence.Kill();
            }
            highlightCoroutine = null;
        }
    }


    public class BaseEventService
    {
        public BaseEventService() { }

        /// <summary>
        /// 点击相关物体可以加载新的节点。
        /// </summary>
        /// <param name="gameobject">当前步骤需要点击的物体</param>
        /// <param name="eventNames">当前步骤下可选择的事件名称集合</param>
        /// <param name="eventUI">相关物体上的组件</param>
        /// <param name="objectName">当前步骤需要点击的物体名称</param>
        public void HoldForClickedEvent(GameObject gameobject, List<string> eventNames, EventUI eventUI, string objectName)
        {
            if (gameobject != null)
            {
                // 获取烧杯上的Clickable组件
                Clickable clickable = gameobject.GetComponent<Clickable>();
                if (clickable != null)
                {
                    // 设置烧杯为等待点击状态
                    clickable.SetWaitingForClick(true);

                    // 准备事件列表


                    // 更新UI内容（但此时UI还未显示）
                    if (eventUI != null)
                    {
                        eventUI.Setup(eventNames, objectName);
                    }
                }
            }
        }



        /// <summary>
        /// UI数值从一个数值快速变换到目标数值
        /// </summary>
        /// <param name="showObject">需要显示UI的物体</param>
        /// <param name="startValue">起始数值</param>
        /// <param name="endValue">目标数值</param>
        /// <param name="format">格式化参数</param>
        public void AnimateTextValue(GameObject showObject, float startValue, float endValue, string format = "F0")
        {
            TextMeshPro text = showObject.GetComponentInChildren<TextMeshPro>();

            if (text == null) return;

            // 设置初始值
            text.text = startValue.ToString(format);

            float dur = (endValue - startValue) / 250;
            // 创建DOTween序列
            DOTween.To(() => startValue, x => {
                text.text = x.ToString(format);
            }, endValue, dur)
            .SetEase(Ease.OutQuad)
            .Play(); // 使用OutQuad缓动效果，使动画更自然
        }

        /// <summary>
        /// UI数值从一个数值快速变换到目标数值
        /// </summary>
        /// <param name="showObject">需要显示UI的物体</param>
        /// <param name="des">显示内容文本</param>
        public void ShowRecord(GameObject showObject, string des)//显示记录的内容以及其他东西
        {
            TextMeshPro text = showObject.GetComponentInChildren<TextMeshPro>();
            text.text = "";

            // 设置完整文本
            string fullText = des;

            // 创建打字机效果
            DOTween.To(() => 0, (int x) => {
                text.text = fullText.Substring(0, x);
            }, fullText.Length, 0.1f * fullText.Length)
            .SetDelay(0.1f)
            .SetEase(Ease.Linear)
            .Play();
        }

    }

    #endregion
}
