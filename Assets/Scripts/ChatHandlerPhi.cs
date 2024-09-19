using UnityEngine;
using TMPro;
using System.Collections;

public class ChatHandlerPhi : MonoBehaviour
{
    public TMP_InputField userInputField;  // User input field
    public TextMeshProUGUI displayText;    // Text to display input/output
    public RunPhi15 runPhi15;              // Reference to the RunPhi15 class for text generation

    private bool isFirstResponse = true;   // Flag to check if it's the first response

    // Called when the user ends input
    public void OnEndEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(userInputField.text))
        {
            string userMessage = userInputField.text;
            displayText.text = "You entered: " + userMessage;  // Display user input

            // Call RunPhi15 to process the input and generate text
            isFirstResponse = true;
            StartCoroutine(SendToRunPhi15(userMessage));

            // Clear input field
            userInputField.text = "";
        }
    }

    // Coroutine to send the user input to RunPhi15 and get the generated response
    private IEnumerator SendToRunPhi15(string userMessage)
    {
        yield return runPhi15.GenerateText(userMessage, (response) =>
        {
            if (isFirstResponse)
            {
                // First response, add 'Generated:' prefix
                displayText.text += "\nGenerated: " + response;
                isFirstResponse = false;  // After the first response, set the flag to false
            }
            else
            {
                // For subsequent responses, just append the text without the prefix
                displayText.text += response;
            }
        });
    }
}