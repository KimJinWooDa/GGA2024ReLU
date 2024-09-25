using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterPrompt
{
    public string characterName;
    [TextArea]
    public string prompt;
    [TextArea]
    public string confessionPrompt;
    public bool isConfession;
}

[CreateAssetMenu(fileName = "PromptsSO", menuName = "ScriptableObjects/PromptsSO", order = 1)]
public class PromptsSO : ScriptableObject
{
    public List<CharacterPrompt> characterPrompts;
}
