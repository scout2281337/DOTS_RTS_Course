using Unity.Entities;
using Unity.Mathematics;

public struct BulletShotEvent : IBufferElementData
{
    //public Entity Shooter;
    public WeaponType WeaponType; 
    public float3 Start;
    public float3 End;
}

// For pointing with abilities like "Scorcher"
public struct AbilityPointerEvent : IBufferElementData // TO-DO
{
    public Entity Caster;
    public AbilityType Type;
    public AbilityPointerType PointerType;
    public float Range;
    public float Area;
    public float3 Start; // For LinesFromPointToPoint 
}

public struct AbilityPointerEndedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
}

public struct AbilityStartedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
    public float3 Start; 
    public float3 End; 
    public float Duration;
    public float Range;
    public float Area;
}

public struct AbilityEndedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
    public float Cooldown; // TO-DO
}

public struct AbilityCooldownEndedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
}
