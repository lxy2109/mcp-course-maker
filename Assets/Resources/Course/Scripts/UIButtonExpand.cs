using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
public class UIButtonExpand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(new Vector3(.8f,.8f,.8f), 0.8f)
    .SetEase(Ease.OutBack);  // 弹性效果
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(Vector3.one, 0.8f)
    .SetEase(Ease.OutBack);  // 弹性效果
    }
}
