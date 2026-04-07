using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
partial struct MeleeAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Получаем мир физики
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        NativeList<RaycastHit> raycastHitList = new NativeList<RaycastHit>(Allocator.Temp);
        EntityCommandBuffer ecb =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        // Проходимся по всем юнитам с атакой
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<MeleeAttack> meleeAttack,
            RefRO<Target> target,
            RefRW<UnitMover> unitMover,
            Entity entity
        ) in SystemAPI.Query<
            RefRO<LocalTransform>,
            RefRW<MeleeAttack>,
            RefRO<Target>,
            RefRW<UnitMover>>().WithEntityAccess()) //.WithDisabled<MoveOverride>()
        {
            if (target.ValueRO.targetEntity == Entity.Null) continue;

            if (SystemAPI.HasComponent<StunEffect>(entity)) continue;

            LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);

            // Проверка дистанции (квадрат расстояния)
            float meleeDistance = meleeAttack.ValueRO.colliderSize; // например 2f
            bool isCloseEnough = math.distancesq(localTransform.ValueRO.Position, targetTransform.Position) < meleeDistance * meleeDistance;

            bool isTouchingTarget = false;

            if (isCloseEnough)
            {
                float3 dirToTarget = targetTransform.Position - localTransform.ValueRO.Position;
                dirToTarget = math.normalize(dirToTarget);

                float distanceExtra = 0.4f;
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = localTransform.ValueRO.Position,
                    End = localTransform.ValueRO.Position + dirToTarget * (meleeAttack.ValueRO.colliderSize + distanceExtra),
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = ~0u,
                        GroupIndex = 0
                    }
                };

                raycastHitList.Clear();
                if (collisionWorld.CastRay(raycastInput, ref raycastHitList))
                {
                    foreach (var hit in raycastHitList)
                    {
                        if (hit.Entity == target.ValueRO.targetEntity)
                        {
                            isTouchingTarget = true;
                            break;
                        }
                    }
                }
            }

            // Двигаемся к цели или остаёмся на месте
            if (!isCloseEnough && !isTouchingTarget)
            {
                unitMover.ValueRW.targetPosition = targetTransform.Position;
                
            }
            else
            {
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;

                // Таймер атаки
                meleeAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (meleeAttack.ValueRW.timer > 0f) continue;

                meleeAttack.ValueRW.timer = meleeAttack.ValueRO.timerMax;

                // Берём EventHub и добавляем DamageEvent
                var hub = SystemAPI
                    .QueryBuilder()
                    .WithAll<EventHub>()
                    .Build()
                    .GetSingletonEntity();

                var damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hub);

                damageBuffer.Add(new DamageEvent
                {
                    SourceEntity = entity,
                    TargetEntity = target.ValueRO.targetEntity,
                    TargetEntityClass = SystemAPI.GetComponent<Unit>(target.ValueRO.targetEntity).Class,
                    DamageAmount = meleeAttack.ValueRO.damageAmount
                });
                if (SystemAPI.HasComponent<Adrenaline>(target.ValueRO.targetEntity)) 
                {
                    RefRW<Adrenaline> adrenaline = SystemAPI.GetComponentRW<Adrenaline>(target.ValueRO.targetEntity);
                    if (adrenaline.ValueRO.CanActivate) 
                    {
                        continue;
                    }
                    adrenaline.ValueRW.CanActivate = true;
                }


                // ===================
                //проверка на наличие компонента для анимации атаки
                //====================
                if (!SystemAPI.HasComponent<AttackRequest>(entity))
                {
                    ecb.AddComponent<AttackRequest>(entity);
                }
            }
        }
        raycastHitList.Dispose();
    }
}



