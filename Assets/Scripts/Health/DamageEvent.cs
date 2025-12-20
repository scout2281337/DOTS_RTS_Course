using Unity.Entities;

public struct DamageEvent : IComponentData
{
    public Entity TargetEntity;
    public float DamageAmount;
}
