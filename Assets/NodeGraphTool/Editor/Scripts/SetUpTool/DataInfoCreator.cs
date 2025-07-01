/*
 * DataInfoCreator.cs
 * 
 * 功能概述：
 * 本工具是一个Unity编辑器扩展，用于从Excel文件中提取教学步骤数据并生成相应的语音资源。
 * 主要功能包括：
 * 1. 从Excel表格读取结构化教学数据（操作引导、引导文本、手部提示等）
 * 2. 通过TTSUtillity将文本内容转换为语音文件
 * 3. 为NodeGraph节点系统提供数据源，支持可视化教学流程的构建
 * 
 * 工作流程：
 * 1. 选择包含教学步骤数据的Excel文件
 * 2. 点击"提取步骤数据"按钮从Excel读取数据
 * 3. 调用CreatVoice方法生成语音文件
 * 4. 在NodeGraphWindow中使用这些数据创建可视化节点图
 * 
 * 此类与NodeGraphWindow配合使用，是节点图教学系统的数据处理层
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Unity.EditorCoroutines.Editor;
using System;
using UnityEngine.Serialization;

/// <summary>
/// 教学流程节点的数据模型类，用于存储从Excel中提取的每行数据
/// </summary>
[System.Serializable]
public class FlowNodeTempAsset
{
    [LabelText("flowGraph 实验步骤(FlowGraph)实验逻辑部分")] 
    public string flowGraph;
    [FormerlySerializedAs("tipsName")] [LabelText("eventName 实验动作(EventName)具体动作事件")]
    public string eventName;
    [FormerlySerializedAs("tipContent")] [LabelText("引导文本(阶段文案 UI引导提示)")]
    public string eventContent;
    [LabelText("手部提示(需要点击的物品名称)")]
    public string handTip;
    [LabelText("虚拟交互Enter(进入事件时进行的Event名称)")]
    public string enterEventName;
    [FormerlySerializedAs("enterEventCon")] [LabelText("交互内容Enter(进入事件时进行的Event内容)")]
    public string enterEventContent;
    [LabelText("虚拟交互Exit(离开事件时进行的Event名称)")]
    public string exitEventName;
    [FormerlySerializedAs("exitEventCon")] [LabelText("交互内容Exit(离开事件时进行的Event名内容)")]
    public string exitEventContent;
    [LabelText("配音脚本名称(进入事件时播放的音频名称)")]
    public string voiceName;
    [LabelText("配音脚本内容(进入事件时播放的音频文案内容)")]
    public string voiceContent;
    [LabelText("镜头timeline名称")]
    public string cameraTimelineName;
    [LabelText("镜头timeline内容")]
    public string cameraTimelineContent;
    [LabelText("物体timeline名称")]
    public string objectTimelineName;
    [LabelText("物体timeline内容")]
    public string objectTimelineContent;
    
    

    /// <summary>
    /// 构造函数，创建一个完整的流程节点资产
    /// </summary>
    /// <param name="flowGraph">实验步骤(FlowGraph)实验逻辑部分</param>
    /// <param name="eventName">实验动作(EventName)具体动作事件</param>
    /// <param name="eventContent">引导文本(阶段文案 UI引导提示)</param>
    /// <param name="handTip">手部提示(需要点击的物品名称)</param>
    /// <param name="enterEventName">虚拟交互Enter(进入事件时进行的Event名称)</param>
    /// <param name="enterEventContent">交互内容Enter(进入事件时进行的Event内容)</param>
    /// <param name="exitEventName">虚拟交互Exit(离开事件时进行的Event名称)</param>
    /// <param name="exitEventContent">交互内容Exit(离开事件时进行的Event名内容)</param>
    /// <param name="voiceName">配音脚本名称(进入事件时播放的音频名称)</param>
    /// <param name="voiceContent">配音脚本内容(进入事件时播放的音频文案内容)</param>
    /// <param name="cameraTimelineName">镜头timeline名称</param>
    /// <param name="cameraTimelineContent">镜头timeline内容</param>
    /// <param name="objectTimelineName">物体timeline名称</param>
    /// <param name="objectTimelineContent">物体timeline内容</param>
    public FlowNodeTempAsset (
        string flowGraph,
        string eventName,
        string eventContent,
        string handTip,
        string enterEventName,
        string enterEventContent,
        string exitEventName,
        string exitEventContent,
        string voiceName,
        string voiceContent,
        string cameraTimelineName,
        string cameraTimelineContent,
        string objectTimelineName,
        string objectTimelineContent
    )
    {
        this.flowGraph = flowGraph;
        this.eventName = eventName;
        this.eventContent = eventContent;
        this.handTip = handTip;
        this.enterEventName = enterEventName;
        this.enterEventContent = enterEventContent;
        this.exitEventName = exitEventName;
        this.exitEventContent = exitEventContent;
        this.voiceName = voiceName;
        this.voiceContent = voiceContent;
        this.cameraTimelineName = cameraTimelineName;
        this.cameraTimelineContent = cameraTimelineContent;
        this.objectTimelineName = objectTimelineName;
        this.objectTimelineContent = objectTimelineContent;
    }
}

/// <summary>
/// Excel数据创建工具，提供从Excel提取数据并生成语音资源的功能
/// </summary>
public class DataInfoCreator : OdinEditorWindow
{
    /// <summary>
    /// 创建并显示数据信息创建器窗口
    /// </summary>
    /// <param name="windowTitle">窗口标题</param>
    /// <returns>创建的窗口实例</returns>
    public static DataInfoCreator ShowWindow(string windowTitle)
    {
        DataInfoCreator dataInfo = GetWindow<DataInfoCreator>();
        dataInfo.titleContent = new GUIContent(windowTitle);
        dataInfo.Show();
        return dataInfo;
    }
    
    /// <summary>
    /// Excel文件路径，使用Odin的FilePath特性提供文件选择器
    /// </summary>
    [Sirenix.OdinInspector.FilePath]
    [LabelText("Excel文件")]
    public string assetFilePath;
    
    /// <summary>
    /// 从Excel提取的步骤数据列表
    /// </summary>
    [SerializeField]
    [LabelText("步骤")]
    public List<FlowNodeTempAsset> datas = new List<FlowNodeTempAsset>();

    /// <summary>
    /// 测试文本转语音功能
    /// </summary>
    /// <param name="content">要转换为语音的文本内容</param>
    [Button("测试文本转语音功能")]
    public void TestCreatVoice(string content)
    {
        // 启动编辑器协程，执行TTS转换
        EditorCoroutineUtility.StartCoroutine(TTSUtillity.TTS(content), this);
        // 刷新资源数据库，使新生成的语音文件可见
        UnityEditor.AssetDatabase.Refresh();
    }

    /// <summary>
    /// 为所有步骤数据创建语音文件，完成后执行回调
    /// </summary>
    /// <param name="action">语音创建完成后的回调函数</param>
    public void CreatVoice(Action action)
    {
        // 启动编辑器协程，为所有数据生成语音
        // EditorCoroutineUtility.StartCoroutine(TTSUtillity.TTS(datas, action), this);
    }

    /// <summary>
    /// 为指定内容创建语音文件并保存到指定文件夹
    /// </summary>
    /// <param name="content">语音内容文本</param>
    /// <param name="fileName">语音文件名</param>
    /// <param name="fold">存储文件夹</param>
    public void CreatVoice(string content, string fileName, string fold)
    {
        // 启动编辑器协程，为指定内容生成语音
        EditorCoroutineUtility.StartCoroutine(TTSUtillity.TTS(content, fileName, fold), this);
    }

    /// <summary>
    /// 从Excel文件中提取步骤数据，生成FlowNodeTempAsset对象列表
    /// </summary>
    [Button("提取步骤数据")]
    private void GetAssets()
    {
        // 检查文件路径是否为空
        if (assetFilePath.IsNullOrWhitespace()) return;

        // 清空现有数据
        datas.Clear();

        // 从Excel的不同列读取数据
        List<string> flowGraphNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 1, 0, ref flowGraphNames, 1);
        
        List<string> eventNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 2, 0, ref eventNames, 1);
        
        List<string> eventContents = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 3, 0, ref eventContents, 1);
        
        List<string> handTips = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 4, 0, ref handTips, 1);
        
        List<string> enterEventNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 5, 0, ref enterEventNames, 1);
        
        List<string> enterEventContents= new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 6, 0, ref enterEventContents, 1);
        
        List<string> exitEventNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 7, 0, ref exitEventNames, 1);
        
        List<string> exitEventContents = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 8, 0, ref exitEventContents, 1);
        
        List<string> voiceNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 9, 0, ref voiceNames, 1);
        
        List<string> voiceContents = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 10, 0, ref voiceContents, 1);
        
        List<string> cameraTimelineNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 11, 0, ref cameraTimelineNames, 1);
        
        List<string> cameraTimelineContents = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 12, 0, ref cameraTimelineContents, 1);
        
        List<string> objectTimelineNames = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 13, 0, ref objectTimelineNames, 1);
        
        List<string> objectTimelineContents = new List<string>();
        ExcelUtility.GetExcelRowData(assetFilePath, 14, 0, ref objectTimelineContents, 1);

        // 将读取的各列数据组合成FlowNodeTempAsset对象
        for (int i = 0; i < eventNames.Count; i++)
        {
            datas.Add(new FlowNodeTempAsset(
                flowGraphNames[i],
                eventNames[i],
                eventContents[i],
                handTips[i],
                enterEventNames[i],
                enterEventContents[i],
                exitEventNames[i],
                exitEventContents[i],
                voiceNames[i],
                voiceContents[i],
                cameraTimelineNames[i],
                cameraTimelineContents[i],
                objectTimelineNames[i],
                objectTimelineContents[i]
            ));
            
                
        }
    }
    
}