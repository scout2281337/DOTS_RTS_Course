using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerAnimationStateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (animState, target, entity) in
                 SystemAPI.Query<RefRW<AnimationStateComponent>, Target>()
                          .WithNone<DeadUnit>()
                          .WithEntityAccess())
        {

            // 1. Атака (самый высокий приоритет)
            if (SystemAPI.HasComponent<AttackRequest>(entity) && animState.ValueRW.Value != AnimationState.Attack)
            {
                animState.ValueRW.Value = AnimationState.Attack;
                ecb.RemoveComponent<AttackRequest>(entity);
                continue;
            }

            // 2. Движение
            if (target.targetEntity != Entity.Null)
            {
                animState.ValueRW.Value = AnimationState.Move;
            }
            else
            {
                animState.ValueRW.Value = AnimationState.Move;// можно idle если надо
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
