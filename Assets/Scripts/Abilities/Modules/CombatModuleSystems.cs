using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct CombatModulesTickSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var ricochet in SystemAPI.Query<RefRW<RicochetModule>>())
        {
            if (ricochet.ValueRO.CooldownLeft > 0f)
            {
                ricochet.ValueRW.CooldownLeft = math.max(0f, ricochet.ValueRO.CooldownLeft - dt);
            }
        }

        foreach (var bloody in SystemAPI.Query<RefRW<BloodySpeedUpModule>>())
        {
            if (bloody.ValueRO.Stacks <= 0)
                continue;

            bloody.ValueRW.ResetTimer -= dt;
            if (bloody.ValueRO.ResetTimer <= 0f)
            {
                bloody.ValueRW.Stacks = 0;
                bloody.ValueRW.ResetTimer = bloody.ValueRO.ResetTimerMax;
            }
        }

        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRW<StunEffect> stun, Entity entity) in SystemAPI.Query<RefRW<StunEffect>>().WithEntityAccess())
        {
            stun.ValueRW.TimeLeft -= dt;
            if (stun.ValueRO.TimeLeft <= 0f)
            {
                ecb.RemoveComponent<StunEffect>(entity);
            }
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DamageSystem))]
partial struct ModuleOnKillSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<EventHub>(out var hub))
            return;

        var killEvents = SystemAPI.GetBuffer<KillEvent>(hub);
        if (killEvents.Length == 0)
            return;

        var damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hub);

        for (int i = 0; i < killEvents.Length; i++)
        {
            var kill = killEvents[i];
            if (!SystemAPI.Exists(kill.Killer))
                continue;

            Entity killer = kill.Killer;

            if (SystemAPI.HasComponent<EnergyVampireModule>(killer) && SystemAPI.HasComponent<Ability>(killer))
            {
                var vampire = SystemAPI.GetComponentRO<EnergyVampireModule>(killer);
                var ability = SystemAPI.GetComponentRW<Ability>(killer);
                ability.ValueRW.CooldownLeft = math.max(0f, ability.ValueRO.CooldownLeft - vampire.ValueRO.CooldownReductionOnKill);
            }

            if (SystemAPI.HasComponent<VampirismModule>(killer) &&
                SystemAPI.HasComponent<Health>(killer) &&
                SystemAPI.HasComponent<LocalTransform>(killer) &&
                SystemAPI.HasComponent<LocalTransform>(kill.Victim))
            {
                var vampirism = SystemAPI.GetComponentRO<VampirismModule>(killer);
                float distSq = math.distancesq(
                    SystemAPI.GetComponent<LocalTransform>(killer).Position,
                    SystemAPI.GetComponent<LocalTransform>(kill.Victim).Position);

                if (distSq <= vampirism.ValueRO.Radius * vampirism.ValueRO.Radius)
                {
                    var health = SystemAPI.GetComponentRW<Health>(killer);
                    health.ValueRW.healthAmount = math.min(
                        health.ValueRO.healthAmountMax,
                        health.ValueRO.healthAmount + vampirism.ValueRO.HealAmount);
                    health.ValueRW.OnHealthChanged = true;
                }
            }

            if (SystemAPI.HasComponent<BloodySpeedUpModule>(killer))
            {
                var bloody = SystemAPI.GetComponentRW<BloodySpeedUpModule>(killer);
                bloody.ValueRW.Stacks = math.min(bloody.ValueRO.MaxStacks, bloody.ValueRO.Stacks + 1);
                bloody.ValueRW.ResetTimer = bloody.ValueRO.ResetTimerMax;
            }

            if (!SystemAPI.HasComponent<RicochetModule>(killer) || !SystemAPI.HasComponent<Unit>(killer))
                continue;

            var ricochet = SystemAPI.GetComponentRW<RicochetModule>(killer);
            if (ricochet.ValueRO.CooldownLeft > 0f)
                continue;

            float3 center = SystemAPI.HasComponent<LocalTransform>(kill.Victim)
                ? SystemAPI.GetComponent<LocalTransform>(kill.Victim).Position
                : SystemAPI.GetComponent<LocalTransform>(killer).Position;

            Faction killerFaction = SystemAPI.GetComponent<Unit>(killer).faction;
            float radiusSq = ricochet.ValueRO.Radius * ricochet.ValueRO.Radius;
            Entity nearestTarget = Entity.Null;
            float nearestDistSq = float.MaxValue;

            foreach ((RefRO<Unit> unit, RefRO<LocalTransform> transform, Entity target) in
                     SystemAPI.Query<RefRO<Unit>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (target == killer || target == kill.Victim)
                    continue;
                if (unit.ValueRO.faction == killerFaction)
                    continue;
                if (!SystemAPI.HasComponent<Health>(target))
                    continue;
                if (SystemAPI.GetComponent<Health>(target).healthAmount <= 0f)
                    continue;

                float distSq = math.distancesq(center, transform.ValueRO.Position);
                if (distSq > radiusSq || distSq >= nearestDistSq)
                    continue;

                nearestDistSq = distSq;
                nearestTarget = target;
            }

            if (nearestTarget == Entity.Null)
                continue;

            float ricochetDamage = math.max(1f, kill.DamageDealt * ricochet.ValueRO.DamageMultiplier);
            Unit ricochetTargetUnit = SystemAPI.GetComponent<Unit>(nearestTarget);
            damageBuffer.Add(new DamageEvent
            {
                SourceEntity = killer,
                TargetEntity = nearestTarget,
                TargetEntityClass = ricochetTargetUnit.Class,
                DamageAmount = ricochetDamage
            });

            ricochet.ValueRW.CooldownLeft = ricochet.ValueRO.Cooldown;
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ModuleOnKillSystem))]
partial struct UnitDeathConsoleEventSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<EventHub>(out var hub))
            return;

        var killEvents = SystemAPI.GetBuffer<KillEvent>(hub);
        if (killEvents.Length == 0)
            return;

        var consoleEvents = SystemAPI.GetBuffer<UnitDeathConsoleEvent>(hub);
        for (int i = 0; i < killEvents.Length; i++)
        {
            var kill = killEvents[i];
            var consoleEvent = new UnitDeathConsoleEvent();

            if (SystemAPI.Exists(kill.Victim) && SystemAPI.HasComponent<Unit>(kill.Victim))
            {
                Unit victim = SystemAPI.GetComponent<Unit>(kill.Victim);
                consoleEvent.VictimClass = victim.Class;
                consoleEvent.VictimFaction = victim.faction;
                consoleEvent.HasVictimUnit = true;
            }

            if (SystemAPI.Exists(kill.Killer) && SystemAPI.HasComponent<Unit>(kill.Killer))
            {
                Unit killer = SystemAPI.GetComponent<Unit>(kill.Killer);
                consoleEvent.KillerClass = killer.Class;
                consoleEvent.KillerFaction = killer.faction;
                consoleEvent.HasKillerUnit = true;
            }

            consoleEvents.Add(consoleEvent);
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
partial struct ModuleEventsCleanupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<EventHub>(out var hub))
            return;

        SystemAPI.GetBuffer<KillEvent>(hub).Clear();
    }
}
