using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InformationPanel : MonoBehaviour
{
    public Action<string> OnSelectedProfile;
    
    [SerializeField] private List<Profile> profiles;
    [SerializeField] private UnityEngine.UI.Image profileImage;
    [SerializeField] private TextMeshProUGUI informationText;

    private Profile selectedProfile;
    private void Start()
    {
        foreach (var profile in profiles)
        {
            profile.OnSelected += OnProfileSelected;
            profile.Initialize();
            profile.SetSelected(false);
        }
        profiles[0].ProfileButton.onClick.Invoke();
    }

    private void OnProfileSelected(Profile inProfile)
    {
        if (inProfile != selectedProfile)
        {
            selectedProfile?.SetSelected(false);
            selectedProfile = inProfile;
        }
        
        inProfile.SetSelected(true);
        profileImage.sprite = selectedProfile.ProfileSprite;
        informationText.text = selectedProfile.InformationString;
        
        OnSelectedProfile?.Invoke(selectedProfile.ProfileName);
    }
}
