using Unity.Entities;
using Unity.Mathematics;

public struct DeadUnit : IComponentData
{
    public float3 DeathPosition;
}

public struct RespawnRequest : IComponentData
{
    public float3 Position;
    public bool UseCustomPosition;
}

public struct RespawnPoint : IComponentData
{
    public float3 Position;
}

public struct UnitDeathEvent : IBufferElementData
{
    public Entity UnitEntity;
    public UnitClass UnitClass;
    public Faction Faction;
    public float3 Position;
    public bool CanRespawn;
}
