using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct AbilitySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var em = state.EntityManager;

        var hubQuery = SystemAPI.QueryBuilder().WithAll<EventHub>().Build();
        bool hasEventHub = !hubQuery.IsEmptyIgnoreFilter;

        DynamicBuffer<AbilityStartedEvent> startedEvents = default;
        DynamicBuffer<AbilityEndedEvent> endedEvents = default;

        if (hasEventHub)
        {
            var hubEntity = hubQuery.GetSingletonEntity();
            startedEvents = SystemAPI.GetBuffer<AbilityStartedEvent>(hubEntity);
            endedEvents = SystemAPI.GetBuffer<AbilityEndedEvent>(hubEntity);
        }

        foreach ((RefRW<Ability> ability, Entity ent) in SystemAPI.Query<RefRW<Ability>>().WithEntityAccess())
        {
            // cooldown tick
            if (!ability.ValueRO.Active && ability.ValueRO.CooldownLeft > 0)
            {
                ability.ValueRW.CooldownLeft -= dt;
                // CooldownEndEvent
                if (ability.ValueRO.CooldownLeft <= 0)
                {
                    ecb.AddComponent<CooldownEndEvent>(ent);
                }

            }

            if (!ability.ValueRO.Active && ability.ValueRO.CooldownLeft <= 0f && SystemAPI.HasComponent<ExtraBatteryModule>(ent))
            {
                var battery = SystemAPI.GetComponentRW<ExtraBatteryModule>(ent);
                if (battery.ValueRO.Charges < battery.ValueRO.MaxCharges)
                {
                    battery.ValueRW.Charges = battery.ValueRO.MaxCharges;
                }
            }

            // Activation
            bool canActivate = ability.ValueRO.CooldownLeft <= 0f;
            if (!canActivate && ability.ValueRO.IsTriggered && !ability.ValueRO.Active && SystemAPI.HasComponent<ExtraBatteryModule>(ent))
            {
                var battery = SystemAPI.GetComponentRW<ExtraBatteryModule>(ent);
                if (battery.ValueRO.Charges > 0)
                {
                    battery.ValueRW.Charges -= 1;
                    canActivate = true;
                }
            }

            if (ability.ValueRO.IsTriggered && !ability.ValueRO.Active && canActivate)
            {
                ecb.AddComponent<AbilityStartEvent>(ent);

                if (hasEventHub)
                {
                    Entity caster = ability.ValueRO.Owner != Entity.Null && em.Exists(ability.ValueRO.Owner)
                        ? ability.ValueRO.Owner
                        : ent;

                    float3 startPos = ResolveAbilityStartPosition(em, caster, ent);
                    float3 forward = ResolveAbilityForward(em, caster, ent);

                    startedEvents.Add(new AbilityStartedEvent
                    {
                        Caster = caster,
                        Type = ability.ValueRO.Type,
                        Start = startPos,
                        End = ResolveAbilityEndPosition(ability.ValueRO, startPos, forward),
                        Duration = ability.ValueRO.Duration
                    });
                }

                ability.ValueRW.Active = true;
                ability.ValueRW.TimeLeft = ability.ValueRO.Duration;
                ability.ValueRW.CooldownLeft = ability.ValueRO.Cooldown;
                ability.ValueRW.IsTriggered = false;
            }

            // End of ability
            if (ability.ValueRO.Active)
            {
                ability.ValueRW.TimeLeft -= dt;
                if (ability.ValueRO.TimeLeft <= 0)
                {
                    if (hasEventHub)
                    {
                        Entity caster = ability.ValueRO.Owner != Entity.Null && em.Exists(ability.ValueRO.Owner)
                            ? ability.ValueRO.Owner
                            : ent;

                        endedEvents.Add(new AbilityEndedEvent
                        {
                            Caster = caster,
                            Type = ability.ValueRO.Type,
                            Cooldown = ability.ValueRO.Cooldown
                        });
                    }

                    ability.ValueRW.Active = false;
                    ecb.AddComponent<AbilityEndEvent>(ent);
                }
            }
        }

        // применяем все структурные изменения после foreach
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static float3 ResolveAbilityStartPosition(EntityManager em, Entity caster, Entity fallback)
    {
        if (em.HasComponent<LocalTransform>(caster))
        {
            LocalTransform casterTransform = em.GetComponentData<LocalTransform>(caster);
            if (em.HasComponent<BulletSpawnPosition>(caster))
            {
                BulletSpawnPosition spawn = em.GetComponentData<BulletSpawnPosition>(caster);
                return casterTransform.TransformPoint(spawn.bulletSpawnLocalPosition);
            }

            return casterTransform.Position;
        }

        if (em.HasComponent<LocalTransform>(fallback))
            return em.GetComponentData<LocalTransform>(fallback).Position;

        return float3.zero;
    }

    private static float3 ResolveAbilityForward(EntityManager em, Entity caster, Entity fallback)
    {
        if (em.HasComponent<LocalTransform>(caster))
            return math.forward(em.GetComponentData<LocalTransform>(caster).Rotation);

        if (em.HasComponent<LocalTransform>(fallback))
            return math.forward(em.GetComponentData<LocalTransform>(fallback).Rotation);

        return new float3(0f, 0f, 1f);
    }

    private static float3 ResolveAbilityEndPosition(in Ability ability, float3 startPos, float3 fallbackForward)
    {
        if (ability.Type != AbilityType.Gauss)
            return ability.TargetPosition;

        float3 direction = ability.TargetPosition - startPos;
        direction.y = 0f;

        if (math.lengthsq(direction) < 0.0001f)
        {
            direction = fallbackForward;
            direction.y = 0f;
        }

        if (math.lengthsq(direction) < 0.0001f)
            direction = new float3(0f, 0f, 1f);

        float range = ability.Range > 0f ? ability.Range : math.length(direction);
        return startPos + math.normalize(direction) * range;
    }
}
