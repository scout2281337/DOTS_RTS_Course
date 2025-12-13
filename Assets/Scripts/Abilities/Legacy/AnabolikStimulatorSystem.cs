using Unity.Entities;
using UnityEngine;

partial struct AnabolikStimulatorSystem : ISystem
{
    
    public void OnUpdate(ref SystemState state) 
    {
        var dt = SystemAPI.Time.DeltaTime;
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        foreach ((RefRW<AnabolikStimulator> AnabolikStimulator, Entity entity) in SystemAPI.Query<RefRW<AnabolikStimulator>>().WithEntityAccess()) 
        {
            //cooldown
            if (AnabolikStimulator.ValueRW.CooldownLeft > 0 && !AnabolikStimulator.ValueRO.Active) 
            {
                AnabolikStimulator.ValueRW.CooldownLeft -= dt;
            }

            //CooldownEndEvent
            if (AnabolikStimulator.ValueRO.CooldownLeft <= 0) 
            {
                entityManager.AddComponent<CooldownEndEvent>(entity);
            }

            //Activation
            if (AnabolikStimulator.ValueRW.isTriggered && !AnabolikStimulator.ValueRW.Active && AnabolikStimulator.ValueRW.CooldownLeft <= 0) 
            {
                entityManager.AddComponent<AbilityStartEvent>(entity);
                AnabolikStimulator.ValueRW.Active = true;
                AnabolikStimulator.ValueRW.TimeLeft = AnabolikStimulator.ValueRO.Duration;
                AnabolikStimulator.ValueRW.CooldownLeft = AnabolikStimulator.ValueRO.AbilityReload;
                AnabolikStimulator.ValueRW.isTriggered = false;
            }

            if (!AnabolikStimulator.ValueRW.Active) 
            {
                continue;
            }

            foreach ((RefRW<UnitMover> unitMover, RefRO<ShootAttack> shootAttack) in SystemAPI.Query<RefRW<UnitMover>, RefRO<ShootAttack>>())
            {
                unitMover.ValueRW.CurrentMoveSpeed = unitMover.ValueRO.BaseSpeed * (1 + (AnabolikStimulator.ValueRO.SpeedBonus));

            }

            AnabolikStimulator.ValueRW.TimeLeft -= dt;
            if (AnabolikStimulator.ValueRW.TimeLeft <= 0) 
            {
                entityManager.AddComponent<AbilityEndEvent>(entity);
                AnabolikStimulator.ValueRW.Active = false;
                foreach ((RefRW<UnitMover> unitMover, RefRO<ShootAttack> shootAttack) in SystemAPI.Query<RefRW<UnitMover>, RefRO<ShootAttack>>())
                {
                    unitMover.ValueRW.CurrentMoveSpeed = unitMover.ValueRO.BaseSpeed;
                }
            }
        }
    }
}
