using UnityEngine;

public abstract class ModuleBaseSO : ScriptableObject
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
    CenterOfAttention,
    Ricochet,
    AcidBullets,
    EnergyVampire,
    ExtraBattery,
    DeafeningReverb,
    Vampirism,
    BloodySpeedUp,
    SupplyLines,
    DoubleShell
}
