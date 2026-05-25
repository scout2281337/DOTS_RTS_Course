using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerAnimationStateSystem : ISystem
{
    private const float AttackStateDuration = 0.35f;

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (animState, localTransform, unitMover, entity) in
                 SystemAPI.Query<
                     RefRW<AnimationStateComponent>,
                     RefRO<LocalTransform>,
                     RefRO<UnitMover>>()
                          .WithNone<DeadUnit>()
                          .WithEntityAccess())
        {
            if (SystemAPI.HasComponent<AttackRequest>(entity))
            {
                animState.ValueRW.Value = AnimationState.Attack;
                animState.ValueRW.AttackLockTimer = AttackStateDuration;
                ecb.RemoveComponent<AttackRequest>(entity);
                continue;
            }

            if (animState.ValueRO.AttackLockTimer > 0f)
            {
                animState.ValueRW.AttackLockTimer = math.max(0f, animState.ValueRO.AttackLockTimer - deltaTime);
                animState.ValueRW.Value = AnimationState.Attack;
                continue;
            }

            animState.ValueRW.Value = IsMoving(localTransform.ValueRO, unitMover.ValueRO)
                ? AnimationState.Move
                : AnimationState.Idle;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static bool IsMoving(in LocalTransform localTransform, in UnitMover unitMover)
    {
        float3 delta = unitMover.targetPosition - localTransform.Position;
        delta.y = 0f;
        return math.lengthsq(delta) > UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ;
    }
}
