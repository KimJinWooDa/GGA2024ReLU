using System.Collections;
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
    [SerializeField] private Animator catAnimator;
    [SerializeField] private TextMeshProUGUI attemptText;
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private UnityEngine.UI.Button restartButton;

    [SerializeField] private Sprite recordingSprite;
    [SerializeField] private Sprite notRecordingSprite;
    [FormerlySerializedAs("perfectScore")] [SerializeField] private int perfectOneRoundScore = 100;
    [SerializeField] private float finalScore = 1000;
    
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private AzureSTT azureSTT;
    [SerializeField] private List<string> answerBook = new List<string>();

    //options
    public enum STTType
    {
        Azure,
        Whisper
    }
    public enum CalculationType
    {
        Levenshtein,
        SimpleCharacterDifference
    }

    public enum StringCompareType
    {
        Phoneme,
        Syllable,
    }
    STTType sttType = STTType.Azure;
    CalculationType calculationType = CalculationType.Levenshtein;
    StringCompareType stringCompareType = StringCompareType.Syllable;
    
    private bool isRecording = false;
    private string answerString = string.Empty;
    private float totalScore = 0;
    private const string isMove = "IsMove";
    private void Start()
    {
        answerString = questionText.text;
        
        newQuestionButton.onClick.RemoveAllListeners();
        newQuestionButton.onClick.AddListener(SetNewQuestion);
        
        recordButton.onClick.RemoveAllListeners();
        recordButton.onClick.AddListener(() =>
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecordingWhisper();
            }
        });
        
        azureSTT.TranscriptionCompleteCallback += OnTranscriptionComplete;
        
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartGame);
        
        recordImage.sprite = notRecordingSprite;
        scoreSlider.value = 0;
    }

    private void RestartGame()
    {
        endGamePanel.gameObject.SetActive(false);
        scoreSlider.value = 0;
        totalScore = 0;
        attemptText.text = string.Empty;
    }

    private void SetNewQuestion()
    {
        answerString = answerBook[UnityEngine.Random.Range(0, answerBook.Count)];
        questionText.text = answerString;
    }

    private void StartRecording()
    {
        SetToStartRecording();

        if (sttType == STTType.Whisper)
        {
            azureSTT.IsEnabled = false;
            microphoneRecorder.StartRecording();
            microphoneRecorder.transcriptionCompleteCallback -= OnTranscriptionComplete;
            microphoneRecorder.transcriptionCompleteCallback += OnTranscriptionComplete;
        }
        else
        {
            azureSTT.IsEnabled = true;
            azureSTT.RecognizeSpeech();
        }
    }

    private void SetToStartRecording()
    {
        isRecording = true;
        recordImage.sprite = recordingSprite;
    }
    private void SetToStopRecording()
    {
        isRecording = false;
        recordImage.sprite = notRecordingSprite;
    }
    private void StopRecordingWhisper()
    {
        SetToStopRecording();

        if (sttType == STTType.Whisper)
        {
            microphoneRecorder.StopRecording();
        }
    }

    private void OnTranscriptionComplete(string inTranscribedString)
    {
        if (sttType == STTType.Azure)
        {
            SetToStopRecording();
        }
        
        int distance = 0;
        if (calculationType == CalculationType.Levenshtein)
        {
            distance = LevenshteinDistance(inTranscribedString, answerString);
        }
        else if (calculationType == CalculationType.SimpleCharacterDifference)
        {
            distance = SimpleCharacterDifference(inTranscribedString, answerString);
        }
        
        int score = Mathf.Max(0, perfectOneRoundScore - distance);
        scoreText.text = score.ToString();
        float newTotalScore = totalScore + score;
        
        StartCoroutine(UpdateSliderValueOverTime(totalScore, newTotalScore, 1f));
        
        totalScore = newTotalScore;
        if (totalScore > finalScore)
        {
            EndGame();
        }
    }
    private void EndGame()
    {
        endGamePanel.gameObject.SetActive(true);
    }

    private IEnumerator UpdateSliderValueOverTime(float startValue, float endValue, float duration)
    {
        float timeElapsed = 0f;
        catAnimator.SetBool(isMove, true);
        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            scoreSlider.value = Mathf.Lerp(startValue / finalScore, endValue / finalScore, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        scoreSlider.value = endValue / finalScore;
        catAnimator.SetBool(isMove, false);
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
    
    private int LevenshteinDistance(string a, string b)
    {
        int[,] matrix = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (int j = 0; j <= b.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                matrix[i, j] = Mathf.Min(
                    matrix[i - 1, j] + 1,        // deletion
                    matrix[i, j - 1] + 1,        // insertion
                    matrix[i - 1, j - 1] + cost  // substitution
                );
            }
        }

        return matrix[a.Length, b.Length];
    }

}
