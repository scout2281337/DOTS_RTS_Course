using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct DamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;

        if (!SystemAPI.TryGetSingletonEntity<EventHub>(out var hub))
            return;

        var damageEvents = SystemAPI.GetBuffer<DamageEvent>(hub);
        var killEvents = SystemAPI.GetBuffer<KillEvent>(hub);

        for (int i = 0; i < damageEvents.Length; i++)
        {
            var dmg = damageEvents[i];

            if (!em.HasComponent<Health>(dmg.TargetEntity))
                continue;

            if (SystemAPI.HasComponent<Invulnerable>(dmg.TargetEntity))
                continue;

            float damage = dmg.DamageAmount;

            var healthRO = SystemAPI.GetComponentRO<Health>(dmg.TargetEntity);
            float healthBefore = healthRO.ValueRO.healthAmount;

            // armor
            if (healthRO.ValueRO.armor > 0f)
            {
                damage *= 1f - math.saturate(healthRO.ValueRO.armor / 100f);
            }

            // модуль двойной панцирь
            if (em.HasComponent<DoubleShell>(dmg.TargetEntity) && damage / healthRO.ValueRO.healthAmountMax > 0.15f)
            {
                damage *= 1f - math.saturate(healthRO.ValueRO.armor / 100f);
            }

            if (em.HasComponent<AcidBulletsDebuff>(dmg.TargetEntity))
            {
                var acidDebuff = em.GetComponentData<AcidBulletsDebuff>(dmg.TargetEntity);
                damage *= 1f + acidDebuff.Stacks * acidDebuff.DamageTakenPerStack;
            }

            var healthRW = SystemAPI.GetComponentRW<Health>(dmg.TargetEntity);
            healthRW.ValueRW.healthAmount -= damage;
            healthRW.ValueRW.OnHealthChanged = true;

            if (dmg.IsAbilityDamage && SystemAPI.Exists(dmg.SourceEntity) && em.HasComponent<DeafSoundModule>(dmg.SourceEntity))
            {
                float stunDuration = em.GetComponentData<DeafSoundModule>(dmg.SourceEntity).StunDuration;
                if (em.HasComponent<StunEffect>(dmg.TargetEntity))
                {
                    var stun = em.GetComponentData<StunEffect>(dmg.TargetEntity);
                    stun.TimeLeft = math.max(stun.TimeLeft, stunDuration);
                    em.SetComponentData(dmg.TargetEntity, stun);
                }
                else
                {
                    em.AddComponentData(dmg.TargetEntity, new StunEffect
                    {
                        TimeLeft = stunDuration
                    });
                }
            }

            if (SystemAPI.Exists(dmg.SourceEntity) && em.HasComponent<AcidBulletsModule>(dmg.SourceEntity) && em.HasComponent<Health>(dmg.TargetEntity))
            {
                if (em.HasComponent<AcidBulletsDebuff>(dmg.TargetEntity))
                {
                    var acidDebuff = em.GetComponentData<AcidBulletsDebuff>(dmg.TargetEntity);
                    acidDebuff.Stacks = math.min(acidDebuff.MaxStacks, acidDebuff.Stacks + 1);
                    em.SetComponentData(dmg.TargetEntity, acidDebuff);
                }
                else
                {
                    var module = em.GetComponentData<AcidBulletsModule>(dmg.SourceEntity);
                    em.AddComponentData(dmg.TargetEntity, new AcidBulletsDebuff
                    {
                        Stacks = 1,
                        MaxStacks = module.MaxStacks,
                        MoveSlowPerStack = module.MoveSlowPerStack,
                        DamageTakenPerStack = module.DamageTakenPerStack
                    });
                }
            }

            float healthAfter = healthRW.ValueRO.healthAmount;
            if (healthBefore > 0f && healthAfter <= 0f && SystemAPI.Exists(dmg.SourceEntity))
            {
                killEvents.Add(new KillEvent
                {
                    Killer = dmg.SourceEntity,
                    Victim = dmg.TargetEntity,
                    DamageDealt = damage
                });
            }
        }
    }
}
