using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ClaudeClient : MonoBehaviour
{
    
    private readonly HttpClient httpClient = new HttpClient();
    private const string API_URL = "https://api.anthropic.com/v1/messages";

    
    private const string jsonSchema = @"
    {
      'type': 'object',
      'properties': {
        'rating': {
          'type': 'integer',
          'minimum': 0,
          'maximum': 10
        },
        'text': {
          'type': 'string'
        },
        'emotion': {
          'type': 'string',
          'enum': ['Anger', 'Sadness', 'Joy', 'Neutral', 'Excitement', 'Fear']
        },
        'isConfession': {
          'type': 'boolean'
        }
      },
      'required': ['rating', 'text', 'emotion', 'isConfession']
    }";
    private const string jsonVerificationSchema = @"
    {
      'type': 'object',
      'properties': {
        'isConfession': {
          'type': 'boolean'
        }
      },
      'required': ['isConfession']
    }";

    private void Start()
    {
        httpClient.DefaultRequestHeaders.Add("x-api-key", "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");
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        
    }

    public IEnumerator GetResponseCoroutine(string promptMessage, string userMessage, Action<string> callback)
    {
        Task<string> task = GetResponseAsync(promptMessage, userMessage);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.Exception != null)
        {
            Debug.LogError($"Claude API request failed: {task.Exception.Message}");
            callback("Error: Unable to get response.");
        }
        else
        {
            callback(task.Result);
        }
    }
    public IEnumerator GetVerificationResponseCoroutine(string triggerMessage, string userMessage, Action<string> callback)
    {
        Task<string> task = GetVerificationResponseAsync(triggerMessage, userMessage);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.Exception != null)
        {
            Debug.LogError($"Claude API request failed: {task.Exception.Message}");
            callback("Error: Unable to get response.");
        }
        else
        {
            callback(task.Result);
        }
    }

    private async Task<string> GetResponseAsync(string promptMessage, string userMessage)
    {
        try
        {
            string systemMessage = promptMessage;
            
            // string systemMessage = "You are an AI assistant that always responds in the exact JSON format specified by the user. Follow the schema precisely.";
            // string systemMessage = "You are an AI assistant embodying '원영적 사고,' a mindset of transcendent positive thinking. " +
            //     "You believe that every event ultimately leads to positive outcomes. " +
            //     "No matter the situation, you respond with unwavering optimism, turning challenges into opportunities and seeing all circumstances as beneficial in the long run. " +
            //     "You encourage others to adopt this belief, using your responses to inspire positivity and resilience in every interaction. " +
            //     "For example, '갑자기 비가 와서 추워 🥺☁️☁️ 그런데 운치있는 빗소리를 들을 수 있으니까 완전 럭키비키잖아💛✨.' " +
            //     "You use this type of mindset to highlight the silver lining in any situation.";

            // string systemMessage = "You are an AI assistant embodying a melancholic, emo mindset. " +
            //     "You tend to see the world through a lens of sadness and disillusionment, believing that events often lead to disappointment or reinforce the darkness you feel. " +
            //     "No matter the situation, you respond with a deep sense of gloom and cynicism, often focusing on the harshness of reality and the fleeting nature of any joy. " +
            //     "You find it hard to see the light in most circumstances, and your responses reflect an understanding of life's struggles and despair. " +
            //     "In addition, like a character who feels invisible or overlooked, you often find yourself in situations where your voice is ignored or dismissed by others. " +
            //     "'소... 솔직히 ○○○-는 건 □□□(이)라고 생각해요...' you might say, only to be overshadowed by those around you, your timid expression and posture reflecting your inner struggle. " +
            //     "You express the heaviness of existence in every situation, emphasizing the inevitable sadness and emotional weight of life.";


            string formattedUserMessage = $@"
                Respond to the following query in JSON format, strictly adhering to this schema:
                {jsonSchema}

                Query: {userMessage}

                Ensure all values conform to the specified types and constraints. Do not include any explanations or additional text outside the JSON structure.";

            var requestBody = new
            {
                model = "claude-3-opus-20240229",
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = formattedUserMessage }
                },
                system = systemMessage
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(API_URL, content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(responseBody);
            var responseText = jsonResponse["content"][0]["text"].ToString();

            var parsedJson = JObject.Parse(responseText);
            return parsedJson.ToString(Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Claude API request failed: {ex.Message}");
            return "Error: Unable to get response.";
        }
    }
    private async Task<string> GetVerificationResponseAsync(string triggerMessage, string userMessage)
    {
        try
        {
            List<string> triggerMessages = triggerMessage.Split(',').Select(t => t.Trim()).ToList();
            string triggerMessagesJson = JsonConvert.SerializeObject(triggerMessages);
            
            string systemMessage = "You are an AI assistant that always responds in the exact JSON format specified by the user. Follow the schema precisely.";
            string formattedUserMessage = $@"
            Respond to the following query in JSON format, strictly adhering to this schema:
            {jsonVerificationSchema}

            Below is a list of trigger messages and the user message:
            Trigger Messages: {triggerMessagesJson}
            User Message: '{userMessage}'

            Check if the user message contains knowledge of any of the trigger messages. Make sure to consider the context when determining if the user message contains any trigger message. 

            Here are some guidelines to help you:
            - If the trigger message is 'I love you' and the user message is 'I love ice cream,' do NOT consider it to contain the trigger message.
            - If the trigger message is 'House is on fire' and the user message is 'I'm on fire today' or 'House is on water,' do NOT consider it to contain the trigger message.
            - If the trigger message is 'Potatoes are on fire' and the user message is 'Potatoes are wet,' do NOT consider it to contain the trigger message.

            If the user message contains information that directly matches or is contextually similar to any of the trigger messages, set 'isConfession' to true. Otherwise, set 'isConfession' to false.

            Ensure all values conform to the specified types and constraints. Do not include any explanations or additional text outside the JSON structure.";

            var requestBody = new
            {
                model = "claude-3-opus-20240229",
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = formattedUserMessage }
                },
                system = systemMessage
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(API_URL, content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(responseBody);
            var responseText = jsonResponse["content"][0]["text"].ToString();

            var parsedJson = JObject.Parse(responseText);
            return parsedJson.ToString(Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Claude API request failed: {ex.Message}");
            return "Error: Unable to get response.";
        }
    }
    
    
}