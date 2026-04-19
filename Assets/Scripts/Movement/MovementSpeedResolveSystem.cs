using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct MovementSpeedResolveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (mover, entity)
                 in SystemAPI.Query<RefRW<UnitMover>>()
                             .WithEntityAccess())
        {
            float speed = mover.ValueRO.BaseSpeed;

            if (SystemAPI.HasBuffer<SlowDebuff>(entity))
            {
                var buffer = SystemAPI.GetBuffer<SlowDebuff>(entity);

                for (int i = 0; i < buffer.Length; i++)
                {
                    speed *= buffer[i].Multiplier;
                }
            }

            if (SystemAPI.HasComponent<AcidBulletsDebuff>(entity))
            {
                var acid = SystemAPI.GetComponentRO<AcidBulletsDebuff>(entity);
                speed *= math.max(0f, 1f - acid.ValueRO.Stacks * acid.ValueRO.MoveSlowPerStack);
            }

            if (SystemAPI.HasComponent<BloodySpeedUpModule>(entity))
            {
                var bloody = SystemAPI.GetComponentRO<BloodySpeedUpModule>(entity);
                speed *= 1f + bloody.ValueRO.Stacks * bloody.ValueRO.SpeedPerStack;
            }

            if (SystemAPI.HasComponent<StunEffect>(entity))
            {
                speed = 0f;
            }

            mover.ValueRW.CurrentMoveSpeed = speed;
        }
    }
}
