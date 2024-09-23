using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DecisionSystem : MonoBehaviour
{
    //UI 요소들 
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

    //녹음 상태 이미지 교체 리소스
    [SerializeField] private Sprite recordingSprite;
    [SerializeField] private Sprite notRecordingSprite;
    
    //게임 설정
    [SerializeField] private int perfectOneRoundScore = 100;
    [SerializeField] private float finalScore = 1000;
    
    //STT, 음성인식, 비교 관련
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private AzureSTT azureSTT;
    [SerializeField] private List<string> answerBook = new List<string>();

    //토글 UI 요소들 
    [SerializeField] private Toggle whisperToggle;
    [SerializeField] private Toggle azureToggle;
    [SerializeField] private Toggle levenshteinToggle;
    [SerializeField] private Toggle simpleCharacterDifferenceToggle;
    [SerializeField] private Toggle phonemeToggle;
    [SerializeField] private Toggle syllableToggle;
    
    // Enum definitions for STT, comparison, and string comparison types
    private enum STTType { Whisper, Azure }
    private enum CalculationType { Levenshtein, SimpleCharacterDifference }
    private enum StringCompareType { Phoneme, Syllable }
    
    // Current settings
    private STTType sttType = STTType.Whisper;
    private CalculationType calculationType = CalculationType.SimpleCharacterDifference;
    private StringCompareType stringCompareType = StringCompareType.Syllable;
    
    //현재 게임 상태 
    private bool isRecording = false;
    private string answerString = string.Empty;
    private float totalScore = 0;
    private const string isMove = "IsMove";
    private void Start()
    {
        InitializeGame();
        SetupUIListeners();
    }

    private void InitializeGame()
    {
        RestartGame();
        SetNewQuestion();
        recordImage.sprite = notRecordingSprite;
        scoreSlider.value = 0;
        azureSTT.TranscriptionCompleteCallback += OnTranscriptionComplete;
    }

    private void SetupUIListeners()
    {
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
                StopRecording();
            }
        });
        
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartGame);
    }

    public void OnToggle()
    {
        if (whisperToggle != null && azureToggle != null)
        {
            sttType = whisperToggle.isOn ? STTType.Whisper : STTType.Azure;
        }

        if (levenshteinToggle != null && simpleCharacterDifferenceToggle != null)
        {
            calculationType = levenshteinToggle.isOn ? CalculationType.Levenshtein : CalculationType.SimpleCharacterDifference;
        }
        
        if (phonemeToggle != null && syllableToggle != null)
        {
            stringCompareType = phonemeToggle.isOn ? StringCompareType.Phoneme : StringCompareType.Syllable;
        }
    }
    private void SetNewQuestion()
    {
        answerString = answerBook[UnityEngine.Random.Range(0, answerBook.Count)];
        questionText.text = answerString;
    }

    //API related functions ==================================================================================
    private void StartRecording()
    {
        SetRecordingState(true);

        if (sttType == STTType.Whisper)
        {
            azureSTT.IsEnabled = false;
            microphoneRecorder.StartRecording(); //STT 녹음 시작
            microphoneRecorder.transcriptionCompleteCallback -= OnTranscriptionComplete;
            microphoneRecorder.transcriptionCompleteCallback += OnTranscriptionComplete;
        }
        else
        {
            azureSTT.IsEnabled = true;
            azureSTT.RecognizeSpeech(); //Azure 녹음 시작
        }
    }
    private void StopRecording()
    {
        SetRecordingState(false);
        
        //@note: Azure는 자동 녹음 일시 중지 됨. 
        if (sttType == STTType.Whisper)
        {
            microphoneRecorder.StopRecording(); //STT 녹음 중지
        }
    }
    //API related functions end ===============================================================================

    private void SetRecordingState(bool isRecording)
    {
        this.isRecording = isRecording;
        recordImage.sprite = isRecording ? recordingSprite : notRecordingSprite;
    }

    private void OnTranscriptionComplete(string inTranscribedString)
    {
        //@note: Azure는 자동 녹음 일시 중지 되지만 이미지 및 상태만 변경 필요. 
        if (sttType == STTType.Azure)
        {
            SetRecordingState(false);
        }

        if (stringCompareType == StringCompareType.Phoneme)
        {
            inTranscribedString = DecomposeKoreanToPhonemes(inTranscribedString);
            answerString = DecomposeKoreanToPhonemes(answerString);
        }
        
        // Perform selected comparison
        int distance = calculationType == CalculationType.Levenshtein ?
            LevenshteinDistance(inTranscribedString, answerString) :
            SimpleCharacterDifference(inTranscribedString, answerString);

        UpdateScore(distance);
    }

    private void UpdateScore(int distance)
    {
        int score = Mathf.Max(0, perfectOneRoundScore - distance);
        scoreText.text = score.ToString();
        float newTotalScore = totalScore + score;

        StartCoroutine(UpdateSliderValueOverTime(totalScore, newTotalScore, 1f));
        totalScore = newTotalScore;

        if (totalScore >= finalScore)
        {
            EndGame();
        }
    }


    private string DecomposeKoreanToPhonemes(string input)
    {
        string result = string.Empty;

        foreach (char c in input)
        {
            // Check if the character is a Korean syllable
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                int unicodeIndex = c - 0xAC00;
                int initial = unicodeIndex / (21 * 28);
                int medial = (unicodeIndex % (21 * 28)) / 28;
                int final = unicodeIndex % 28;

                // Add the individual phonemes (초성, 중성, 종성)
                result += (char)(0x1100 + initial);  // 초성 (initial)
                result += (char)(0x1161 + medial);   // 중성 (medial)

                if (final != 0)
                {
                    result += (char)(0x11A7 + final); // 종성 (final)
                }
            }
            else
            {
                // If not a Korean syllable, keep the character as it is
                result += c;
            }
        }

        return result;
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
    
    private void RestartGame()
    {
        endGamePanel.gameObject.SetActive(false);
        scoreSlider.value = 0;
        totalScore = 0;
        scoreText.text = string.Empty;
        attemptText.text = string.Empty;
    }
    private void EndGame()
    {
        endGamePanel.gameObject.SetActive(true);
    }

}
