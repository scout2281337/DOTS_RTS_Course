using UnityEngine;

public abstract class ModuleBaseSO : ScriptableObject
{
    public Module module;
    public ModuleCategory category;
    public ModuleTriggerType triggerType;
    public int tier;
    public Texture2D wideIcon;
    [TextArea] public string description;
}

public enum ModuleTriggerType
{
    None,
    Constant,
    Instant,
    Temporarily,
    stackable
}

public enum ModuleCategory
{
    None,
    WeaponPower,
    TacticalSystem,
    DefensiveProtocol
}

public enum Module
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