using Unity.Entities;

public struct DamageEvent : IBufferElementData
{
    public Entity TargetEntity;
    public UnitClass TargetEntityClass;
    public float DamageAmount;
}
