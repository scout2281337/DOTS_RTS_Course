using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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



        foreach ((RefRW<MainCharacter> MainCharacteModule, RefRO<LocalTransform> CurrentTransform)
            in SystemAPI.Query<RefRW<MainCharacter>, RefRO<LocalTransform>>()) 
        {
            var hits = new NativeList<DistanceHit>(Allocator.Temp);

            physicsWorld.OverlapSphere(
                CurrentTransform.ValueRO.Position,
                MainCharacteModule.ValueRO.Range,
                ref hits,
                CollisionFilter.Default);


            float counter = 0;
            foreach (var h in hits)
            {
                counter += 1;
            }
            MainCharacteModule.ValueRW.FireRateBoost = counter * MainCharacteModule.ValueRO.FireRatePerscentBoost;
            if (MainCharacteModule.ValueRO.FireRateBoost > MainCharacteModule.ValueRO.MaxPercent) 
            {
                MainCharacteModule.ValueRW.FireRateBoost = MainCharacteModule.ValueRO.MaxPercent;
            }


            hits.Dispose();

        }    
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
