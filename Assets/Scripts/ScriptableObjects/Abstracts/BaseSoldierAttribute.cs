using UnityEngine;


public abstract class BaseSoldierAttribute : ScriptableObject
{
    [Header("Base")] 
    public string attributeName;
    [TextArea] public string attributeDescription;
}
