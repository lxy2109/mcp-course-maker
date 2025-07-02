using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Xml.Serialization;

public class CanvasButtonListener : MonoBehaviour
{
    private EventManager eventManager;
    private Button[] button = new Button[5];
    private Text currentHighlightText;
    private Text currentOutlineText;

    private void Awake()
    {
        button[0] = transform.GetChild(0).GetChild(0).GetComponent<Button>();
        button[1] = transform.GetChild(0).GetChild(1).GetComponent<Button>();
        button[2] = transform.GetChild(0).GetChild(2).GetComponent<Button>();
        button[3] = transform.GetChild(0).GetChild(4).GetComponent<Button>();
        button[4] = transform.GetChild(1).GetComponentInChildren<Button>();
        currentHighlightText = transform.GetChild(0).GetChild(3).GetComponentInChildren<Text>();
        currentOutlineText = transform.GetChild(0).GetChild(4).GetComponentInChildren<Text>();
       
    }

    void Start()
    {
        eventManager = FindObjectOfType<EventManager>();
        if (eventManager == null)
        {
            Debug.LogError("EventManager not found in scene!");
            return;
        }

        if (button == null)
        {
            Debug.LogError("No Button found in children!");
            return;
        }

        button[0].onClick.AddListener(CameraReSet);
        button[1].onClick.AddListener(ReStartExp);
        button[2].onClick.AddListener(SwitchHighlightType);
        button[3].onClick.AddListener(SwitchOutlineType);
        button[4].onClick.AddListener(StartExp);
    }

    /// <summary>
    /// 按钮点击事件处理,自身panel消失，且自动加载当前nodegraph的第一个node
    /// </summary>
    private void StartExp()
    {
        transform.GetChild(1).gameObject.SetActive(false);
        if (eventManager != null)
        {
            eventManager.LoadFlowEvent(0);
        }
    }

    private void CameraReSet()
    {
        eventManager.ResetCameraPos();
    }

    private void ReStartExp()
    {
        eventManager.ResetAndRestartExperiment();
    }


    private void SwitchHighlightType()
    {
        eventManager.highlightService.NextHighlightType();
        currentHighlightText.text = eventManager.highlightService.GetHighlightType() switch
        {
            HighlightType.Prompt => "标识符",
            HighlightType.OutLine => "外缘线",
            HighlightType.Both => "共用",
            _ => "标识符"
        };
        var outpanel = button[3].transform.gameObject;
        if(currentHighlightText.text == "标识符")
        {
            outpanel.SetActive(false);
        }
        else
        {
            outpanel.SetActive(true);
        }
    }

    private void SwitchOutlineType()
    {
        eventManager.highlightService.NextOutlineType();
        currentOutlineText.text = eventManager.highlightService.GetOutlineType() switch
        {
            OutlineType.None => "无特效",
            OutlineType.WidthBreathing => "呼吸灯",
            OutlineType.ColorBlinking => "闪烁",
            _ => "无特效"
        };
    }
}
