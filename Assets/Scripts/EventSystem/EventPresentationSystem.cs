using Unity.Entities;
using Unity.Burst;

[UpdateInGroup(typeof(PresentationSystemGroup))]

public partial struct EventPresentationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var hubEntity = SystemAPI
            .QueryBuilder()
            .WithAll<EventHub>()
            .Build()
            .GetSingletonEntity();

        // ====================== BULLET SHOT EVENTS ======================
        var bulletEvents = SystemAPI.GetBuffer<BulletShotEvent>(hubEntity);
        for (int i = 0; i < bulletEvents.Length; i++)
        {
            var e = bulletEvents[i];
            EventMediator.Instance?.InvokeBulletShot(e);
            // Debug.Log("BulletShot event processed");
        }
        bulletEvents.Clear();

        // ====================== DAMAGE EVENTS ======================
        var damageEvents = SystemAPI.GetBuffer<DamageEvent>(hubEntity);
        for (int i = 0; i < damageEvents.Length; i++)
        {
            var e = damageEvents[i];
            EventMediator.Instance?.InvokeDamageReceived(e);

            // Оставил Debug.Log для отладки (можно закомментировать потом)
            //Debug.Log($"OnHealthChanged  Class: {e.TargetEntityClass} | Damage: {e.DamageAmount} | IsAbility: {e.IsAbilityDamage}");
        }
        damageEvents.Clear();

        // ====================== ABILITY STARTED EVENTS ======================
        var startEvents = SystemAPI.GetBuffer<AbilityStartedEvent>(hubEntity);
        for (int i = 0; i < startEvents.Length; i++)
        {
            var e = startEvents[i];
            EventMediator.Instance?.InvokeAbilityStarted(e);
            // Debug.Log("AbilityStarted event processed");
        }
        startEvents.Clear();

        // ====================== ABILITY ENDED EVENTS ======================
        var endEvents = SystemAPI.GetBuffer<AbilityEndedEvent>(hubEntity);
        for (int i = 0; i < endEvents.Length; i++)
        {
            var e = endEvents[i];
            EventMediator.Instance?.InvokeAbilityEnded(e);
            // Debug.Log("AbilityEnded event processed");
        }
        endEvents.Clear();
    }
}