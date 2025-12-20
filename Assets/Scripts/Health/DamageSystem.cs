using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DamageSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state) 
    {
        var em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);


        foreach ((RefRO<DamageEvent> dmg, Entity dmgEntity)
                 in SystemAPI.Query<RefRO<DamageEvent>>().WithEntityAccess()) 
        {
            var target = dmg.ValueRO.TargetEntity;

            if (!em.HasComponent<Health>(target))
            {
                ecb.DestroyEntity(dmgEntity);
                continue;
            }

            float damage = dmg.ValueRO.DamageAmount;

            // защита
            if (SystemAPI.GetComponentRO<Health>(target).ValueRO.Armour != 0)
            {
                var Armour = SystemAPI.GetComponentRO<Health>(target).ValueRO.Armour;
                damage *= (1f - (Armour / 100));
            }

            // применяем урон
            var health = SystemAPI.GetComponentRW<Health>(target);
            health.ValueRW.healthAmount -= damage;

            // удаляем event
            ecb.DestroyEntity(dmgEntity);
        }
        ecb.Playback(em);
        ecb.Dispose();
    }    
}
