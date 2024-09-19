using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecisionSystem : MonoBehaviour
{
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private List<string> answerBook = new List<string>();
    private void Start()
    {
        microphoneRecorder.transcriptionCompleteCallback += OnTranscriptionComplete;
    }

    private void OnTranscriptionComplete(string inTranscribedString)
    {
        
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
}
