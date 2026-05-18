using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public enum WorldEventType
{
    Custom,
    Noise,
    ZombieHorde,
    ResourceFound,
    ObjectiveUpdated,
    UnitUnderAttack,
    UnitDeath,
    BossSpawn,
    ExtractionPoint,
    ArtilleryStrike
}

public enum WorldEventImportance
{
    Low,
    Medium,
    High,
    Critical
}

public enum WorldEventKnowledge
{
    Exact,
    Approximate,
    DirectionOnly
}

public struct WorldEvent : IBufferElementData
{
    public WorldEventType Type;
    public WorldEventImportance Importance;
    public WorldEventKnowledge Knowledge;
    public float3 Position;
    public float Radius;
    public float Duration;
    public FixedString64Bytes Label;
}
