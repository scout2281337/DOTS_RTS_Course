using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
partial struct NavPathFollowSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (
            mover,
            transform,
            path,
            progress
        ) in SystemAPI.Query<
            RefRW<UnitMover>,
            RefRO<LocalTransform>,
            DynamicBuffer<NavPathPoint>,
            RefRW<NavPathProgress>>())
        {
            if (path.Length == 0)
                continue;

            int index = progress.ValueRO.CurrentIndex;
            if (index >= path.Length)
                continue;

            float3 target = path[index].Value;
            mover.ValueRW.targetPosition = target;

            float distSq = math.distancesq(transform.ValueRO.Position, target);

            // дошли до текущей точки идем к следующей
            if (distSq < 0.2f * 0.2f)
            {
                progress.ValueRW.CurrentIndex++;
            }
        }
    }
}
