using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct CubeMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        EntityManager entityManager = state.EntityManager;
        foreach ((
            RefRO<Cube> cube, 
            RefRW<LocalTransform> localTransform,
            Entity entity) 
            in SystemAPI.Query<
                RefRO<Cube>, 
                RefRW<LocalTransform>>().WithEntityAccess()) 
        {
            localTransform.ValueRW.Position += new float3(0f, -9f, 0f) * SystemAPI.Time.DeltaTime;
            if (localTransform.ValueRO.Position.y < 0) 
            {
                entityCommandBuffer.DestroyEntity(entity);
            }
        }  
    }


}
