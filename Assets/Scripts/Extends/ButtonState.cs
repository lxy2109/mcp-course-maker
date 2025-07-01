using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Sirenix.OdinInspector;


[RequireComponent(typeof(Button))]
public class ButtonState : MonoBehaviour
{
    public enum ButtonStateType { 
    Normal,
    Pressed,
    Disabled    
    }
    public ButtonStateType state;
    
    //0=disabled 1=normal 2=highlight 3=pressed
    public GameObject[] groups;

    [Header("这部分方法会直接写入Switch，button设置一个Switch方法就行")]
    [LabelText("激活时执行方法")]
    public UnityEvent trueEvent;
    [LabelText("取消时执行方法")]
    public UnityEvent falseEvent;


    private Button button;




    private void Start()
    {
        button = GetComponent<Button>();
        SwitchState(state);
        if (state == ButtonStateType.Normal)
        {
            falseEvent?.Invoke();
            return;
        }
        if (state == ButtonStateType.Pressed)
        {
            trueEvent?.Invoke();
            return;
        }
    }

    [Button]
    public void SwitchState(ButtonStateType stateType)
    {
        state=stateType;
        switch (stateType)
        {
            case ButtonStateType.Normal:
                groups[0].SetActive(false);
                groups[1].SetActive(true);
                groups[2].SetActive(true);
                groups[3].SetActive(false);
                button.interactable= true;
                break;
            case ButtonStateType.Pressed:
                groups[0].SetActive(false);
                groups[1].SetActive(false);
                groups[2].SetActive(false);
                groups[3].SetActive(true);
                button.interactable = true;
                break;
            case ButtonStateType.Disabled:
                groups[0].SetActive(true);
                groups[1].SetActive(false);
                groups[2].SetActive(false);
                groups[3].SetActive(false);
                button.interactable = false;
                break;
            default:
                break;
        }
    }

    public void Switch()
    {
        if (state == ButtonStateType.Normal)
        {
            SwitchState(ButtonStateType.Pressed);
            Debug.Log("Active");
            trueEvent?.Invoke();
            return;
        }
        if (state == ButtonStateType.Pressed)
        {
            SwitchState(ButtonStateType.Normal);
            Debug.Log("DisActive");
            falseEvent?.Invoke();
            return;
        }
    }
}
