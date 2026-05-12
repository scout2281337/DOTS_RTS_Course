using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial class ZombieSpawnPointApplySystem : SystemBase
{
    private int _appliedVersion;

    protected override void OnCreate()
    {
        RequireForUpdate<ZombieSpawner>();
    }

    protected override void OnUpdate()
    {
        int version = ZombieSpawnPointEvents.Version;
        if (version == _appliedVersion || !ZombieSpawnPointEvents.HasPoints)
            return;

        IReadOnlyList<Vector3> points = ZombieSpawnPointEvents.GetLatestPoints();

        EntityQuery query = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<ZombieSpawner>(),
            ComponentType.ReadWrite<SpawnPointElement>());

        NativeArray<Entity> spawners = query.ToEntityArray(Allocator.Temp);
        int spawnerCount = spawners.Length;

        try
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                Entity spawnerEntity = spawners[i];
                DynamicBuffer<SpawnPointElement> spawnPointBuffer = EntityManager.GetBuffer<SpawnPointElement>(spawnerEntity);

                spawnPointBuffer.Clear();
                for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
                {
                    Vector3 point = points[pointIndex];
                    spawnPointBuffer.Add(new SpawnPointElement
                    {
                        position = new float3(point.x, point.y, point.z)
                    });
                }

                ZombieSpawner spawner = EntityManager.GetComponentData<ZombieSpawner>(spawnerEntity);
                Vector3 firstPoint = points[0];
                spawner.zombieSpawnPosition = new float3(firstPoint.x, firstPoint.y, firstPoint.z);
                EntityManager.SetComponentData(spawnerEntity, spawner);
            }
        }
        finally
        {
            spawners.Dispose();
        }

        _appliedVersion = version;
        Debug.Log($"Applied {points.Count} scene zombie spawn points to {spawnerCount} ZombieSpawner entities.");
    }
}
