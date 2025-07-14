using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sirenix.OdinInspector;

/// <summary>
/// 根据文本内容动态调整Panel宽度，Panel高度固定，Text适应Panel的工具
/// </summary>
public class DynamicTextResizer : MonoBehaviour
{
    [SerializeField] private Text targetText;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float fixedHeight = 170f;

    private void Awake()
    {
        targetText = GetComponentInChildren<Text>();
        panelRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (targetText == null || panelRect == null) return;

        float preferredWidth = targetText.preferredWidth;
        float targetWidth = GetTextTargetWidth(preferredWidth);

        // 判断是否需要两行
        if (preferredWidth > 1100f)
        {
            targetText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
        else
        {
            targetText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        Debug.Log(preferredWidth);
        // 设置Text和Panel宽度
        var textRect = targetText.GetComponent<RectTransform>();
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth+50f);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);

        textRect.anchorMin = new Vector2(0, textRect.anchorMin.y);
        textRect.anchorMax = new Vector2(0, textRect.anchorMax.y);
        textRect.pivot = new Vector2(0, textRect.pivot.y);

        panelRect.anchorMin = new Vector2(0, panelRect.anchorMin.y);
        panelRect.anchorMax = new Vector2(0, panelRect.anchorMax.y);
        panelRect.pivot = new Vector2(0, panelRect.pivot.y);

        // 设置PosX
        textRect.anchoredPosition = new Vector2(25f, textRect.anchoredPosition.y);
    }

    float GetTextTargetWidth(float preferredWidth)
    {
        if (preferredWidth <= 1100f)
        {
            return preferredWidth;
        }
        else if (preferredWidth <= 1540f)
        {
            return 1000f;
        }
        else
        {
            float w = preferredWidth / 2f+40f;
            return Mathf.Clamp(w, 770f, 1500f);
        }
    }
} 