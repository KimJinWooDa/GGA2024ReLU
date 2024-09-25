using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipRequester : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action<string, Sprite, PointerEventData> OnTooltipRequest;
    public Action OnTooltipRequestEnd;
    
    [TextArea]
    public string tooltipText;
    public Sprite tooltipSprite;
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnTooltipRequest?.Invoke(tooltipText, tooltipSprite, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnTooltipRequestEnd?.Invoke();
    }
}
