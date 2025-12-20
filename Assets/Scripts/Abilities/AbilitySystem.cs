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



            // Activation
            if (ability.ValueRO.IsTriggered && !ability.ValueRO.Active && ability.ValueRO.CooldownLeft <= 0)
            {
                ecb.AddComponent<AbilityStartEvent>(ent);
                ability.ValueRW.Active = true;
                ability.ValueRW.TimeLeft = 7;
                ability.ValueRW.CooldownLeft = 12;
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
