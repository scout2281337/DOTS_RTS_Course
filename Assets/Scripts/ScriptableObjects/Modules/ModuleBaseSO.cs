using UnityEngine;
[CreateAssetMenu(fileName = "ModuleBaseSO", menuName = "Scriptable Objects/Modules/ModuleBaseSO")]
public class ModuleBaseSO : ScriptableObject
{
    public int tier;
    public ModuleCategory category;
    public Texture2D wideIcon;
    public string description;
}

public enum ModuleCategory
{
    None,
    WeaponPower,
    TacticalSystem,
    DefensiveProtocol
}
