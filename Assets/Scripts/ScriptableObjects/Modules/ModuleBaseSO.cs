using UnityEngine;
[CreateAssetMenu(fileName = "ModuleBaseSO", menuName = "Scriptable Objects/Modules/ModuleBaseSO")]
public class ModuleBaseSO : ScriptableObject
{
    public int tier;
    public ModuleCategory category;
    public ModuleEffectType effectType;
    public Texture2D wideIcon;
    [TextArea] public string description;
}

public enum ModuleCategory
{
    None,
    WeaponPower,
    TacticalSystem,
    DefensiveProtocol
}

public enum ModuleEffectType
{
    None,
    Berserker,
    Ricochet,
    AcidBullets,
    EnergyVampire,
    ExtraBattery,
    DeafeningEcho,
    Vampirism,
    BloodySpeedUp,
    SupplyLines,
    DoubleShell
}
