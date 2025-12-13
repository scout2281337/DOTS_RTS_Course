
using Unity.Entities;
//using UnityEngine;
partial struct AbilityEffectSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityStartEvent>().WithEntityAccess())
        {
            switch (ability.ValueRO.Type)
            {
                case AbilityType.AnabolikStimulator:
                    ApplySpeedBoost(ref state, em, ability.ValueRO.Owner, ability.ValueRO.TargetType, 3f);
                    //Debug.Log("chf,");
                    break;
                case AbilityType.Fireball:
                    //LaunchFireball(em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
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

            // Убираем одноразовый Event
            ecb.RemoveComponent<AbilityStartEvent>(ent);
        }
        foreach ((RefRO<Ability> ability, Entity ent) in SystemAPI.Query<RefRO<Ability>>().WithAll<AbilityEndEvent>().WithEntityAccess()) //рефактор сделать
        {
            switch (ability.ValueRO.Type)
            {
                case AbilityType.AnabolikStimulator:
                    EndSpeedBoost(ref state, em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
                    break;
                case AbilityType.Fireball:
                    //LaunchFireball(em, ability.ValueRO.Owner, ability.ValueRO.TargetType);
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

            // Убираем одноразовый Event
            ecb.RemoveComponent<AbilityEndEvent>(ent);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    void ApplySpeedBoost(ref SystemState state, EntityManager em, Entity owner, AbilityTargetType target, float multiplier)
    {
        switch (target)
        {
            case AbilityTargetType.Self:
                if (em.HasComponent<UnitMover>(owner))
                {
                    var mover = SystemAPI.GetComponentRW<UnitMover>(owner);
                    
                    mover.ValueRW.CurrentMoveSpeed *= multiplier;
                }
                break;
            case AbilityTargetType.Ally:
                foreach ((RefRW<UnitMover> allyMover, RefRO<Unit> friendlyUnit) in
                         SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>())
                {
                    // проверяем союзников по команде
                    if (friendlyUnit.ValueRO.faction == SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction)
                        allyMover.ValueRW.CurrentMoveSpeed *= multiplier;
                }
                break;
            case AbilityTargetType.Enemy:
                foreach ((RefRW<UnitMover> enemyMover, RefRO<Unit> friendlyUnit) in
                         SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>())
                {
                    // проверяем вражин
                    if (friendlyUnit.ValueRO.faction != SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction)
                        enemyMover.ValueRW.CurrentMoveSpeed *= multiplier;
                }
                break;
            case AbilityTargetType.Area:
                //  // чета придумать я тупой
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
                    //Debug.Log("endSpeedB");
                    var mover = SystemAPI.GetComponentRW<UnitMover>(owner);

                    mover.ValueRW.CurrentMoveSpeed = mover.ValueRO.BaseSpeed;
                }
                break;
            case AbilityTargetType.Ally:
                foreach ((RefRW<UnitMover> allyMover, RefRO<Unit> friendlyUnit) in
                             SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>())
                {
                    // проверяем союзников по команде
                    if (friendlyUnit.ValueRO.faction == SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction)
                        allyMover.ValueRW.CurrentMoveSpeed = allyMover.ValueRO.BaseSpeed;
                }
                break;
            case AbilityTargetType.Enemy:
                foreach ((RefRW<UnitMover> enemyMover, RefRO<Unit> friendlyUnit) in
                             SystemAPI.Query<RefRW<UnitMover>, RefRO<Unit>>())
                {
                    // проверяем вражин
                    if (friendlyUnit.ValueRO.faction != SystemAPI.GetComponentRW<Unit>(owner).ValueRO.faction)
                        enemyMover.ValueRW.CurrentMoveSpeed = enemyMover.ValueRO.BaseSpeed;
                }
                break;
            case AbilityTargetType.Area:
                // пример: можно применить к юнитам рядом с owner (через Position) // чета придумать я тупой
                break;
        }
    }

}
