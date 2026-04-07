using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct FireballSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI
            .GetSingleton<PhysicsWorldSingleton>()
            .CollisionWorld;


        var hub = SystemAPI
            .QueryBuilder()
            .WithAll<EventHub>()
            .Build()
            .GetSingletonEntity();

        var damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hub);

        foreach ((RefRW<Fireball> fireball,
            RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<
                RefRW<Fireball>,
                RefRO<LocalTransform>>()) 
        {
            fireball.ValueRW.Timer -= SystemAPI.Time.DeltaTime;

            if (fireball.ValueRO.Timer >= 0f)
            {
                continue;
            }
            fireball.ValueRW.Timer = fireball.ValueRO.TimerMax;

            var hits = new NativeList<DistanceHit>(Allocator.Temp);

            physicsWorld.OverlapSphere(
                localTransform.ValueRO.Position,
                fireball.ValueRO.Radius,
                ref hits,
                CollisionFilter.Default);

            foreach (var h in hits)
            {
                Entity hitEntity = physicsWorld.Bodies[h.RigidBodyIndex].Entity;
                if (!SystemAPI.HasComponent<Unit>(hitEntity))
                    continue;
                if (hitEntity == fireball.ValueRO.Owner)
                    continue;

                Unit targetUnit = SystemAPI.GetComponent<Unit>(hitEntity);
                damageBuffer.Add(new DamageEvent
                {
                    SourceEntity = fireball.ValueRO.Owner,
                    TargetEntity = hitEntity,
                    TargetEntityClass = targetUnit.Class,
                    DamageAmount = fireball.ValueRO.Damage,
                    IsAbilityDamage = true
                });
            }

            hits.Dispose();
        }    
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
