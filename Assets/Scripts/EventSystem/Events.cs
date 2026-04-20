using Unity.Entities;
using Unity.Mathematics;

public struct BulletShotEvent : IBufferElementData
{
    //public Entity Shooter;
    public WeaponType WeaponType; // TO-DO
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

public struct AbilityStartedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
    public float3 Start; // TO-DO
    public float3 End; // TO-DO
    public float Duration; // TO-DO in seconds
}

public struct AbilityEndedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
}

public struct AbilityCooldownEndedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
}