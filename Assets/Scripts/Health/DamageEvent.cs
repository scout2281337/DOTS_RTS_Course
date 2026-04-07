using Unity.Entities;

public struct DamageEvent : IBufferElementData
{
    public Entity SourceEntity;
    public Entity TargetEntity;
    public UnitClass TargetEntityClass;
    public float DamageAmount;
    public bool IsAbilityDamage;
}
