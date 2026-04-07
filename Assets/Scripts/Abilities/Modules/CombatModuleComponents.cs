using Unity.Entities;

public struct KillEvent : IBufferElementData
{
    public Entity Killer;
    public Entity Victim;
    public float DamageDealt;
}

public struct UnitDeathConsoleEvent : IBufferElementData
{
    public UnitClass VictimClass;
    public Faction VictimFaction;
    public UnitClass KillerClass;
    public Faction KillerFaction;
    public bool HasVictimUnit;
    public bool HasKillerUnit;
}

public struct RicochetModule : IComponentData
{
    public float Cooldown;
    public float CooldownLeft;
    public float Radius;
    public float DamageMultiplier;
}

public struct AcidBulletsModule : IComponentData
{
    public int MaxStacks;
    public float MoveSlowPerStack;
    public float DamageTakenPerStack;
}

public struct AcidBulletsDebuff : IComponentData
{
    public int Stacks;
    public int MaxStacks;
    public float MoveSlowPerStack;
    public float DamageTakenPerStack;
}

public struct EnergyVampireModule : IComponentData
{
    public float CooldownReductionOnKill;
}

public struct ExtraBatteryModule : IComponentData
{
    public int MaxCharges;
    public int Charges;
}

public struct DeafSoundModule : IComponentData
{
    public float StunDuration;
}

public struct StunEffect : IComponentData
{
    public float TimeLeft;
}

public struct VampirismModule : IComponentData
{
    public float Radius;
    public float HealAmount;
}

public struct BloodySpeedUpModule : IComponentData
{
    public int Stacks;
    public int MaxStacks;
    public float SpeedPerStack;
    public float ResetTimer;
    public float ResetTimerMax;
}

public struct SupplyLinesModule : IComponentData
{
    public float RespawnTimeMultiplier;
}
