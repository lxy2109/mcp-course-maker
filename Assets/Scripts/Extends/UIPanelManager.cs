using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using TMPro;

public class UIPanelManager :SerializedMonoBehaviour
{
    [BoxGroup("设置")]
    public List<ButtonState> buttons = new List<ButtonState>();

    [BoxGroup("UI设置")]
    [LabelText("引导文本")]
    public GameObject tipPannel;
    [BoxGroup("UI设置")]
    [LabelText("患者信息文本")]
    public GameObject infoPannel;
    [BoxGroup("UI设置")]
    [LabelText("操作用具文本")]
    public GameObject itemPannel;
    [BoxGroup("UI设置")]
    [LabelText("操作记录文本")]
    public GameObject logPannel;
    [BoxGroup("UI设置")]
    [LabelText("操作帮助文本")]
    public GameObject helpPannel;

    [BoxGroup("UI设置")]
    [LabelText("患者信息Text")]
    public TextMeshProUGUI infoText;
    //[BoxGroup("UI设置")]
    //[LabelText("操作记录Text")]
    //public TextMeshProUGUI logText;
    [BoxGroup("UI设置")]
    [LabelText("提示Text")]
    public TextMeshProUGUI tipsText;

    [BoxGroup("操作记录设置")]
    public List<ActionInfo> actionRecords = new List<ActionInfo>();
    [BoxGroup("操作记录设置")]
    public Transform recordRoot;
    [BoxGroup("操作记录设置")]
    public GameObject actionRecordPrefab;


    /// <summary>
    /// 更新提示信息Text
    /// </summary>
    /// <param name="text"></param>
    [Button]
    public void UpdateTipsText(string text)
    {
        tipsText.text = text;
    }
    /// <summary>
    /// 更新患者信息Text
    /// </summary>
    /// <param name="text"></param>
    [Button]
    public void UpdateInfoText(string text)
    {
        infoText.text = text;
    }
    /// <summary>
    /// 更新操作记录Text
    /// </summary>
    /// <param name="text"></param>
    [Button]
    public void UpdateLog(ActionInfo info)
    {
      if (!actionRecords.Contains(info))
        {
            actionRecords.Add(info);

            GameObject temp = GameObject.Instantiate(actionRecordPrefab, recordRoot);
            temp.GetComponent<ActionRecordInfo>().SetUp(info);
        }
    }

    #region Pannel
    public void OpenTipPannelActive()
    {
        tipPannel.SetActive(true);
    }
    public void CloseTipPannelActive()
    {
        tipPannel.SetActive(false);
    }

    public void OpenPannel(int index)
    {
        if (index >= 4) return;
        if (index == 0) { infoPannel.SetActive(true); itemPannel.SetActive(false); logPannel.SetActive(false); helpPannel.SetActive(false); }
        if (index == 1) { infoPannel.SetActive(false); itemPannel.SetActive(true); logPannel.SetActive(false); helpPannel.SetActive(false); }
        if (index == 2) { infoPannel.SetActive(false); itemPannel.SetActive(false); logPannel.SetActive(true); helpPannel.SetActive(false); }
        if (index == 3) { infoPannel.SetActive(false); itemPannel.SetActive(false); logPannel.SetActive(false); helpPannel.SetActive(true); }


        for (int i = 1; i < buttons.Count; i++)
        {
            if (i == index+1)
            {
                continue;
            }
            else
            {
                ActiveButton(buttons[i]);
            }
        }

    }
    public void ClosePannel(int index)
    {
        if (index >= 4) return;
        if (index == 0) infoPannel.SetActive(false);
        if (index == 1) itemPannel.SetActive(false);
        if (index == 2) logPannel.SetActive(false);
        if (index == 3) helpPannel.SetActive(false);
    }
    #endregion

    #region Button
    public void DefreezeOtherButton(ButtonState besideButton)
    {
        foreach (ButtonState button in buttons)
        {
            if (button != besideButton)
            {
                if (button.state != ButtonState.ButtonStateType.Disabled)
                {
                    button.SwitchState(ButtonState.ButtonStateType.Normal);
                }
            }
        }
    }

    public void ActiveButton(ButtonState button)
    {
        button.SwitchState(ButtonState.ButtonStateType.Normal);
    }

    public void ActiveButton(ButtonState[] buttons)
    {
        foreach (ButtonState button in buttons)
            button.SwitchState(ButtonState.ButtonStateType.Normal);
    }

    public void DisActiveButton(ButtonState button)
    {
        button.SwitchState(ButtonState.ButtonStateType.Disabled);
    }

    public void DisActiveButton(ButtonState[] buttons)
    {
        foreach (ButtonState button in buttons)
            button.SwitchState(ButtonState.ButtonStateType.Disabled);
    }
    #endregion

}
