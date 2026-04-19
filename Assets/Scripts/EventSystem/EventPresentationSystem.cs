using Unity.Entities;
using Unity.Burst;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]     // обязательно
[UpdateAfter(typeof(EndFixedStepSimulationEntityCommandBufferSystem))]  // добавь эту строку!
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
            AbilityEventListener.Instance?.InvokeBulletShot(e.From, e.To);
            // Debug.Log("BulletShot event processed");
        }
        bulletEvents.Clear();

        // ====================== DAMAGE EVENTS ======================
        var damageEvents = SystemAPI.GetBuffer<DamageEvent>(hubEntity);
        for (int i = 0; i < damageEvents.Length; i++)
        {
            var e = damageEvents[i];
            AbilityEventListener.Instance?.InvokeHealthChanged(e.TargetEntityClass, e.DamageAmount);

            // Оставил Debug.Log для отладки (можно закомментировать потом)
            //Debug.Log($"OnHealthChanged  Class: {e.TargetEntityClass} | Damage: {e.DamageAmount} | IsAbility: {e.IsAbilityDamage}");
        }
        damageEvents.Clear();

        // ====================== ABILITY STARTED EVENTS ======================
        var startEvents = SystemAPI.GetBuffer<AbilityStartedEvent>(hubEntity);
        for (int i = 0; i < startEvents.Length; i++)
        {
            var e = startEvents[i];
            AbilityEventListener.Instance?.InvokeAbilityStarted(e.Caster, e.Type);
            // Debug.Log("AbilityStarted event processed");
        }
        startEvents.Clear();

        // ====================== ABILITY ENDED EVENTS ======================
        var endEvents = SystemAPI.GetBuffer<AbilityEndedEvent>(hubEntity);
        for (int i = 0; i < endEvents.Length; i++)
        {
            var e = endEvents[i];
            AbilityEventListener.Instance?.InvokeAbilityEnded(e.Caster, e.Type);
            // Debug.Log("AbilityEnded event processed");
        }
        endEvents.Clear();
    }
}