using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventUI : MonoBehaviour
{
    [Tooltip("UI与物体在屏幕上的偏移")]
    public Vector2 screenOffset = new Vector2(0, 0);
    
    private Camera mainCamera;
    private RectTransform panelRect;

    [SerializeField]
    private Text clickObjectName; // 当前显示物体的名称

    [SerializeField]
    private Transform content;
    public List<Text> currenteventname = new List<Text>(); // 当前选中物体的所有事件名称
    public List<Button> currenteventbotton = new List<Button>();
    [SerializeField, Tooltip("按钮组件引用，如果不指定则尝试从当前对象获取")]

    private void Awake()
    {
        mainCamera = Camera.main;
        clickObjectName = transform.GetChild(0).GetComponentInChildren<Text>();
        panelRect = transform.GetComponent<RectTransform>();
        
        // 初始化时隐藏UI
       // gameObject.SetActive(false);
        
        // 获取所有事件按钮
        foreach (Transform child in content)
        {
            if (child == transform) continue;

            Text component = child.GetComponentInChildren<Text>();
            Button button = child.GetComponent<Button>();
            if (component != null)
            {
                currenteventname.Add(component);
                currenteventbotton.Add(button);
            }
        }
    }


    public void UpdateUIPosition(GameObject targetObject)
    {
        // 原代码注释开始
        /*
        if (targetObject == null) return;

        // 获取物体在屏幕上的位置
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetObject.transform.position);

        // 检查物体是否在摄像机前方
        if (screenPos.z < 0)
        {
            gameObject.SetActive(false);
            return;
        }

        // 将屏幕坐标转换为Canvas坐标
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            screenPos,
            null,
            out canvasPos
        );

        // 应用偏移
        canvasPos += screenOffset;

        // 设置UI位置
        panelRect.anchoredPosition = canvasPos;
        gameObject.SetActive(true);
        */
        // 原代码注释结束
        
        // 新代码：将UI固定显示在屏幕中间偏下位置
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 50); // 距离屏幕底部50像素
        gameObject.SetActive(true);
    }

    public void Setup(List<string> eventNames, string objectName)
    {
        // 更新物体名称
        clickObjectName.text = objectName;

        // 隐藏所有按钮
        foreach (var button in currenteventbotton)
        {
            button.gameObject.SetActive(false);
        }

        // 显示并更新事件按钮
        int size = Mathf.Min(eventNames.Count, currenteventbotton.Count);
        for (int i = 0; i < size; i++)
        {
            currenteventbotton[i].gameObject.SetActive(true);
            currenteventname[i].text = eventNames[i];
            
            // 先移除所有旧的监听器
            currenteventbotton[i].onClick.RemoveAllListeners();
            
            // 添加新的监听器
            string eventName = eventNames[i]; // 创建局部变量来捕获当前的事件名称
            currenteventbotton[i].onClick.AddListener(() => ExecuteEvent(eventName));
        }
    }//当你点击的时候更新下面的事件条，并更新内容信息。


    /// <summary>
    /// 执行指定名称的事件
    /// </summary>
    public void ExecuteEvent(string eventNameToLoad)
    {
        if (string.IsNullOrEmpty(eventNameToLoad))
        {
            Debug.LogError("【按钮测试】未指定事件名称，请在Inspector中设置eventNameToLoad");
            return;
        }

        if (EventManager.instance == null)
        {
            Debug.LogError("【按钮测试】EventManager实例不存在");
            return;
        }

        Debug.LogFormat("<color=yellow>【按钮测试】执行事件: {0}</color>", eventNameToLoad);
        EventManager.instance.LoadFlowEvent(eventNameToLoad);
        for(int i = 0;i< 5;i++)
        {
            if (IsButtonListenerAdded(currenteventbotton[i]))
                currenteventbotton[i].onClick.RemoveAllListeners();
        }
        gameObject.SetActive(false);
    }//执行跳转的button事件



    public bool IsButtonListenerAdded(Button button)//检测当前按钮是否添加了监听代码
    {
        // 获取按钮上的所有监听器
        var listeners = button.onClick.GetPersistentEventCount();
        Debug.Log($"【按钮测试】当前按钮有 {listeners} 个监听器");

        // 检查是否包含ExecuteEvent方法
        bool hasExecuteEvent = false;
        for (int i = 0; i < listeners; i++)
        {
            var target = button.onClick.GetPersistentTarget(i);
            var methodName = button.onClick.GetPersistentMethodName(i);
            if (target == this && methodName == "ExecuteEvent")
            {
                hasExecuteEvent = true;
                break;
            }
        }

        return hasExecuteEvent;
    }
}
