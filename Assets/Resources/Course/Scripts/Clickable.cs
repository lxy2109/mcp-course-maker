using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class Clickable : MonoBehaviour
{
    [Tooltip("需要显示的UI预制体")]
    public GameObject uiPrefab;

    private bool isUIOpen = false;
    private EventUI eventUI;
    private bool isWaitingForClick = false;

    private void Start()
    {
        // 获取EventUI组件
        eventUI = uiPrefab.GetComponent<EventUI>();
        if (eventUI == null)
        {
            Debug.LogError("UI预制体上缺少EventUI组件！");
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (isWaitingForClick)
        {
            if (!isUIOpen)
            {
                // 显示UI并更新位置
                ShowUI();
                isWaitingForClick = false;
            }
            else
            {
                // 关闭UI
                HideUI();
            }
        }
    }

    private void ShowUI()
    {
        if (uiPrefab != null)
        {
            uiPrefab.SetActive(true);
            // 更新UI位置
            eventUI.UpdateUIPosition(gameObject);
            isUIOpen = true;
        }
    }

    private void HideUI()
    {
        if (uiPrefab != null)
        {
            uiPrefab.SetActive(false);
            isUIOpen = false;
        }
    }

    public void SetWaitingForClick(bool waiting)
    {
        isWaitingForClick = waiting;
    }
}

