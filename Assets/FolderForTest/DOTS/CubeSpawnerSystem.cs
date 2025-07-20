using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(CubeMoverSystem))]
partial struct CubeSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        EntityManager entityManager = state.EntityManager;
        var newSpawnQuery = SystemAPI.QueryBuilder().WithAll<NewSpawn>().Build();
        state.EntityManager.RemoveComponent<NewSpawn>(newSpawnQuery);
        foreach (var cubeSpawner in SystemAPI.Query<RefRW<CubeSpawnerComponent>>()) 
        {
            cubeSpawner.ValueRW.timer += SystemAPI.Time.DeltaTime;
            if (cubeSpawner.ValueRO.timer > cubeSpawner.ValueRO.timerMax) 
            {
                cubeSpawner.ValueRW.timer = 0f;
                Random random = cubeSpawner.ValueRO.random;
                
                
                
                /*NativeArray<Entity> cubeEntityArray = entityManager.Instantiate(cubeSpawner.ValueRO.cubePrefabEntity, cubeSpawner.ValueRO.amountToSpawn, Allocator.Temp);
                foreach (var cubeEntities in cubeEntityArray) 
                {
                    SystemAPI.SetComponent(cubeEntities, LocalTransform.FromPosition(random.NextInt(-30, 30), 30f, 0f));
                    cubeSpawner.ValueRW.random = random;
                }*/
                
                
                
                for (int i = 0; i < cubeSpawner.ValueRO.amountToSpawn; i++) 
                {


                    //через командбаффер тоже самое
                    Entity entity = entityCommandBuffer.Instantiate(cubeSpawner.ValueRO.cubePrefabEntity);
                    entityCommandBuffer.SetComponent(entity, LocalTransform.FromPosition(random.NextInt(-45, 45), 45, 0f));

                    //Через entityManager
                    //Entity cubeEntity = entityManager.Instantiate(cubeSpawner.ValueRO.cubePrefabEntity);
                    //SystemAPI.SetComponent(cubeEntity, LocalTransform.FromPosition(random.NextInt(-30, 30), 30f, 0f));
                    cubeSpawner.ValueRW.random = random;
                }
                
                //cubeSpawner.ValueRW.random = random;
            }
        } 
        
    }

}
