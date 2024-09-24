using System;
using UnityEngine;

public class Profile : MonoBehaviour
{
    public Action<Profile> OnSelected;
    public bool IsSelected = false;
    
    [TextArea]
    public string information;
    public Sprite profileSprite;
    
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private UnityEngine.UI.Image profileImage;
    [SerializeField] private UnityEngine.UI.Button profileButton;
    
    private void Start()
    {
        profileImage.sprite = profileSprite;
        profileButton.onClick.AddListener(() =>
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
