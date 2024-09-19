using System;
using System.Collections;
using System.Threading.Tasks;
using Claudia;
using UnityEngine;

public class ClaudeClient : MonoBehaviour
{
    private Anthropic anthropic;

    // MonoBehaviour의 Start 함수에서 API 설정
    private void Start()
    {
        anthropic = new Anthropic()
        {
            ApiKey = "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"
        };
    }

    // Claude API에 메시지를 보내고 응답을 받아오는 코루틴
    public IEnumerator GetResponseCoroutine(string userMessage, Action<string> callback)
    {
        Task<string> task = GetResponseAsync(userMessage);

        // Task가 완료될 때까지 대기
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.Exception != null)
        {
            Debug.LogError("Claude API 요청 실패: " + task.Exception.Message);
            callback("Error: Unable to get response.");
        }
        else
        {
            // 결과 콜백 실행
            callback(task.Result);
        }
    }

    // 비동기 함수로 Claude API에 요청을 보내고 응답을 받아오는 함수
    private async Task<string> GetResponseAsync(string userMessage)
    {
        try
        {
            var message = await anthropic.Messages.CreateAsync(new()
            {
                Model = Models.Claude3Opus,  // 모델 설정
                MaxTokens = 1024,  // 최대 토큰 수
                Messages = new Message[] { new() { Role = "user", Content = userMessage } }  // 유저 메시지 전달
            });

            // Claude의 응답을 반환
            return message.Content.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError("Claude API 요청 실패: " + ex.Message);
            return "Error: Unable to get response.";
        }
    }
}
