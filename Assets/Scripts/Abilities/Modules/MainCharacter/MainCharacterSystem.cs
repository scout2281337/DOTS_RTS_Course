using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct MainCharacterSystem : ISystem
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



        foreach ((RefRW<MainCharacter> MainCharacteModule, RefRO<LocalTransform> CurrentTransform, RefRO<Unit> currentUnit, Entity selfEntity)
            in SystemAPI.Query<RefRW<MainCharacter>, RefRO<LocalTransform>, RefRO<Unit>>().WithEntityAccess()) 
        {
            var hits = new NativeList<DistanceHit>(Allocator.Temp);

            physicsWorld.OverlapSphere(
                CurrentTransform.ValueRO.Position,
                MainCharacteModule.ValueRO.Range,
                ref hits,
                CollisionFilter.Default);


            var uniqueEnemies = new NativeParallelHashSet<Entity>(math.max(8, hits.Length), Allocator.Temp);
            float counter = 0f;
            foreach (var h in hits)
            {
                Entity hitEntity = h.Entity;
                if (hitEntity == selfEntity)
                    continue;
                if (!SystemAPI.Exists(hitEntity))
                    continue;
                if (!SystemAPI.HasComponent<Unit>(hitEntity))
                    continue;

                Unit hitUnit = SystemAPI.GetComponent<Unit>(hitEntity);
                if (hitUnit.faction == currentUnit.ValueRO.faction)
                    continue;
                if (!uniqueEnemies.Add(hitEntity))
                    continue;

                counter += 1;
            }
            float boostPercent = counter * MainCharacteModule.ValueRO.FireRatePerscentBoost;
            MainCharacteModule.ValueRW.FireRateBoost = math.min(boostPercent, MainCharacteModule.ValueRO.MaxPercent);


            uniqueEnemies.Dispose();
            hits.Dispose();

        }    
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
