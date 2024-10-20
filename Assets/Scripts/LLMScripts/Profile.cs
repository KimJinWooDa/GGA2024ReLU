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
    public Sprite DefaultProfileSprite;
    public UnityEngine.UI.Button ProfileButton;
    
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private UnityEngine.UI.Image profileImage;
    
    //sprites info
    public Sprite AngerSprite;
    public Sprite SadnessSprite;
    public Sprite JoySprite;
    public Sprite NeutralSprite;
    public Sprite SurprisedSprite;
    // public Sprite ExcitementSprite;
    // public Sprite FearSprite;
    
    
    public void Initialize()
    {
        profileImage.sprite = DefaultProfileSprite;
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
