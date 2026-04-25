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

                    float3 startPos = float3.zero;
                    if (em.HasComponent<LocalTransform>(caster))
                    {
                        startPos = em.GetComponentData<LocalTransform>(caster).Position;
                    }
                    else if (em.HasComponent<LocalTransform>(ent))
                    {
                        startPos = em.GetComponentData<LocalTransform>(ent).Position;
                    }

                    startedEvents.Add(new AbilityStartedEvent
                    {
                        Caster = caster,
                        Type = ability.ValueRO.Type,
                        Start = startPos,
                        End = ability.ValueRO.TargetPosition,
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
}
