using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

partial struct UnitMoverSystem : ISystem
{
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 0.04f; // 0.2 * 0.2

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new UnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithNone(typeof(DeadUnit))]
public partial struct UnitMoverJob : IJobEntity
{
    public float deltaTime;

    public void Execute(
        ref LocalTransform localTransform,
        in UnitMover unitMover,
        ref PhysicsVelocity physicsVelocity)
    {
        float3 toTarget = unitMover.targetPosition - localTransform.Position;
        toTarget.y = 0; // тест
        float distSq = math.lengthsq(toTarget);

        if (distSq <= UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }

        // безопасная нормализация
        float invLen = math.rsqrt(distSq);
        float3 moveDir = toTarget * invLen;
        //float3 freezeRot = new float3(0, 1, 0);
        // поворот
        quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
        localTransform.Rotation =
            math.slerp(localTransform.Rotation, targetRot, deltaTime * unitMover.rotationSpeed);

        //localTransform.Position += moveDir * unitMover.CurrentMoveSpeed * deltaTime;
        localTransform.Position.y = 0f;
        
        physicsVelocity.Linear = moveDir * unitMover.CurrentMoveSpeed;
        physicsVelocity.Angular = float3.zero;
    }
}
