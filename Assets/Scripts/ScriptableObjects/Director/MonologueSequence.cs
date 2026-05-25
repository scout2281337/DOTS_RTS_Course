using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MonologueSequence", menuName = "Scriptable Objects/Director/Monologue Sequence")]
public class MonologueSequence : ScriptableObject
{
    [Header("Playback")]
    public bool InterruptCurrent = true;
    public bool QueueIfBusy;

    [Header("Lines")]
    public MonologueLine[] Lines = Array.Empty<MonologueLine>();
}

[Serializable]
public class MonologueLine
{
    public string Speaker = "COMMAND";
    [TextArea(2, 5)] public string Text;
    public string Hint = "Нажми \"Дальше\"";
    public AudioClip VoiceClip;
    public MonologueMood Mood = MonologueMood.Radio;
    [Min(0f)] public float StartDelay;
    [Min(0f)] public float Duration = 4f;
    public bool WaitForClick;
}

public enum MonologueMood
{
    Radio,
    Info,
    Warning,
    Danger,
    Success
}
