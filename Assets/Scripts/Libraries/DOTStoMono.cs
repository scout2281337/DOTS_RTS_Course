using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public static class DOTStoMono
{
    public static bool TryGetEntityManager(out EntityManager entityManager)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            entityManager = default;
            return false;
        }

        entityManager = world.EntityManager;
        return true;
    }

    public static bool TryGetEntityPosition(Entity entity, out Vector3 position)
    {
        position = default;

        if (!TryGetEntityManager(out var entityManager)
            || entity == Entity.Null || !entityManager.Exists(entity)) return false;

        if (entityManager.HasComponent<LocalTransform>(entity))
        {
            position = entityManager.GetComponentData<LocalTransform>(entity).Position;
            return true;
        }

        if (entityManager.HasComponent<LocalToWorld>(entity))
        {
            position = entityManager.GetComponentData<LocalToWorld>(entity).Position;
            return true;
        }

        return false;
    }

    public static List<Entity> GetSoldiersEntities()
    {
        if (!TryGetEntityManager(out var entityManager)) return new List<Entity>();

        var result = new List<Entity>();
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Unit>());
        using var entities = query.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            if (!entityManager.Exists(entity))
                continue;

            if (!entityManager.HasComponent<Unit>(entity))
                continue;

            var unit = entityManager.GetComponentData<Unit>(entity);
            if (unit.faction == Faction.Friendly)
                result.Add(entity);
        }

        return result;
    }
}
