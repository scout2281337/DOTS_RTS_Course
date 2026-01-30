using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct EventPresentationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var hub = SystemAPI
            .QueryBuilder()
            .WithAll<EventHub>()
            .Build()
            .GetSingletonEntity();

        // BULLET
        var bulletEvents = SystemAPI.GetBuffer<BulletShotEvent>(hub);
        foreach (var e in bulletEvents)
        {
            AbilityEventListener.Instance?.RaiseBulletShot(e.From, e.To);
            //Debug.Log("worked 1");
        }
        bulletEvents.Clear();
        var HealthChangedEvents = SystemAPI.GetBuffer<DamageEvent>(hub);
        foreach (var e in HealthChangedEvents)
        {
            AbilityEventListener.Instance?.RaiseOnHealthChanged(e.TargetEntityClass, e.DamageAmount);
            Debug.Log("worked OnHealthChanged " + e.TargetEntityClass + " " + e.DamageAmount);
        }
        HealthChangedEvents.Clear();
        // ABILITY START
        var startEvents = SystemAPI.GetBuffer<AbilityStartedEvent>(hub);
        foreach (var e in startEvents)
        {
            AbilityEventListener.Instance?.RaiseAbilityStarted(e.Caster, e.Type);
            //Debug.Log("worked 2");
        }
        startEvents.Clear();

        // ABILITY END
        var endEvents = SystemAPI.GetBuffer<AbilityEndedEvent>(hub);
        foreach (var e in endEvents)
        {
            AbilityEventListener.Instance?.RaiseAbilityEnded(e.Caster, e.Type);
            //Debug.Log("worked 3");
        }
        endEvents.Clear();
    }
}
