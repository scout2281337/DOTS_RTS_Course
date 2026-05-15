using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthDeadSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        DynamicBuffer<UnitDeathEvent> deathEvents = default;
        bool hasEventHub = SystemAPI.TryGetSingletonEntity<EventHub>(out Entity hub);
        if (hasEventHub)
        {
            deathEvents = SystemAPI.GetBuffer<UnitDeathEvent>(hub);
        }

        foreach ((RefRW<Health> health, Entity entity)
            in SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.healthAmount > 0)
                continue;

            if (SystemAPI.HasComponent<DeadUnit>(entity))
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

            float3 position = SystemAPI.HasComponent<LocalTransform>(entity)
                ? SystemAPI.GetComponent<LocalTransform>(entity).Position
                : float3.zero;

            bool isFriendlyUnit = SystemAPI.HasComponent<Unit>(entity)
                && SystemAPI.GetComponent<Unit>(entity).faction == Faction.Friendly;

            if (hasEventHub && SystemAPI.HasComponent<Unit>(entity))
            {
                Unit unit = SystemAPI.GetComponent<Unit>(entity);
                deathEvents.Add(new UnitDeathEvent
                {
                    UnitEntity = entity,
                    UnitClass = unit.Class,
                    Faction = unit.faction,
                    Position = position,
                    CanRespawn = isFriendlyUnit
                });
            }

            if (isFriendlyUnit)
            {
                entityCommandBuffer.AddComponent(entity, new DeadUnit
                {
                    DeathPosition = position
                });

                if (SystemAPI.HasComponent<Target>(entity))
                {
                    Target target = SystemAPI.GetComponent<Target>(entity);
                    target.targetEntity = Entity.Null;
                    entityCommandBuffer.SetComponent(entity, target);
                }

                if (SystemAPI.HasComponent<UnitMover>(entity))
                {
                    UnitMover mover = SystemAPI.GetComponent<UnitMover>(entity);
                    mover.targetPosition = position;
                    mover.CurrentMoveSpeed = 0f;
                    entityCommandBuffer.SetComponent(entity, mover);
                }

                if (SystemAPI.HasComponent<MoveOverride>(entity))
                    entityCommandBuffer.SetComponentEnabled<MoveOverride>(entity, false);

                if (SystemAPI.HasComponent<AttackRequest>(entity))
                    entityCommandBuffer.RemoveComponent<AttackRequest>(entity);

                if (SystemAPI.HasComponent<Selected>(entity))
                {
                    Selected selected = SystemAPI.GetComponent<Selected>(entity);
                    selected.OnSelected = false;
                    selected.OnDeselected = SystemAPI.IsComponentEnabled<Selected>(entity);
                    entityCommandBuffer.SetComponent(entity, selected);
                    entityCommandBuffer.SetComponentEnabled<Selected>(entity, false);
                }

                continue;
            }

            entityCommandBuffer.DestroyEntity(entity);
        }



    }

}
