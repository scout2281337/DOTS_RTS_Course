using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct CubeSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = state.EntityManager;
        foreach (var cubeSpawner in SystemAPI.Query<RefRW<CubeSpawnerComponent>>()) 
        {
            cubeSpawner.ValueRW.timer += SystemAPI.Time.DeltaTime;
            if (cubeSpawner.ValueRO.timer > cubeSpawner.ValueRO.timerMax) 
            {
                Random random = cubeSpawner.ValueRO.random;
                for (int i = 0; i < cubeSpawner.ValueRO.amountToSpawn; i++) 
                {
                    Entity cubeEntity = entityManager.Instantiate(cubeSpawner.ValueRO.cubePrefabEntity);
                    SystemAPI.SetComponent(cubeEntity, LocalTransform.FromPosition(random.NextInt(-30, 30), 30f, 0f));
                
                }
                
                cubeSpawner.ValueRW.random = random;
                cubeSpawner.ValueRW.timer = 0f;
            }
        } 
        
    }

}
