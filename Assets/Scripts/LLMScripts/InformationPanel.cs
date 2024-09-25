using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InformationPanel : MonoBehaviour
{
    public Action<string> OnSelectedProfile;
    public Action<string, bool> OnConfessionStatus;
    
    [SerializeField] private List<Profile> profiles;
    [SerializeField] private TextMeshProUGUI informationText;
    [SerializeField] private UnityEngine.UI.Image ratingBar;
    [SerializeField] private UnityEngine.UI.Image profileImage;
    [SerializeField] private TextMeshProUGUI emotionText;
    [SerializeField] private UnityEngine.UI.Toggle confessionToggle;

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
        ratingBar.fillAmount = rating / 10f;
        emotionText.text = emotion.ToString();
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
        
        confessionToggle.onValueChanged.AddListener(OnConfessionToggle);
    }

    private void OnProfileSelected(Profile inProfile)
    {
        if (inProfile != selectedProfile)
        {
            selectedProfile?.SetSelected(false);
            selectedProfile = inProfile;
        }
        
        inProfile.SetSelected(true);
        ratingBar.fillAmount = 1;
        profileImage.sprite = selectedProfile.DefaultProfileSprite;
        emotionText.text = string.Empty;
        informationText.text = selectedProfile.InformationString;
        
        OnSelectedProfile?.Invoke(selectedProfile.ProfileName);
    }

    private void OnConfessionToggle(bool isConfession)
    {
        OnConfessionStatus?.Invoke(selectedProfile.ProfileName, isConfession);
    }

    public void SetToggle(bool isOn)
    {
        confessionToggle.isOn = isOn;
    }
}
