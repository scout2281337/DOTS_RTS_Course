using Unity.Burst;
using Unity.Entities;

[BurstCompile]
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

            mover.ValueRW.CurrentMoveSpeed = speed;
        }
    }
}
