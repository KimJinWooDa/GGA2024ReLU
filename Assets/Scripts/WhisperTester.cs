using UnityEngine;
using UnityEngine.UI;  // 버튼을 사용하기 위해 필요
using TMPro;  // TextMeshPro 사용을 위해 필요

public class WhisperTester : MonoBehaviour
{
    // Whisper 작업을 처리하는 RunWhisper 클래스 연결
    public RunWhisper runWhisper;

    // AudioClip과 TextMeshPro는 에디터에서 설정
    public AudioClip audioClip;
    public TextMeshProUGUI resultText;  // 트랜스크립션 결과를 표시할 TextMeshPro
    public Button startButton;  // 트랜스크립션 시작을 위한 버튼

    void Start()
    {
        // 버튼을 눌렀을 때 트랜스크립션 시작
        startButton.onClick.AddListener(StartTranscription);
    }

    // 버튼 클릭 시 호출되는 트랜스크립션 시작 함수
    void StartTranscription()
    {
        if (runWhisper != null && audioClip != null)
        {
            // RunWhisper 클래스의 StartTranscription을 호출하고 콜백 설정
            runWhisper.StartTranscription(audioClip, UpdateResultText);
        }
        else
        {
            Debug.LogError("RunWhisper 인스턴스나 AudioClip이 설정되지 않았습니다.");
        }
    }

    // 트랜스크립션이 완료되었을 때 TextMeshPro의 텍스트를 업데이트하는 함수
    void UpdateResultText(string transcription)
    {
        if (resultText != null)
        {
            resultText.text = transcription;  // 트랜스크립션 결과를 TextMeshPro에 표시
        }
        else
        {
            Debug.LogError("TextMeshPro가 설정되지 않았습니다.");
        }
    }
}
