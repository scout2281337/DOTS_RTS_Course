using Unity.Entities;
using Unity.VisualScripting;

public partial struct EventHubInitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var hub = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<EventHub>(hub);

        state.EntityManager.AddBuffer<BulletShotEvent>(hub);
        state.EntityManager.AddBuffer<AbilityStartedEvent>(hub);
        state.EntityManager.AddBuffer<AbilityEndedEvent>(hub);
        state.EntityManager.AddBuffer<DamageEvent>(hub);
        state.EntityManager.AddBuffer<KillEvent>(hub);
        state.EntityManager.AddBuffer<UnitDeathConsoleEvent>(hub);
        state.EntityManager.AddBuffer<RicochetConsoleEvent>(hub);
    }
}
