using System;
using Unity.Entities;
using UnityEditor.Localization.Plugins.XLIFF.V20;

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct AbilityEffectSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences))
            return;

        var em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);


        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityStartEvent>().WithEntityAccess())
        {
            switch (ability.ValueRO.Type)
            {
                case AbilityType.AnabolicStimulator:
                    ApplySpeedBoost(ref state, em, ecb, ent, ability.ValueRO.TargetType, 3f); //ability.ValueRO.Owner
                    break;
                case AbilityType.AntiGravitationBarrier:
                    SpawnObject(entitiesReferences.AntiGravitationBarrier, ref state);
                    break;
                case AbilityType.Shield:
                    //ApplyShield(em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
                    break;
                case AbilityType.Heal:
                    //HealTargets(em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
                    break;
                case AbilityType.None:
                    break;
            }
            AbilityEventListener.Instance?.RaiseAbilityStarted(ent, ability.ValueRO.Type); 

            // ������� ����������� Event
            ecb.RemoveComponent<AbilityStartEvent>(ent);
        }

        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityEndEvent>().WithEntityAccess()) //�������� �������
        {
            switch (ability.ValueRO.Type)
            {
                case AbilityType.AnabolicStimulator:
                    EndSpeedBoost(ref state, em, ent, ability.ValueRO.TargetType);
                    break;
                case AbilityType.AntiGravitationBarrier:
                    //�����
                    break;
                case AbilityType.Shield:
                    //ApplyShield(em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
                    break;
                case AbilityType.Heal:
                    //HealTargets(em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
                    break;
                case AbilityType.None:
                    break;
            }
            AbilityEventListener.Instance?.RaiseAbilityEnded(ent, ability.ValueRO.Type); // LEGACY but fine right now
            // ������� ����������� Event
            ecb.RemoveComponent<AbilityEndEvent>(ent);
        }

        foreach ((RefRW<Ability> ability, Entity ent) in SystemAPI.Query<RefRW<Ability>>().WithAll<CooldownEndEvent>().WithEntityAccess()) //�������� �������
        {
            
            AbilityEventListener.Instance?.RaiseCooldownEnded(ent, ability.ValueRO.Type);
            ecb.RemoveComponent<CooldownEndEvent>(ent);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    void ApplySpeedBoost(ref SystemState state, EntityManager em, EntityCommandBuffer ecb, Entity owner, AbilityTargetType target, float multiplier)
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
                    });
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
                        });
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
                    // ��������� ������
                    if (friendlyUnit.ValueRO.faction != SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction)
                        enemyMover.ValueRW.CurrentMoveSpeed = enemyMover.ValueRO.BaseSpeed;
                }
                break;
            case AbilityTargetType.Area:
                // ������: ����� ��������� � ������ ����� � owner (����� Position) // ���� ��������� � �����
                break;
        }
    }
    void SpawnObject(Entity EntityToSpawn, ref SystemState State) 
    {
        Entity entityToSpawn = State.EntityManager.Instantiate(EntityToSpawn);
    }
}
