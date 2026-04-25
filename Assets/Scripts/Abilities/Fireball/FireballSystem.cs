using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]

public partial struct FireballSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var hub = SystemAPI.QueryBuilder().WithAll<EventHub>().Build().GetSingletonEntity();
        var damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hub);
        foreach (var (fireball, localTransform) in
            SystemAPI.Query<RefRW<Fireball>, RefRO<LocalTransform>>())
        {
            fireball.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if (fireball.ValueRO.Timer >= 0f)
                continue;

            fireball.ValueRW.Timer = fireball.ValueRO.TimerMax;

            var hits = new NativeList<DistanceHit>(16, Allocator.Temp);
            physicsWorld.OverlapSphere(
                localTransform.ValueRO.Position,
                fireball.ValueRO.Radius,
                ref hits,
                CollisionFilter.Default);

            //Debug.Log($"Fireball at {localTransform.ValueRO.Position} | Radius: {fireball.ValueRO.Radius} | Hits found: {hits.Length}"); 

            foreach (var h in hits)
            {
                Entity hitEntity = physicsWorld.Bodies[h.RigidBodyIndex].Entity;

                if (!SystemAPI.HasComponent<Unit>(hitEntity)) continue;
                if (hitEntity == fireball.ValueRO.Owner) continue;

                Unit targetUnit = SystemAPI.GetComponent<Unit>(hitEntity);

                damageBuffer.Add(new DamageEvent
                {
                    SourceEntity = fireball.ValueRO.Owner,
                    TargetEntity = hitEntity,
                    TargetEntityClass = targetUnit.Class,
                    DamageAmount = fireball.ValueRO.Damage,
                    //IsAbilityDamage = true
                });

                //Debug.Log($"Fireball added damage to entity {hitEntity} | Damage: {fireball.ValueRO.Damage}");
            }
            hits.Dispose();
        }
    }
}