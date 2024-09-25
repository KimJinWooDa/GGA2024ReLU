using System;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Serialization;

public class ResponseData
{
    public int rating { get; set; }
    public string text { get; set; }
    public string emotion { get; set; }
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
    
    private void Start()
    {
        loadingIndicator.SetActive(false);
        displayClaudeText.gameObject.SetActive(false);
        displayUserText.gameObject.SetActive(false);
        informationPanel.OnSelectedProfile += OnProfileSelected;
        
        for(int i = 0; i < characterPrompts.characterPrompts.Count; i++)
        {
            characterPromptDict.TryAdd(characterPrompts.characterPrompts[i].characterName,
                characterPrompts.characterPrompts[i]);
        }
    }

    private void OnProfileSelected(string selectedName)
    {
        selectedProfileName = selectedName;
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
        if (selectedProfile.isConfession)
        {
            characterPrompt = selectedProfile.confessionPrompt;
        }
        else
        {
            characterPrompt = selectedProfile.prompt;
        }
        
        yield return claudeClient.GetResponseCoroutine(characterPrompt, userMessage, (response) =>
        {
            try
            {
                ResponseData responseData = JsonConvert.DeserializeObject<ResponseData>(response);
                displayClaudeText.text = responseData.text; // Claude의 응답을 텍스트에 표시
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize the response: {ex.Message}");
                displayClaudeText.text = "\nClaude: " + response;  // Claude의 응답을 텍스트에 추가로 표시
            }
            displayClaudeText.gameObject.SetActive(true);
        });
        
        loadingIndicator.SetActive(false);
    }
}
