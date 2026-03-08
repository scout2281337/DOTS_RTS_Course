using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthDeadSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRW<Health> health, Entity entity)
            in SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.healthAmount > 0)
                continue;

            if (SystemAPI.HasComponent<EmergencyStabilization>(entity))
            {
                var stab = SystemAPI.GetComponentRW<EmergencyStabilization>(entity);

                if (stab.ValueRO.CanActivate)
                {
                    stab.ValueRW.CanActivate = false;

                    entityCommandBuffer.AddComponent(entity, new Invulnerable
                    {
                        Timer = 2f
                    });

                    health.ValueRW.healthAmount = 1f;

                    continue;
                }
            }

            entityCommandBuffer.DestroyEntity(entity);
        }



    }

}
