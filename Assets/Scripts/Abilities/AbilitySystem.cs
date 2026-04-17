using Unity.Entities;

partial struct AbilitySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);


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
