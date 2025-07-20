using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static UnityEngine.EventSystems.EventTrigger;

partial struct CubeMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecbPW = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        EntityManager entityManager = state.EntityManager;


        new CubeMoverJob
        {
            ecbPWJob = ecbPW,
            deltaTime = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel();
        
        //Однопоточно
        /*foreach ((
            RefRO<Cube> cube, 
            RefRW<LocalTransform> localTransform,
            Entity entity) 
            in SystemAPI.Query<
                RefRO<Cube>, 
                RefRW<LocalTransform>>().WithNone<NewSpawn>().WithEntityAccess()) 
        {
            localTransform.ValueRW.Position += new float3(0f, -9f, 0f) * SystemAPI.Time.DeltaTime;
            if (localTransform.ValueRO.Position.y < 0) 
            {
                entityCommandBuffer.DestroyEntity(entity);
            }
        } */
    }


}


//C системой Jobs
[BurstCompile]
public partial struct CubeMoverJob : IJobEntity 
{
    public EntityCommandBuffer.ParallelWriter ecbPWJob;
    public float deltaTime;

    public void Execute([ChunkIndexInQuery] int chunkIndex, in Cube cube, ref LocalTransform localTransform, Entity entity) 
    {
        localTransform.Position += new float3(0f, -9f, 0f) * deltaTime;
        if (localTransform.Position.y < 0)
        {
            ecbPWJob.DestroyEntity(chunkIndex, entity);
        }
    }
}