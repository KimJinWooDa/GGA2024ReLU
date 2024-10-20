using System;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Serialization;

public enum Emotion
{
    Anger,
    Sadness,
    Joy,
    Neutral,
    Excitement,
    Fear
}
public class ResponseData
{
    public int rating { get; set; }
    public string text { get; set; }
    public Emotion emotion { get; set; }
}

public class VerificationData
{
    public bool isConfession { get; set; }
}
public class ChatHandler : MonoBehaviour
{
    [SerializeField] private PromptsSO characterPrompts;
    [SerializeField] private GameObject loadingIndicator;  // API 호출 중에 표시할 로딩 인디케이터
    [SerializeField] private InformationPanel informationPanel;
    
    public TMP_InputField userInputField;  // 유저의 입력을 받을 InputField
    [FormerlySerializedAs("displayText")] public TextMeshProUGUI displayUserText;    // 입력된 문자열을 표시할 Text
    public TextMeshProUGUI displayClaudeText;
    public ClaudeClient claudeClient;      // Claude API를 호출할 클라이언트 (다른 MonoBehaviour)

    private string selectedProfileName = string.Empty;
    private Dictionary<string, CharacterPrompt> characterPromptDict = new Dictionary<string, CharacterPrompt>();

    private List<int> usedBirdMessageIndices = new List<int>();

    private string[] birdMessages = { 
        "짹짹! 짹!\n짹짹이는 행복해보인다. 무언가 배부르게 먹은 것일까? 좀 더 자세히 알아볼 필요가 있다.", 
        "짹 째잭! 짹!\n짹짹이의 부리에 붉은 무언가가 묻어 있다.", 
        "짹! 짹! 짹!\n새장 안에 무언가의 씨앗이 떨어져 있다. 짹짹이가 먹고 남은 흔적일까?" 
    };

    
    private void Start()
    {
        loadingIndicator.SetActive(false);
        displayClaudeText.gameObject.SetActive(false);
        displayUserText.gameObject.SetActive(false);
        informationPanel.OnSelectedProfile += OnProfileSelected;
        informationPanel.OnConfessionStatus += OnConfessionStatus;
        
        for(int i = 0; i < characterPrompts.characterPrompts.Count; i++)
        {
            characterPromptDict.TryAdd(characterPrompts.characterPrompts[i].characterName,
                characterPrompts.characterPrompts[i]);
        }
    }

    private void OnConfessionStatus(string name, bool isConfession)
    {
        if (characterPromptDict.ContainsKey(name))
        {
            characterPromptDict[name].isConfession = isConfession;
        }
    }


    private void OnProfileSelected(string selectedName)
    {
        selectedProfileName = selectedName;
        if (characterPromptDict.ContainsKey(selectedName))
        {
            CharacterPrompt characterPrompt = characterPromptDict[selectedName];
            informationPanel.SetToggle(characterPrompt.isConfession);
        }
    }

    // 유저가 메시지를 입력했을 때 호출되는 함수
    public void OnEndEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(userInputField.text))  // Enter 키가 눌렸고 입력 필드가 비어있지 않으면
        {
            displayClaudeText.gameObject.SetActive(false);
            
            string userMessage = userInputField.text;
            displayUserText.text = "You entered: " + userMessage;  // 유저 입력을 화면에 표시
            displayUserText.gameObject.SetActive(true);

            // ClaudeClient를 통해 메시지를 보내고 응답을 받음
            StartCoroutine(SendToClaude(userMessage));

            // 입력 필드를 비움
            userInputField.text = "";
        }
    }

    // Claude API에 메시지를 보내고 응답을 표시하는 코루틴 함수
    private IEnumerator SendToClaude(string userMessage)
    {
        loadingIndicator.SetActive(true);

        string characterPrompt = string.Empty;
        CharacterPrompt selectedProfile = characterPromptDict[selectedProfileName];

        if (selectedProfileName == "Bird")
        {
            // 사용되지 않은 메시지 중에서 랜덤으로 선택
            if (usedBirdMessageIndices.Count == birdMessages.Length)
            {
                usedBirdMessageIndices.Clear(); // 모든 메시지가 한 번씩 사용되면 초기화
            }

            List<int> availableIndices = Enumerable.Range(0, birdMessages.Length)
                                                .Where(i => !usedBirdMessageIndices.Contains(i))
                                                .ToList();

            int randomIndex = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
            usedBirdMessageIndices.Add(randomIndex); // 선택된 메시지의 인덱스를 추가

            string randomBirdMessage = birdMessages[randomIndex];
            
            // 미리 지정한 문자열을 출력
            displayClaudeText.text = randomBirdMessage;
            informationPanel.OnResponseReceived(0, randomBirdMessage, Emotion.Joy); // 임의의 값 사용
            displayClaudeText.gameObject.SetActive(true);
            
            loadingIndicator.SetActive(false);
            yield break; // 코루틴 종료
        }

        if (!selectedProfile.isConfession)
        {
            yield return StartCoroutine(VerifyMessage(selectedProfile.triggerPrompt, userMessage, (isConfession) =>
            {
                if (isConfession)
                {
                    selectedProfile.isConfession = isConfession;
                    informationPanel.SetToggle(true);
                    characterPrompt = selectedProfile.generalPrompt + selectedProfile.confessionPrompt;
                }
                else
                {
                    characterPrompt = selectedProfile.generalPrompt + selectedProfile.beforeConfessionPrompt;
                }
            }));
        }
        else
        {
            characterPrompt = selectedProfile.generalPrompt + selectedProfile.confessionPrompt;
        }
        
        yield return StartCoroutine(SendMessageToClaude(characterPrompt, userMessage));

        loadingIndicator.SetActive(false);
    }

    private IEnumerator VerifyMessage(string triggerMessage, string userMessage, Action<bool> callback)
    {
        yield return claudeClient.GetVerificationResponseCoroutine(triggerMessage, userMessage, (response) =>
        {
            try
            {
                VerificationData verificationData = JsonConvert.DeserializeObject<VerificationData>(response);
                callback(verificationData.isConfession);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize the verification response: {ex.Message}");
                callback(false); //default to false if error 
            }
        });
    }

    private IEnumerator SendMessageToClaude(string prompt, string userMessage)
    {
        yield return claudeClient.GetResponseCoroutine(prompt, userMessage, (response) =>
        {
            try
            {
                ResponseData responseData = JsonConvert.DeserializeObject<ResponseData>(response);
                displayClaudeText.text = responseData.text;
                informationPanel.OnResponseReceived(responseData.rating, responseData.text, responseData.emotion);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize the response: {ex.Message}");
                displayClaudeText.text = "\nClaude: " + response;
            }

            displayClaudeText.gameObject.SetActive(true);
        });
    }
}
