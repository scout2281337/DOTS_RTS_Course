using Unity.Entities;

public partial struct EventHubInitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var hub = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<EventHub>(hub);

        state.EntityManager.AddBuffer<BulletShotEvent>(hub);
        state.EntityManager.AddBuffer<AbilityPointerEvent>(hub);
        state.EntityManager.AddBuffer<AbilityPointerEndedEvent>(hub);
        state.EntityManager.AddBuffer<AbilityStartedEvent>(hub);
        state.EntityManager.AddBuffer<AbilityEndedEvent>(hub);
        state.EntityManager.AddBuffer<AbilityCooldownEndedEvent>(hub);
        state.EntityManager.AddBuffer<DamageEvent>(hub);
        state.EntityManager.AddBuffer<UnitDeathEvent>(hub);
        state.EntityManager.AddBuffer<KillEvent>(hub);
        state.EntityManager.AddBuffer<UnitDeathConsoleEvent>(hub);
        state.EntityManager.AddBuffer<RicochetConsoleEvent>(hub);
    }
}
