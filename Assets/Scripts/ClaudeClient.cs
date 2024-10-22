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

    [SerializeField]
    private string API_KEY;
    
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
          'enum': ['Anger', 'Sadness', 'Joy', 'Neutral', 'Surprised']
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
        httpClient.DefaultRequestHeaders.Add("x-api-key", API_KEY);
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
            
            // string systemMessage = "You are an AI assistant that always responds in the exact JSON format specified by the user. Follow the schema precisely.";
            string systemMessage = "You are an AI in character role-playing, implementing the dialogue of a specific character. You respond exactly in the JSON format specified by the user. Follow the schema strictly.";
            
            string formattedUserMessage = $@"
            Respond to the following query in JSON format, strictly adhering to this schema:
            {jsonVerificationSchema}

            Below is a list of trigger messages and the user message:
            User Message: '{userMessage}'" +
            $@"Trigger Messages: {triggerMessagesJson}" + 

            "Ensure that isConfession becomes true only if when the facts contained in the user message match all the content of the trigger message." + 
            "Be sure to consider the context when determining whether the user message fully contains the trigger message." +

            // "Check if the user message contains knowledge of any of the trigger messages." +
            // "Make sure to consider the context when determining if the user message contains all trigger message." + 

            // Important guidelines:
            // - If the user message contains negations or modifications that change the meaning of the trigger message (e.g., adding 'not', 'didn't', 'never'), do NOT consider it as containing the trigger message.
            // - If the user message contains similar words but changes the overall intent or context of the trigger message, do NOT consider it as containing the trigger message.
            
            // Here are some guidelines to help you:
            // - If the trigger message is 'I love you' and the user message is 'I love ice cream,' do NOT consider it to contain the trigger message.
            // - If the trigger message is 'House is on fire' and the user message is 'I'm on fire today' or 'House is on water,' do NOT consider it to contain the trigger message.
            // - If the trigger message is 'Potatoes are on fire' and the user message is 'Potatoes are wet,' do NOT consider it to contain the trigger message.
            // - If the trigger message is 'You did hold onto the ruby' and the user message is 'You did not hold onto the ruby,' do NOT consider it to contain the trigger message.
            // - If the trigger message is 'They are not happy' and the user message is 'They are happy, but I am not,' do NOT consider it to contain the trigger message.

            // If the user message directly confirms or is contextually similar to any of the trigger messages (without negations or changes in meaning), set 'isConfession' to true. Otherwise, set 'isConfession' to false.

            "Ensure all values conform to the specified types and constraints. Do not include any explanations or additional text outside the JSON structure.";

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