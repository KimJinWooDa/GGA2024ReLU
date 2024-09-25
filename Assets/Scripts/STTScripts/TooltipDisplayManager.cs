using System;
using System.Collections;
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
        
        tooltipDisplay.gameObject.SetActive(true);
    }
    private void HideTooltip()
    {
        tooltipDisplay.gameObject.SetActive(false);
    }

    
}
