using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LogManager : MonoBehaviour
{
    public static LogManager instance;
    public UIPanelManager uiManager;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }


    public void LogInfo(string actionName)//记录一个string到UnityEvent里，这样只会显示在CMD窗口
    {
        string[] logstr = actionName.Split('|');
        ActionInfo info= new ActionInfo();
        info.userName = logstr[1];//获取UserName字段
        info.actionName = logstr[0];
        info.isDone = true;//设为true
        uiManager.UpdateLog(info);
    }
}
