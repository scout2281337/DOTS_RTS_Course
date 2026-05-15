using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;

partial struct FindTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI
            .GetSingleton<PhysicsWorldSingleton>()
            .CollisionWorld;
        ComponentLookup<FogRevealable> fogRevealableLookup =
            SystemAPI.GetComponentLookup<FogRevealable>(true);
        ComponentLookup<FogVisible> fogVisibleLookup =
            SystemAPI.GetComponentLookup<FogVisible>(true);

        NativeList<DistanceHit> hits =
            new NativeList<DistanceHit>(Allocator.Temp);

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<FindTarget> findTarget,
            RefRW<Target> target)
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<FindTarget>,
                RefRW<Target>>()
            .WithNone<DeadUnit>())
        {
            float3 myPos = localTransform.ValueRO.Position;


            if (target.ValueRO.targetEntity != Entity.Null)
            {
                if (!SystemAPI.Exists(target.ValueRO.targetEntity) ||
                    SystemAPI.HasComponent<DeadUnit>(target.ValueRO.targetEntity) ||
                    IsHiddenByFog(fogRevealableLookup, fogVisibleLookup, target.ValueRO.targetEntity))
                {
                    target.ValueRW.targetEntity = Entity.Null;
                }
                else
                {
                    float3 targetPos =
                        SystemAPI.GetComponent<LocalTransform>(
                            target.ValueRO.targetEntity).Position;

                    float distSq =
                        math.distancesq(myPos, targetPos);

                    if (distSq >
                        findTarget.ValueRO.range *
                        findTarget.ValueRO.range)
                    {
                        target.ValueRW.targetEntity = Entity.Null;
                    }
                }
            }


            findTarget.ValueRW.timer -= SystemAPI.Time.DeltaTime;

            if (findTarget.ValueRO.timer > 0f)
                continue;

            findTarget.ValueRW.timer =
                findTarget.ValueRO.timerMax;

            // если цель всё ещё есть — не ищем
            if (target.ValueRO.targetEntity != Entity.Null)
                continue;


            hits.Clear();

            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER,
                GroupIndex = 0
            };

            if (physicsWorld.OverlapSphere(
                myPos,
                findTarget.ValueRO.range,
                ref hits,
                filter))
            {
                Entity closest = Entity.Null;
                float closestDistSq = float.MaxValue;

                foreach (var hit in hits)
                {
                    if (!SystemAPI.Exists(hit.Entity))
                        continue;

                    if (!SystemAPI.HasComponent<Unit>(hit.Entity))
                        continue;

                    if (SystemAPI.HasComponent<DeadUnit>(hit.Entity))
                        continue;

                    if (IsHiddenByFog(fogRevealableLookup, fogVisibleLookup, hit.Entity))
                        continue;

                    Unit unit =
                        SystemAPI.GetComponent<Unit>(hit.Entity);

                    if (unit.faction !=
                        findTarget.ValueRO.targetFaction)
                        continue;

                    float distSq =
                        math.distancesq(myPos, hit.Position);

                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closest = hit.Entity;
                    }
                }

                target.ValueRW.targetEntity = closest;
            }
        }

        hits.Dispose();
    }

    private static bool IsHiddenByFog(
        ComponentLookup<FogRevealable> fogRevealableLookup,
        ComponentLookup<FogVisible> fogVisibleLookup,
        Entity entity)
    {
        return fogRevealableLookup.HasComponent(entity) &&
               (!fogVisibleLookup.HasComponent(entity) ||
                !fogVisibleLookup.IsComponentEnabled(entity));
    }
}
