using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct DamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;

        if (!SystemAPI.TryGetSingletonEntity<EventHub>(out var hub))
            return;

        var damageEvents = SystemAPI.GetBuffer<DamageEvent>(hub);

        for (int i = 0; i < damageEvents.Length; i++)
        {
            var dmg = damageEvents[i];
            var target = dmg.DamageAmount;

            if (!em.HasComponent<Health>(dmg.TargetEntity))
                continue;

            float damage = dmg.DamageAmount;

            var healthRO = SystemAPI.GetComponentRO<Health>(dmg.TargetEntity);

            // armor
            if (healthRO.ValueRO.armor > 0)
            {
                damage *= 1f - (healthRO.ValueRO.armor / 100f);
            }

            var healthRW = SystemAPI.GetComponentRW<Health>(dmg.TargetEntity);
            healthRW.ValueRW.healthAmount -= damage;
        }
    }
}
