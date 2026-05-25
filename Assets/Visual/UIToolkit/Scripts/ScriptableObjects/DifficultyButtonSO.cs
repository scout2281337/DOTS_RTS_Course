using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyButtonSO", menuName = "Scriptable Objects/UI/DifficultyButtonSO")]
public class DifficultyButtonSO : ScriptableObject
{
    public Texture2D DifficultyIcon;
    public string DifficultyName;
    [TextArea] public string DifficultyDescription;
    [TextArea] public string DifficultyModifiers;
    public string SceneName;
}