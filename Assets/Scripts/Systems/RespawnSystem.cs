using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RespawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRW<Health> health, RefRO<RespawnRequest> request, Entity entity) in
                 SystemAPI.Query<RefRW<Health>, RefRO<RespawnRequest>>().WithEntityAccess())
        {
            float3 respawnPosition = request.ValueRO.UseCustomPosition
                ? request.ValueRO.Position
                : ResolveRespawnPosition(state.EntityManager, entity);

            health.ValueRW.healthAmount = health.ValueRO.healthAmountMax;
            health.ValueRW.OnHealthChanged = true;

            if (SystemAPI.HasComponent<LocalTransform>(entity))
            {
                LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(entity);
                transform.Position = respawnPosition;
                ecb.SetComponent(entity, transform);
            }

            if (SystemAPI.HasComponent<UnitMover>(entity))
            {
                UnitMover mover = SystemAPI.GetComponent<UnitMover>(entity);
                mover.CurrentMoveSpeed = mover.BaseSpeed;
                mover.targetPosition = respawnPosition;
                ecb.SetComponent(entity, mover);
            }

            if (SystemAPI.HasComponent<Target>(entity))
            {
                Target target = SystemAPI.GetComponent<Target>(entity);
                target.targetEntity = Entity.Null;
                ecb.SetComponent(entity, target);
            }

            if (SystemAPI.HasComponent<MoveOverride>(entity))
                ecb.SetComponentEnabled<MoveOverride>(entity, false);

            if (SystemAPI.HasComponent<Selected>(entity))
                ecb.SetComponentEnabled<Selected>(entity, false);

            if (SystemAPI.HasComponent<AnimationStateComponent>(entity))
            {
                ecb.SetComponent(entity, new AnimationStateComponent
                {
                    Value = AnimationState.Idle
                });
            }

            if (SystemAPI.HasComponent<DeadUnit>(entity))
                ecb.RemoveComponent<DeadUnit>(entity);

            ecb.RemoveComponent<RespawnRequest>(entity);
        }
    }

    private static float3 ResolveRespawnPosition(EntityManager em, Entity entity)
    {
        if (em.HasComponent<RespawnPoint>(entity))
            return em.GetComponentData<RespawnPoint>(entity).Position;

        if (em.HasComponent<LocalTransform>(entity))
            return em.GetComponentData<LocalTransform>(entity).Position;

        return float3.zero;
    }
}
