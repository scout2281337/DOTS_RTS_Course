using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
partial struct AdrenalineSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // =========================
        // ТРИГГЕР АБИЛКИ
        // =========================

        foreach ((RefRW<Adrenaline> adrenaline, Entity entity)
            in SystemAPI.Query<RefRW<Adrenaline>>().WithEntityAccess())
        {
            if (adrenaline.ValueRO.Timer > 0)
            {
                adrenaline.ValueRW.Timer -= dt;
                continue;
            }
            if (!adrenaline.ValueRO.CanActivate)
            {
                continue;
            }
            adrenaline.ValueRW.CanActivate = false;
            adrenaline.ValueRW.Timer = adrenaline.ValueRO.TimerMax;

            if (!SystemAPI.HasBuffer<SlowDebuff>(entity))
            {
                ecb.AddBuffer<SlowDebuff>(entity);
                continue;
            }

            var buffer = SystemAPI.GetBuffer<SlowDebuff>(entity);

            buffer.Add(new SlowDebuff
            {
                Multiplier = adrenaline.ValueRO.SpeedMultiplier,
                Source = entity,
                Timer = adrenaline.ValueRO.BuffDuration
            });
        }

        // =========================
        // ОБНОВЛЕНИЕ ТАЙМЕРОВ БАФФОВ
        // =========================
        // мб вынести отдельно
        foreach ((RefRO<UnitMover> unitMover, Entity entity)
            in SystemAPI.Query<RefRO<UnitMover>>().WithEntityAccess())
        {

            if (!SystemAPI.HasBuffer<SlowDebuff>(entity))
            {
                continue;
            }

            var buffer = SystemAPI.GetBuffer<SlowDebuff>(entity);


            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                var debuff = buffer[i];
                debuff.Timer -= dt;

                if (debuff.Timer <= 0)
                {
                    buffer.RemoveAt(i);

                }
                else
                {
                    buffer[i] = debuff;
                }
            }
        }
    }
}
