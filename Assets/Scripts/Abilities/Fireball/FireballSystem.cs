using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEditor.Localization.Plugins.XLIFF.V12;

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

        foreach ((RefRW<Fireball> Fireball, 
            RefRO<LocalTransform> LocalTransform) 
            in SystemAPI.Query<
                RefRW<Fireball>, 
                RefRO<LocalTransform>>()) 
        {
            Fireball.ValueRW.Timer -= SystemAPI.Time.DeltaTime;

            if (Fireball.ValueRO.Timer >= 0) 
            {
                return;
            }
            Fireball.ValueRW.Timer = Fireball.ValueRO.TimerMax;

            var hits = new NativeList<DistanceHit>(Allocator.Temp);

            physicsWorld.OverlapSphere(
                            LocalTransform.ValueRO.Position,
                            Fireball.ValueRO.Radius,
                            ref hits,
                            CollisionFilter.Default);

            foreach (var h in hits)
            {
                Entity e =
                    physicsWorld.Bodies[h.RigidBodyIndex].Entity;
                Unit targetUnit =
                    SystemAPI.GetComponent<Unit>(e);

                damageBuffer.Add(new DamageEvent
                {
                    TargetEntity = e,
                    TargetEntityClass = targetUnit.Class,
                    DamageAmount = Fireball.ValueRO.Radius
                });
            }
        }    
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
