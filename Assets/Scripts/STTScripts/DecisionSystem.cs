using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DecisionSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private UnityEngine.UI.Button newQuestionButton;
    [SerializeField] private UnityEngine.UI.Button recordButton;
    [SerializeField] private UnityEngine.UI.Image recordImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private UnityEngine.UI.Slider scoreSlider;

    [SerializeField] private Sprite recordingSprite;
    [SerializeField] private Sprite notRecordingSprite;
    [FormerlySerializedAs("perfectScore")] [SerializeField] private int perfectOneRoundScore = 100;
    [SerializeField] private float finalScore = 1000;
    
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private List<string> answerBook = new List<string>();

    private bool isRecording = false;
    private string answerString = string.Empty;
    private float totalScore = 0;
    private void Start()
    {
        answerString = questionText.text;
        
        newQuestionButton.onClick.AddListener(SetNewQuestion);
        
        recordButton.onClick.AddListener(() =>
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
        
        microphoneRecorder.transcriptionCompleteCallback += OnTranscriptionComplete;
        recordImage.sprite = notRecordingSprite;
        scoreSlider.value = 0;
    }

    private void SetNewQuestion()
    {
        answerString = answerBook[UnityEngine.Random.Range(0, answerBook.Count)];
        questionText.text = answerString;
    }

    private void StartRecording()
    {
        isRecording = true;
        recordImage.sprite = recordingSprite;
        microphoneRecorder.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        recordImage.sprite = notRecordingSprite;
        microphoneRecorder.StopRecording();
    }

    private void OnTranscriptionComplete(string inTranscribedString)
    {
        int score = Mathf.Max(0, perfectOneRoundScore - SimpleCharacterDifference(inTranscribedString, answerString));
        scoreText.text = score.ToString();
        totalScore += score;
        
        scoreSlider.value = totalScore / finalScore;
    }

    private int SimpleCharacterDifference(string a, string b)
    {
        int length = Mathf.Min(a.Length, b.Length);
        int difference = Mathf.Abs(a.Length - b.Length);

        for (int i = 0; i < length; i++)
        {
            if (a[i] != b[i])
                difference++;
        }

        return difference;
    }
}
