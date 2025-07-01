using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class MainUIController : MonoBehaviour
{
    public List<GameObject> panels;
    public List<bool> buttonSelect;
    public List<GameObject> mainButtons;//需要显示的按钮
    public List<GameObject> supButtons;
    public List<GameObject> patientInfos;

    public void SetPanelState(int i)
    {
        for (int j = 0; j< buttonSelect.Count; j++)
        {
            if (j == i)
            {
                buttonSelect[j] = !buttonSelect[j];
            }
            else 
            {
                buttonSelect[j] = false;
            }
            panels[j].SetActive(buttonSelect[j]);
        }

    }

    public void OnMainButtonClick(int i)
    {
        SetPanelState(i);
        if (buttonSelect[i])//如果选中了i，则i之后的main按钮关闭，sup按钮打开
        {
            for (int j = 0; j < mainButtons.Count; j++)
            {
                if (j == i)
                {
                    mainButtons[j].SetActive(false);
                    supButtons[j].SetActive(true);
                }
                else
                {
                    mainButtons[j].SetActive(true);
                    supButtons[j].SetActive(false);
                }
            }
        }
        else
        {
            for (int j = 0; j < mainButtons.Count; j++)
            {
                mainButtons[j].SetActive(true);
                supButtons[j].SetActive(false);
            }
        }
    }

    public void ShowPatientInfo(int i)
    {
        for (int j = 0; j < patientInfos.Count; j++)
        {
            if (j == i)
            {
                patientInfos[j].SetActive(true);
            }
            else
            {
                patientInfos[j].SetActive(false);
            }
        }
    }

    public void SetDefaltePatientInfo(int i)
    {
        for (int j = 0; j < patientInfos.Count; j++)
        {
            if (j == i)
            {
                patientInfos[j].GetComponent<TMP_Text>().color = Color.yellow;
            }
            else
            {
                patientInfos[j].GetComponent<TMP_Text>().color = Color.white;
            }
        }
    }    
}
