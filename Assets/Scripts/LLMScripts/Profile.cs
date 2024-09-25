using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Profile : MonoBehaviour
{
    public Action<Profile> OnSelected;
    public bool IsSelected = false;
    
    public string ProfileName;
    [TextArea]
    public string InformationString;
    public Sprite ProfileSprite;
    public UnityEngine.UI.Button ProfileButton;
    
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private UnityEngine.UI.Image profileImage;
    
    
    public void Initialize()
    {
        profileImage.sprite = ProfileSprite;
        ProfileButton.onClick.RemoveAllListeners();
        ProfileButton.onClick.AddListener(() =>
        {
            OnSelected?.Invoke(this);
        });
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        selectedIndicator.SetActive(selected);
    }
}
