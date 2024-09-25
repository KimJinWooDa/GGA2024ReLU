using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipDisplayManager : MonoBehaviour
{
    [SerializeField] private TooltipDisplay tooltipDisplay;
    [SerializeField] private List<TooltipRequester> tooltipRequesters = new List<TooltipRequester>();

    private void Start()
    {
        for(int i = 0; i < tooltipRequesters.Count; i++)
        {
            tooltipRequesters[i].OnTooltipRequest -= DisplayTooltip;
            tooltipRequesters[i].OnTooltipRequest += DisplayTooltip;
            
            tooltipRequesters[i].OnTooltipRequestEnd -= HideTooltip;
            tooltipRequesters[i].OnTooltipRequestEnd += HideTooltip;
        }
        
        tooltipDisplay.gameObject.SetActive(false);
    }

    private void DisplayTooltip(string inDescription, Sprite inIcon, PointerEventData inPointerEventData)
    {
        if (!string.IsNullOrEmpty(inDescription))
        {
            tooltipDisplay.TooltipText.text = inDescription;
            tooltipDisplay.TooltipText.gameObject.SetActive(true);
        }
        else
        {
            tooltipDisplay.TooltipText.gameObject.SetActive(false);
        }
        
        if (inIcon != null)
        {
            tooltipDisplay.TooltipImage.sprite = inIcon;
            tooltipDisplay.TooltipImage.gameObject.SetActive(true);
        }
        else
        {
            tooltipDisplay.TooltipImage.gameObject.SetActive(false);
        }
        
        RectTransform rectTransform = tooltipDisplay.TooltipRect;
        Vector2 tooltipSize = rectTransform.sizeDelta;

        Vector3 adjustedPosition = new Vector3(
            inPointerEventData.position.x + tooltipSize.x * rectTransform.pivot.x,
            inPointerEventData.position.y - tooltipSize.y * (1 - rectTransform.pivot.y), 
            0
        );

        rectTransform.position = adjustedPosition;
        
        tooltipDisplay.gameObject.SetActive(true);
    }
    private void HideTooltip()
    {
        tooltipDisplay.gameObject.SetActive(false);
    }

    
}
