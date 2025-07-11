using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ZombieSpawnerSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        foreach ((RefRW<ZombieSpawner> zombieSpawner, RefRO<LocalTransform> localTransform)  in SystemAPI.Query<RefRW<ZombieSpawner>, RefRO<LocalTransform>>()) 
        {
            zombieSpawner.ValueRW.timer -= SystemAPI.Time.DeltaTime; ;
            if (zombieSpawner.ValueRO.timer > 0f) 
            {
                continue;
            }
            zombieSpawner.ValueRW.timer = zombieSpawner.ValueRO.timerMax; 
            
            Entity zombieEntity = state.EntityManager.Instantiate(entitiesReferences.zombiePrefabEntity);
            //float3 zombieSpawnWorldPosition = localTransform.ValueRO.TransformPoint(localTransform.ValueRO.Position);
            SystemAPI.SetComponent(zombieEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));
        }  
    }
}
