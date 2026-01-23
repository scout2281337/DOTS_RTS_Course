using Unity.Entities;
using Unity.Mathematics;

public struct BulletShotEvent : IBufferElementData
{
    public float3 From;
    public float3 To;
    //public Entity Shooter;
}

public struct AbilityStartedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
}

public struct AbilityEndedEvent : IBufferElementData
{
    public Entity Caster;
    public AbilityType Type;
}
