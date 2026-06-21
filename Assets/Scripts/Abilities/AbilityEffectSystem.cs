using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
        bool hasEventHub = SystemAPI.TryGetSingletonEntity<EventHub>(out Entity hubEntity);
        DynamicBuffer<DamageEvent> damageBuffer = default;
        if (hasEventHub)
        {
            damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hubEntity);
        }

        bool hasPhysicsWorld = SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out PhysicsWorldSingleton physicsWorldSingleton);

        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityStartEvent>().WithEntityAccess())
        {
            Entity owner = ResolveOwner(em, ability.ValueRO, ent);
            if (IsAbilityCasterDead(em, owner, ent))
            {
                ecb.RemoveComponent<AbilityStartEvent>(ent);
                continue;
            }

            switch (ability.ValueRO.Type)
            {
                case AbilityType.Stim:
                    ApplySpeedBoost(ref state, em, ecb, owner, ability.ValueRO.TargetType, ability.ValueRO.Power, ability.ValueRO.Duration);
                    break;
                case AbilityType.Barricade:
                    float3 barricadePos = ability.ValueRO.TargetPosition;
                    quaternion barricadeRot = quaternion.identity;

                    if (em.HasComponent<LocalTransform>(owner))
                    {
                        var ownerTransform = em.GetComponentData<LocalTransform>(owner);
                        barricadeRot = ownerTransform.Rotation;

                        // Fallback for self-cast scenarios where target wasn't set explicitly.
                        if (math.lengthsq(barricadePos) < 0.0001f)
                        {
                            barricadePos = ownerTransform.Position + math.forward(ownerTransform.Rotation) * 2.5f;
                        }
                    }

                    var field = em.Instantiate(entitiesReferences.AntiGravitationBarrier);
                    if (em.HasComponent<AGB>(field))
                    {
                        AGB agb = em.GetComponentData<AGB>(field);
                        agb.Range = ResolveEffectRadius(ability.ValueRO, agb.Range);
                        agb.Duration = ability.ValueRO.Duration > 0f ? ability.ValueRO.Duration : agb.Duration;
                        agb.SpeedDebuff = ability.ValueRO.Power != 0f ? ability.ValueRO.Power : agb.SpeedDebuff;
                        em.SetComponentData(field, agb);
                    }

                    if (em.HasComponent<LocalTransform>(field))
                    {
                        em.SetComponentData(field, new LocalTransform
                        {
                            Position = barricadePos,
                            Rotation = barricadeRot,
                            Scale = 1f
                        });
                    }
                    break;
                case AbilityType.Scorcher:
                    float3 pos = ability.ValueRO.TargetPosition;

                    var fireball = em.Instantiate(entitiesReferences.FireballPrefabEntity);
                    var fireballData = em.GetComponentData<Fireball>(fireball);
                    fireballData.Owner = owner;
                    fireballData.Radius = ResolveEffectRadius(ability.ValueRO, fireballData.Radius);
                    fireballData.Duration = ability.ValueRO.Duration > 0f ? ability.ValueRO.Duration : fireballData.Duration;
                    fireballData.Damage = ability.ValueRO.Power > 0f ? ability.ValueRO.Power : fireballData.Damage;
                    em.SetComponentData(fireball, fireballData);

                    em.SetComponentData(fireball, new LocalTransform
                    {
                        Position = pos,
                        Rotation = quaternion.identity,
                        Scale = 1
                    });
                    break;
                case AbilityType.Gauss:
                    if (hasEventHub && hasPhysicsWorld)
                    {
                        FireGaussShot(em, owner, ability.ValueRO, damageBuffer, physicsWorldSingleton.CollisionWorld);
                    }
                    break;
                case AbilityType.None:
                    break;
            }

            ecb.RemoveComponent<AbilityStartEvent>(ent);
        }

        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityEndEvent>().WithEntityAccess())
        {
            Entity owner = ResolveOwner(em, ability.ValueRO, ent);
            if (IsAbilityCasterDead(em, owner, ent))
            {
                ecb.RemoveComponent<AbilityEndEvent>(ent);
                continue;
            }

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
                    break;
                case AbilityType.None:
                    break;
            }
            ecb.RemoveComponent<AbilityEndEvent>(ent);
        }

        foreach ((RefRW<Ability> ability, Entity ent) in SystemAPI.Query<RefRW<Ability>>().WithAll<CooldownEndEvent>().WithEntityAccess())
        {
            Entity owner = ResolveOwner(em, ability.ValueRO, ent);
            if (IsAbilityCasterDead(em, owner, ent))
            {
                ecb.RemoveComponent<CooldownEndEvent>(ent);
                continue;
            }

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

    private static float ResolveEffectRadius(in Ability ability, float fallbackRadius)
    {
        return ability.Area > 0f
            ? ability.Area
            : math.max(0f, fallbackRadius);
    }

    private static bool IsAbilityCasterDead(EntityManager em, Entity owner, Entity fallback)
    {
        return (em.Exists(owner) && em.HasComponent<DeadUnit>(owner)) ||
               (fallback != owner && em.Exists(fallback) && em.HasComponent<DeadUnit>(fallback));
    }

    private static void FireGaussShot(EntityManager em, Entity owner, in Ability ability, DynamicBuffer<DamageEvent> damageBuffer, CollisionWorld physicsWorld)
    {
        float3 start = ResolveShotStart(em, owner);
        float3 forward = ResolveShotForward(em, owner);
        float3 end = ResolveGaussEnd(ability, start, forward);

        RaycastInput rayInput = new RaycastInput
        {
            Start = start,
            End = end,
            Filter = CollisionFilter.Default
        };

        var hits = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
        var damagedEntities = new NativeList<Entity>(Allocator.Temp);

        try
        {
            if (!physicsWorld.CastRay(rayInput, ref hits))
                return;

            SortHitsByDistance(hits);

            float damage = ResolveGaussDamage(em, owner, ability);
            int maxPierceCount = ResolveGaussPierceCount(em, owner);
            int damagedCount = 0;
            Faction ownerFaction = em.HasComponent<Unit>(owner)
                ? em.GetComponentData<Unit>(owner).faction
                : default;
            bool hasOwnerFaction = em.HasComponent<Unit>(owner);

            for (int i = 0; i < hits.Length; i++)
            {
                Entity hitEntity = physicsWorld.Bodies[hits[i].RigidBodyIndex].Entity;
                if (hitEntity == owner)
                    continue;

                if (!em.HasComponent<Unit>(hitEntity))
                    continue;

                if (ContainsEntity(damagedEntities, hitEntity))
                    continue;

                Unit targetUnit = em.GetComponentData<Unit>(hitEntity);
                if (hasOwnerFaction && targetUnit.faction == ownerFaction)
                    continue;

                damageBuffer.Add(new DamageEvent
                {
                    SourceEntity = owner,
                    TargetEntity = hitEntity,
                    TargetEntityClass = targetUnit.Class,
                    DamageAmount = damage,
                    IsAbilityDamage = true
                });

                damagedEntities.Add(hitEntity);
                damagedCount++;

                if (damagedCount >= maxPierceCount)
                    break;
            }
        }
        finally
        {
            damagedEntities.Dispose();
            hits.Dispose();
        }
    }

    private static float3 ResolveShotStart(EntityManager em, Entity owner)
    {
        if (!em.HasComponent<LocalTransform>(owner))
            return float3.zero;

        LocalTransform ownerTransform = em.GetComponentData<LocalTransform>(owner);
        if (!em.HasComponent<BulletSpawnPosition>(owner))
            return ownerTransform.Position;

        BulletSpawnPosition spawnPosition = em.GetComponentData<BulletSpawnPosition>(owner);
        return ownerTransform.TransformPoint(spawnPosition.bulletSpawnLocalPosition);
    }

    private static float3 ResolveShotForward(EntityManager em, Entity owner)
    {
        if (!em.HasComponent<LocalTransform>(owner))
            return new float3(0f, 0f, 1f);

        return math.forward(em.GetComponentData<LocalTransform>(owner).Rotation);
    }

    private static float3 ResolveGaussEnd(in Ability ability, float3 start, float3 fallbackForward)
    {
        float3 direction = ability.TargetPosition - start;
        direction.y = 0f;

        if (math.lengthsq(direction) < 0.0001f)
        {
            direction = fallbackForward;
            direction.y = 0f;
        }

        if (math.lengthsq(direction) < 0.0001f)
            direction = new float3(0f, 0f, 1f);

        float range = ability.Range > 0f ? ability.Range : math.length(direction);
        return start + math.normalize(direction) * range;
    }

    private static float ResolveGaussDamage(EntityManager em, Entity owner, in Ability ability)
    {
        if (ability.Power > 0f)
            return ability.Power;

        if (em.HasComponent<ShootAttack>(owner))
        {
            ShootAttack shootAttack = em.GetComponentData<ShootAttack>(owner);
            return shootAttack.ChargedAttackDamage > 0f
                ? shootAttack.ChargedAttackDamage
                : shootAttack.damageAmount;
        }

        return 1f;
    }

    private static int ResolveGaussPierceCount(EntityManager em, Entity owner)
    {
        if (!em.HasComponent<ShootAttack>(owner))
            return int.MaxValue;

        int pierceCount = em.GetComponentData<ShootAttack>(owner).maxPierceCount;
        return pierceCount > 0 ? pierceCount : int.MaxValue;
    }

    private static void SortHitsByDistance(NativeList<Unity.Physics.RaycastHit> hits)
    {
        for (int i = 0; i < hits.Length - 1; i++)
        {
            int closestIndex = i;
            for (int j = i + 1; j < hits.Length; j++)
            {
                if (hits[j].Fraction < hits[closestIndex].Fraction)
                    closestIndex = j;
            }

            if (closestIndex == i)
                continue;

            Unity.Physics.RaycastHit tmp = hits[i];
            hits[i] = hits[closestIndex];
            hits[closestIndex] = tmp;
        }
    }

    private static bool ContainsEntity(NativeList<Entity> entities, Entity entity)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i] == entity)
                return true;
        }

        return false;
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
                    Debug.Log("Added speed boost buffer to self");
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
                        Debug.Log("Added speed boost buffer to ally");
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
