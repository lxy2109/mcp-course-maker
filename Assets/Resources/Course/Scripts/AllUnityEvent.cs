using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[System.Serializable]
public struct FlowEventNodeD {
    public string flowgraph;//每个flowgraph的名称
    public string currentClickObj;//前一步要点击的一个对象
    public string enterEvent;//进入事件
    public string enterDes;//进入事件
    public string exitEvent;//退出事件
    public string exitDes;//退出事件
}


[AddComponentMenu("Course/AllUnityEvent")]
public class AllUnityEvent : SceneSingleton<AllUnityEvent>
{

    [TabGroup("ExcelData")]
    public Dictionary<string,FlowEventNodeD>  flownodeDic  = new Dictionary<string , FlowEventNodeD>();
    [TabGroup("ExcelData")]
    public Dictionary<string,string> objectpartobj = new Dictionary<string,string>();
    [TabGroup("ExcelData")]
    public Dictionary<string, List<string>> HoldDict = new Dictionary<string, List<string>>();
    [TabGroup("ExcelData")]
    public Dictionary<string, List<string>> HoldForCombineDict = new Dictionary<string, List<string>>();


    [TabGroup("ExcelData")]
    [Button("初始化")]
    public void InitializeDictionaries()
    {
        // 清空字典
        flownodeDic.Clear();
        objectpartobj.Clear();
        HoldDict.Clear();
        HoldForCombineDict.Clear();

        // 初始化 flownodeDic
        flownodeDic = new Dictionary<string, FlowEventNodeD>();

        // 初始化 objectpartobj
        objectpartobj = new Dictionary<string, string>();
        HoldDict = new Dictionary<string, List<string>>();
        HoldForCombineDict = new Dictionary<string, List<string>>();
    }

}
