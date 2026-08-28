using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SubtitlePhrase
{
    public string text;    
    public float duration;

    public SubtitlePhrase(string text, float duration)
    {
        this.text = text;
        this.duration = duration;
    }
}

[System.Serializable]
public struct DialogueData
{
    public string speakerName;        
    public AudioClip voiceClip;       
    public List<SubtitlePhrase> phrases; 

    public DialogueData(string speaker, AudioClip clip, List<SubtitlePhrase> phrasesList)
    {
        speakerName = speaker;
        voiceClip = clip;
        phrases = phrasesList;
    }
}