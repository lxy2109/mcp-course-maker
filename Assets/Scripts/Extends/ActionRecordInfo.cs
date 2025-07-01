using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System;


[Serializable]

public struct ActionInfo
{
    [BoxGroup("Data")]
    public string userName;
    [BoxGroup("Data")]
    public string actionName;
    [BoxGroup("Data")]
    public bool isDone;
}

public class ActionRecordInfo : MonoBehaviour
{
    [SerializeField]
    [BoxGroup("Data")]
    public ActionInfo actionInfo;

    [BoxGroup("UI")]
    public TextMeshProUGUI userText;
    [BoxGroup("UI")]
    public TextMeshProUGUI actionText;
    [BoxGroup("UI")]
    public Image isDoneImage;



    public void SetUp(ActionInfo info)
    {
        actionInfo= info;
        UpdateUserName(info.userName);
        UpdateActionName(info.actionName);
        UpdateIsDone(info.isDone);

    }

    public void UpdateUserName(string userName)
    {
        actionInfo.userName = userName;
        userText.text = actionInfo.userName;
    }
    public void UpdateActionName(string actionName)
    {
        actionInfo.actionName = actionName;
        actionText.text = actionInfo.actionName;
    }
    [Button]
    public void UpdateIsDone(bool value)
    {
        actionInfo.isDone = value;
        isDoneImage.color = actionInfo.isDone? Color.green : Color.white;
    }

}
