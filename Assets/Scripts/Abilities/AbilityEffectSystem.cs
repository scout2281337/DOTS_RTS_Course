using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AbilitySystem))]

partial struct AbilityEffectSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences))
            return;

        var em = state.EntityManager;
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityStartEvent>().WithEntityAccess())
        {
            Entity owner = ResolveOwner(em, ability.ValueRO, ent);

            switch (ability.ValueRO.Type)
            {
                case AbilityType.Stim:
                    ApplySpeedBoost(ref state, em, ecb, owner, ability.ValueRO.TargetType, ability.ValueRO.Power, ability.ValueRO.Duration);
                    break;
                case AbilityType.Barricade:
                    SpawnObject(entitiesReferences.AntiGravitationBarrier, ref state);
                    break;
                case AbilityType.Scorcher:
                    float3 pos = ability.ValueRO.TargetPosition;

                    var fireball = em.Instantiate(entitiesReferences.FireballPrefabEntity);
                    var fireballData = em.GetComponentData<Fireball>(fireball);
                    fireballData.Owner = owner;
                    em.SetComponentData(fireball, fireballData);

                    em.SetComponentData(fireball, new LocalTransform
                    {
                        Position = pos,
                        Rotation = quaternion.identity,
                        Scale = 1
                    });
                    break;
                case AbilityType.Gauss:
                    if (SystemAPI.HasComponent<ShootAttack>(owner))
                    {
                        var shootAttack = SystemAPI.GetComponentRW<ShootAttack>(owner);
                        shootAttack.ValueRW.attackMode = AttackMode.Charged;
                    }
                    break;
                case AbilityType.None:
                    break;
            }

            var evt = new AbilityStartedEvent{
                Caster = owner,
                Type = ability.ValueRO.Type};
            EventMediator.Instance?.InvokeAbilityStarted(evt);
            ecb.RemoveComponent<AbilityStartEvent>(ent);
        }

        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityEndEvent>().WithEntityAccess())
        {
            Entity owner = ResolveOwner(em, ability.ValueRO, ent);

            switch (ability.ValueRO.Type)
            {
                case AbilityType.Stim:
                    //EndSpeedBoost(ref state, em, owner, ability.ValueRO.TargetType);
                    break;
                case AbilityType.Barricade:
                    break;
                case AbilityType.Scorcher:
                    break;
                case AbilityType.Gauss:
                    if (SystemAPI.HasComponent<ShootAttack>(owner))
                    {
                        var shootAttack = SystemAPI.GetComponentRW<ShootAttack>(owner);
                        shootAttack.ValueRW.attackMode = AttackMode.Normal;
                    }
                    break;
                case AbilityType.None:
                    break;
            }
            var evt = new AbilityEndedEvent {
                Caster = owner,
                Type = ability.ValueRO.Type};
            EventMediator.Instance?.InvokeAbilityEnded(evt);
            ecb.RemoveComponent<AbilityEndEvent>(ent);
        }

        foreach ((RefRW<Ability> ability, Entity ent) in SystemAPI.Query<RefRW<Ability>>().WithAll<CooldownEndEvent>().WithEntityAccess())
        {
            Entity owner = ResolveOwner(em, ability.ValueRO, ent);
            var evt = new AbilityCooldownEndedEvent {
                Caster = owner,
                Type = ability.ValueRO.Type};
            EventMediator.Instance?.InvokeCooldownEnded(evt);
            ecb.RemoveComponent<CooldownEndEvent>(ent);
        }

        //ecb.Playback(state.EntityManager);
        //ecb.Dispose();
    }

    private static Entity ResolveOwner(EntityManager em, in Ability ability, Entity fallback)
    {
        return ability.Owner != Entity.Null && em.Exists(ability.Owner)
            ? ability.Owner
            : fallback;
    }

    void ApplySpeedBoost(ref SystemState state, EntityManager em, EntityCommandBuffer ecb, Entity owner, AbilityTargetType target, float multiplier, float Timer)
    {
        switch (target)
        {
            case AbilityTargetType.Self:
                if (em.HasComponent<UnitMover>(owner))
                {
                    DynamicBuffer<SlowDebuff> buffer;
                    if (SystemAPI.HasBuffer<SlowDebuff>(owner))
                    {
                        buffer = SystemAPI.GetBuffer<SlowDebuff>(owner);
                    }
                    else
                    {
                        buffer = ecb.AddBuffer<SlowDebuff>(owner);
                    }

                    buffer.Add(new SlowDebuff
                    {
                        Multiplier = multiplier,
                        Source = owner,
                        Timer = Timer
                    });
                    Debug.Log("Добавляем буфер успешно!  self");
                }
                break;
            case AbilityTargetType.Ally:
                foreach ((RefRW<UnitMover> allyMover, RefRO<Unit> friendlyUnit, Entity friendlyEntity) in
                         SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>().WithEntityAccess())
                {
                    if (friendlyUnit.ValueRO.faction == SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction && em.HasComponent<UnitMover>(friendlyEntity))
                    {
                        DynamicBuffer<SlowDebuff> buffer;
                        if (SystemAPI.HasBuffer<SlowDebuff>(friendlyEntity))
                        {
                            buffer = SystemAPI.GetBuffer<SlowDebuff>(friendlyEntity);
                        }
                        else
                        {
                            buffer = ecb.AddBuffer<SlowDebuff>(friendlyEntity);
                        }

                        buffer.Add(new SlowDebuff
                        {
                            Multiplier = multiplier,
                            Source = owner,
                            Timer = Timer
                        });
                        Debug.Log("Добавляем буфер успешно! ally");
                    }
                }
                break;
            case AbilityTargetType.Enemy:
                break;
            case AbilityTargetType.Area:
                break;
        }
    }

    void EndSpeedBoost(ref SystemState state, EntityManager em, Entity owner, AbilityTargetType target)
    {
        switch (target)
        {
            case AbilityTargetType.Self:
                if (em.HasComponent<UnitMover>(owner))
                {
                    if (!SystemAPI.HasBuffer<SlowDebuff>(owner))
                        return;

                    var buffer = SystemAPI.GetBuffer<SlowDebuff>(owner);

                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        if (buffer[i].Source == owner)
                        {
                            buffer.RemoveAt(i);
                        }
                    }
                }
                break;
            case AbilityTargetType.Ally:
                foreach ((RefRW<UnitMover> allyMover, RefRO<Unit> friendlyUnit, Entity friendlyEntity) in
                         SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>().WithEntityAccess())
                {
                    if (!SystemAPI.HasBuffer<SlowDebuff>(friendlyEntity))
                        return;

                    var buffer = SystemAPI.GetBuffer<SlowDebuff>(friendlyEntity);

                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        if (buffer[i].Source == owner)
                        {
                            buffer.RemoveAt(i);
                        }
                    }
                }
                break;
            case AbilityTargetType.Enemy:
                foreach ((RefRW<UnitMover> enemyMover, RefRO<Unit> friendlyUnit) in
                         SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>())
                {
                    if (friendlyUnit.ValueRO.faction != SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction)
                        enemyMover.ValueRW.CurrentMoveSpeed = enemyMover.ValueRO.BaseSpeed;
                }
                break;
            case AbilityTargetType.Area:
                break;
        }
    }

    void SpawnObject(Entity entityToSpawn, ref SystemState state)
    {
        state.EntityManager.Instantiate(entityToSpawn);
    }
}
