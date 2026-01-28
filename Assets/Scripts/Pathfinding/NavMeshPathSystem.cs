using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct NavMeshPathSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (
            transform,
            move,
            entity
        ) in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRO<MoveOverride>>()
            .WithAll<MoveOverride>()
            .WithNone<NavPathProgress>()
            .WithEntityAccess())
        {
            // если уже есть прогресс Ч путь уже построен
            if (state.EntityManager.HasComponent<NavPathProgress>(entity))
                continue;

            if (math.distancesq(transform.ValueRO.Position, move.ValueRO.targetPosition) < 0.01f)
                continue;

            var navPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(
                    transform.ValueRO.Position,
                    move.ValueRO.targetPosition,
                    NavMesh.AllAreas,
                    navPath))
                continue;

            if (navPath.corners.Length == 0)
                continue;

            // гарантируем, что буфер есть
            if (!state.EntityManager.HasBuffer<NavPathPoint>(entity))
                ecb.AddBuffer<NavPathPoint>(entity);

            var buffer = ecb.SetBuffer<NavPathPoint>(entity);
            buffer.Clear();

            foreach (var corner in navPath.corners)
            {
                buffer.Add(new NavPathPoint
                {
                    Value = (float3)corner
                });
            }

            ecb.AddComponent(entity, new NavPathProgress
            {
                CurrentIndex = 0
            });

            Debug.Log($"NavPath built for {entity}, points: {navPath.corners.Length}");
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
