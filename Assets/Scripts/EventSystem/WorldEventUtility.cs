using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class WorldEventUtility
{
    public static bool Report(
        WorldEventType type,
        Vector3 position,
        WorldEventImportance importance = WorldEventImportance.Medium,
        WorldEventKnowledge knowledge = WorldEventKnowledge.Exact,
        float radius = 8f,
        float duration = 4f,
        string label = "")
    {
        return Report(new WorldEvent
        {
            Type = type,
            Importance = importance,
            Knowledge = knowledge,
            Position = new float3(position.x, position.y, position.z),
            Radius = math.max(0f, radius),
            Duration = math.max(0.1f, duration),
            Label = ToFixedLabel(label)
        });
    }

    public static bool Report(in WorldEvent worldEvent)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<EventHub>());

        try
        {
            if (query.CalculateEntityCount() == 0)
                return false;

            Entity hub = query.GetSingletonEntity();
            DynamicBuffer<WorldEvent> events = em.GetBuffer<WorldEvent>(hub);
            events.Add(worldEvent);
            return true;
        }
        finally
        {
            query.Dispose();
        }
    }

    public static FixedString64Bytes ToFixedLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return default;

        FixedString64Bytes result = default;
        result.Append(label);
        return result;
    }
}
