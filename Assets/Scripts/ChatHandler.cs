using UnityEngine;
using TMPro;
using System.Collections;

public class ChatHandler : MonoBehaviour
{
    public TMP_InputField userInputField;  // 유저의 입력을 받을 InputField
    public TextMeshProUGUI displayText;    // 입력된 문자열을 표시할 Text
    public ClaudeClient claudeClient;      // Claude API를 호출할 클라이언트 (다른 MonoBehaviour)

    // 유저가 메시지를 입력했을 때 호출되는 함수
    public void OnEndEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(userInputField.text))  // Enter 키가 눌렸고 입력 필드가 비어있지 않으면
        {
            string userMessage = userInputField.text;
            displayText.text = "You entered: " + userMessage;  // 유저 입력을 화면에 표시

            // ClaudeClient를 통해 메시지를 보내고 응답을 받음
            StartCoroutine(SendToClaude(userMessage));

            // 입력 필드를 비움
            userInputField.text = "";
        }
    }

    // Claude API에 메시지를 보내고 응답을 표시하는 코루틴 함수
    private IEnumerator SendToClaude(string userMessage)
    {
        yield return claudeClient.GetResponseCoroutine(userMessage, (response) =>
        {
            displayText.text += "\nClaude: " + response;  // Claude의 응답을 텍스트에 추가로 표시
        });
    }
}
