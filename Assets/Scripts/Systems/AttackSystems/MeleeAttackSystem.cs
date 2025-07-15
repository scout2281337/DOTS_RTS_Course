using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct MeleeAttackSystem : ISystem
{


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        NativeList<RaycastHit> raycastHitList = new NativeList<RaycastHit>(Allocator.Temp);
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<MeleeAttack> meleeAttack,
            RefRO<Target> target,
            RefRW<UnitMover> unitMover) in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<MeleeAttack>,
                RefRO<Target>,
                RefRW<UnitMover>>().WithDisabled<MoveOverride>()) 
        {
            if (target.ValueRO.targetEntity == Entity.Null) 
            {
                continue;
            }

            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            float meleeAttackDistanceSq = 2f;

            bool isCloseEnoughToAttack = math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position) < meleeAttackDistanceSq;

            bool isTouchingTarget = false;
            if (isCloseEnoughToAttack) 
            {
                float3 dirToTarget = targetLocalTransform.Position - localTransform.ValueRO.Position;
                dirToTarget = math.normalize(dirToTarget);
                float distanceExtraToTestRaycast = 0.4f;

                RaycastInput raycastInput = new RaycastInput
                {
                    Start = localTransform.ValueRO.Position,
                    End = localTransform.ValueRO.Position + dirToTarget * (meleeAttack.ValueRO.colliderSize + distanceExtraToTestRaycast),
                    Filter = CollisionFilter.Default,
                };
                raycastHitList.Clear();
                if (collisionWorld.CastRay(raycastInput, ref raycastHitList )) 
                {
                    foreach (RaycastHit raycastHit in raycastHitList) 
                    {
                        if (raycastHit.Entity == target.ValueRO.targetEntity) 
                        {
                            //we hit target, close enogh to attack
                            isTouchingTarget = true;
                            break;
                        }
                    }
                }
            }
            
            
            
            if (!isCloseEnoughToAttack && !isTouchingTarget)
            {
                unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
            }
            else 
            {
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                meleeAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (meleeAttack.ValueRO.timer > 0) 
                {
                    continue;
                }
                meleeAttack.ValueRW.timer = meleeAttack.ValueRO.timerMax;

                RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
                targetHealth.ValueRW.healthAmount -= meleeAttack.ValueRO.damageAmount;
                targetHealth.ValueRW.OnHealthChanged = true;

            }
        }
    }


}
