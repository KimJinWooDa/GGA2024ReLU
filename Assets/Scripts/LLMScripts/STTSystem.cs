using System;
using TMPro;
using UnityEngine;

public class STTSystem : MonoBehaviour
{
    public DecisionSystem.STTType sttType;
    public DecisionSystem.WhisperModel whisperModel;
    
    [SerializeField] private TextMeshProUGUI transcriptionDisplay;
    [SerializeField] private UnityEngine.UI.Button recordingButton;
    [SerializeField] private UnityEngine.UI.Image recordingImage;
    [SerializeField] private Sprite recordingSprite;
    [SerializeField] private Sprite notRecordingSprite;
    [SerializeField] private AzureSTT azureSTT;
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private RunWhisper runWhisper;

    private bool isRecording = false;
    private DecisionSystem.WhisperModel currentWhisperModel;
    private void Start()
    {
        recordingButton.onClick.RemoveAllListeners();
        recordingButton.onClick.AddListener(() =>
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        });
        
        azureSTT.TranscriptionCompleteCallback += OnTranscriptionComplete;
        currentWhisperModel = this.whisperModel;
        runWhisper.ReloadModel(currentWhisperModel);
    }

    private void OnTranscriptionComplete(string inTranscribedString)
    {
        //@ todo: send the transcribed string to the llm system
        if (sttType == DecisionSystem.STTType.Azure)
        {
            SetRecordingState(false);
        }
        transcriptionDisplay.text = inTranscribedString;
        transcriptionDisplay.gameObject.SetActive(true);
    }

    private void StartRecording()
    {
        SetRecordingState(true);

        if (sttType == DecisionSystem.STTType.Whisper)
        {
            if (currentWhisperModel != whisperModel)
            {
                currentWhisperModel = whisperModel;
                runWhisper.ReloadModel(currentWhisperModel);
            }
            
            azureSTT.IsEnabled = false;
            microphoneRecorder.StartRecording();
            microphoneRecorder.transcriptionCompleteCallback -= OnTranscriptionComplete;
            microphoneRecorder.transcriptionCompleteCallback += OnTranscriptionComplete;
        }
        else if (sttType == DecisionSystem.STTType.Azure)
        {
            azureSTT.IsEnabled = true;
            azureSTT.RecognizeSpeech();
        }
    }
    private void StopRecording()
    {
        SetRecordingState(false);

        if (sttType == DecisionSystem.STTType.Whisper)
        {
            microphoneRecorder.StopRecording();
        }
    }

    
    
    private void SetRecordingState(bool isRecording)
    {
        this.isRecording = isRecording;
        recordingImage.sprite = isRecording ? recordingSprite : notRecordingSprite;
    }
}
