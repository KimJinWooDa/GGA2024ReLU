using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CharacterPrompt
{
    public string characterName;
    [TextArea] 
    public string generalPrompt;
    [TextArea]
    public string beforeConfessionPrompt;
    [TextArea]
    public string confessionPrompt;
    [TextArea] 
    public string triggerPrompt;
    public bool isConfession;
}

[CreateAssetMenu(fileName = "PromptsSO", menuName = "ScriptableObjects/PromptsSO", order = 1)]
public class PromptsSO : ScriptableObject
{
    public List<CharacterPrompt> characterPrompts;
}
