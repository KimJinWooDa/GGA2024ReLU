using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DecisionSystem : MonoBehaviour
{
    //설정 가능한 변수들   
    [Header("Settings to calculate the score")]
    [SerializeField] private STTType STTTypeSetting = STTType.Whisper;
    [SerializeField] private WhisperModel WhisperModelSetting = WhisperModel.Tiny;
    [SerializeField] private CalculationType CalculationTypeSetting = CalculationType.SimpleCharacterDifference;
    [SerializeField] private StringCompareType StringCompareTypeSetting = StringCompareType.Syllable;
    
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
    [SerializeField] private RunWhisper runWhisper;
    [SerializeField] private AzureSTT azureSTT;
    [SerializeField] private List<string> answerBook = new List<string>();

    //토글 UI 요소들 
    [SerializeField] private Toggle whisperToggle;
    [SerializeField] private Toggle azureToggle;
    [SerializeField] private Toggle levenshteinToggle;
    [SerializeField] private Toggle simpleCharacterDifferenceToggle;
    [SerializeField] private Toggle simpleCharacterMatchToggle;
    [SerializeField] private Toggle phonemeToggle;
    [SerializeField] private Toggle syllableToggle;
    //Whister관련 추가 toggle
    [SerializeField] private GameObject whisperModelObject;
    [FormerlySerializedAs("tiny")] [SerializeField] private Toggle tinyModel;
    [FormerlySerializedAs("medium")] [SerializeField] private Toggle mediumModel;
    [FormerlySerializedAs("_base")] [SerializeField] private Toggle baseModel;
    
    // Enum definitions for STT, comparison, and string comparison types
    public enum STTType { Whisper, Azure }
    private enum CalculationType { Levenshtein, SimpleCharacterDifference, SimpleCharacterMatch }
    private enum StringCompareType { Phoneme, Syllable }
    public enum WhisperModel { Tiny, Medium, Base }
    
    // Current settings
    private STTType sttType = STTType.Whisper;
    private CalculationType calculationType = CalculationType.SimpleCharacterDifference;
    private StringCompareType stringCompareType = StringCompareType.Syllable;
    private WhisperModel whisperModel = WhisperModel.Tiny;
    
    //현재 게임 상태 
    private bool isRecording = false;
    private string answerString = string.Empty;
    private float totalScore = 0;
    private const string isMove = "IsMove";
    private void Start()
    {
        GetInspectorSettings();
        InitializeGame();
        SetupUIListeners();
    }

    private void GetInspectorSettings()
    {
        sttType = STTTypeSetting;
        if (sttType == STTType.Whisper)
        {
            whisperToggle.isOn = true;
        }
        else if (sttType == STTType.Azure)
        {
            azureToggle.isOn = true;
        }
        calculationType = CalculationTypeSetting; 
        if (calculationType == CalculationType.Levenshtein)
        {
            levenshteinToggle.isOn = true;
        }
        else if (calculationType == CalculationType.SimpleCharacterDifference)
        {
            simpleCharacterDifferenceToggle.isOn = true;
        }
        else if (calculationType == CalculationType.SimpleCharacterMatch)
        {
            simpleCharacterMatchToggle.isOn = true;
        }
        stringCompareType = StringCompareTypeSetting; 
        if (stringCompareType == StringCompareType.Phoneme)
        {
            phonemeToggle.isOn = true;
        }
        else if (stringCompareType == StringCompareType.Syllable)
        {
            syllableToggle.isOn = true;
        }
        whisperModel = WhisperModelSetting; 
        if (whisperModel == WhisperModel.Tiny)
        {
            tinyModel.isOn = true;
            runWhisper.ReloadModel(WhisperModel.Tiny);
        }
        else if (whisperModel == WhisperModel.Medium)
        {
            mediumModel.isOn = true;
            runWhisper.ReloadModel(WhisperModel.Medium);
        }
        else if (whisperModel == WhisperModel.Base)
        {
            baseModel.isOn = true;
            runWhisper.ReloadModel(WhisperModel.Base);
        }
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
        whisperModelObject.gameObject.SetActive(sttType == STTType.Whisper);
        if (whisperToggle != null && azureToggle != null)
        {
            sttType = whisperToggle.isOn ? STTType.Whisper : STTType.Azure;

            if (whisperToggle.isOn)
            {
                if (tinyModel.isOn)
                {
                    if (whisperModel != WhisperModel.Tiny)
                    {
                        whisperModel = WhisperModel.Tiny;
                        runWhisper.ReloadModel(whisperModel);
                    }
                }
                else if (mediumModel.isOn)
                {
                    if (whisperModel != WhisperModel.Medium)
                    {
                        whisperModel = WhisperModel.Medium;
                        runWhisper.ReloadModel(whisperModel);
                    }
                }
                else if (baseModel.isOn)
                {
                    if (whisperModel != WhisperModel.Base)
                    {
                        whisperModel = WhisperModel.Base;
                        runWhisper.ReloadModel(whisperModel);
                    }
                }
            }
            
        }

        if (levenshteinToggle != null && simpleCharacterDifferenceToggle != null && simpleCharacterMatchToggle != null)
        {
            calculationType = levenshteinToggle.isOn ? CalculationType.Levenshtein :
                simpleCharacterDifferenceToggle.isOn ? CalculationType.SimpleCharacterDifference :
                CalculationType.SimpleCharacterMatch;
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
        answerString = CleanString(answerString);
    }

    //API related functions ==================================================================================
    private void StartRecording()
    {
        SetRecordingState(true);

        if (sttType == STTType.Whisper)
        {
            Debug.Log($"Selected Whisper Model: {whisperModel}");
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

    private string CleanString(string input)
    {
        char[] arr = input
            .Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c))
            .Select(c => char.IsLetter(c) && c <= 'Z' ? char.ToLower(c) : c) // Convert only English letters to lowercase
            .ToArray();
        return new string(arr);
    }
    private void OnTranscriptionComplete(string inTranscribedString)
    {
        //@note: Azure는 자동 녹음 일시 중지 되지만 이미지 및 상태만 변경 필요. 
        if (sttType == STTType.Azure)
        {
            SetRecordingState(false);
        }

        inTranscribedString = CleanString(inTranscribedString);
        Debug.Log("Transcribed string: " + inTranscribedString);
        Debug.Log("Answer string: " + answerString);
        
        if (stringCompareType == StringCompareType.Phoneme)
        {
            inTranscribedString = DecomposeKoreanToPhonemes(inTranscribedString);
            answerString = DecomposeKoreanToPhonemes(answerString);
        }
        
        // Perform selected comparison
        int distance = 0;
        if (calculationType == CalculationType.Levenshtein)
        {
            distance = LevenshteinDistance(inTranscribedString, answerString);
        }
        else if (calculationType == CalculationType.SimpleCharacterDifference)
        {
            distance = SimpleCharacterDifference(inTranscribedString, answerString);
        }
        else if (calculationType == CalculationType.SimpleCharacterMatch)
        {
            distance = SimpleCharacterMatch(inTranscribedString, answerString); 
        }

        UpdateScore(distance, answerString.Length);
    }

    private void UpdateScore(int distance, int totalCharacters)
    {
        int score = 0;
        if (calculationType == CalculationType.SimpleCharacterMatch)
        {
            score = (int)(((float)distance / totalCharacters) * 100);
        }
        else
        {
            score = Mathf.Max(0, (int)(((float)(totalCharacters - distance) / totalCharacters) * 100));
        }
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
        Debug.Log("Decomposing Korean to phonemes " + input);
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

        Debug.Log("Decomposed Korean to phonemes " + result);
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
    
    private int SimpleCharacterMatch(string a, string b)
    {
        int length = Mathf.Min(a.Length, b.Length);
        int matchCount = 0;

        for (int i = 0; i < length; i++)
        {
            if (a[i] == b[i])
            {
                matchCount++;
            }
        }

        return matchCount;
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
        OnToggle();
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
