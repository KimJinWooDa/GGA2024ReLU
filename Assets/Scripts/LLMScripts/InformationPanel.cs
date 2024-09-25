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

    public void OnResponseReceived(int rating, string text, Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.Anger:
                if (!selectedProfile.AngerSprite) return;
                profileImage.sprite = selectedProfile.AngerSprite;
                break;
            case Emotion.Sadness:
                if (!selectedProfile.SadnessSprite) return;
                profileImage.sprite = selectedProfile.SadnessSprite;
                break;
            case Emotion.Joy:
                if (!selectedProfile.JoySprite) return;
                profileImage.sprite = selectedProfile.JoySprite;
                break;
            case Emotion.Neutral:
                if (!selectedProfile.NeutralSprite) return;
                profileImage.sprite = selectedProfile.NeutralSprite;
                break;
            case Emotion.Excitement:
                if (!selectedProfile.ExcitementSprite) return;
                profileImage.sprite = selectedProfile.ExcitementSprite;
                break;
            case Emotion.Fear:
                if (!selectedProfile.FearSprite) return;
                profileImage.sprite = selectedProfile.FearSprite;
                break;
            default:
                if (!selectedProfile.DefaultProfileSprite) return;
                profileImage.sprite = selectedProfile.DefaultProfileSprite;
                break;
        }
    }
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
        profileImage.sprite = selectedProfile.DefaultProfileSprite;
        informationText.text = selectedProfile.InformationString;
        
        OnSelectedProfile?.Invoke(selectedProfile.ProfileName);
    }
}
