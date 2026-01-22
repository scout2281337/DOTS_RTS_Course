using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
partial struct AGBSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);



        foreach (var (fieldTransform, agb, fieldEntity)
                 in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AGB>>()
                             .WithEntityAccess())
        {
            float rangeSq = agb.ValueRO.Range * agb.ValueRO.Range;
            if (agb.ValueRO.Duration <= 0)
            {
                ecb.DestroyEntity(fieldEntity);
                continue;
            }
            else 
            {
                agb.ValueRW.Duration -= SystemAPI.Time.DeltaTime;   
            }
            foreach (var (unitTransform, unitData, unitEntity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Unit>>()
                                 .WithEntityAccess())
            {
                if (unitData.ValueRO.faction != Faction.Zombie)
                    continue;

                bool inside =
                    math.distancesq(fieldTransform.ValueRO.Position,
                                     unitTransform.ValueRO.Position)
                    <= rangeSq;

                if (inside)
                {
                    // гарантируем буфер
                    if (!SystemAPI.HasBuffer<SlowDebuff>(unitEntity))
                    {
                        ecb.AddBuffer<SlowDebuff>(unitEntity);
                        continue; // добавим в следующем кадре
                    }

                    var buffer = SystemAPI.GetBuffer<SlowDebuff>(unitEntity);

                    // провер€ем, есть ли эффект именно от Ё“ќ√ќ пол€
                    bool hasModifierFromThisField = false;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].Source == fieldEntity)
                        {
                            hasModifierFromThisField = true;
                            break;
                        }
                    }

                    if (!hasModifierFromThisField)
                    {
                        buffer.Add(new SlowDebuff
                        {
                            Multiplier = agb.ValueRO.SpeedDebuff,
                            Source = fieldEntity
                        });
                    }
                }
                else
                {
                    // вне зоны убираем “ќЋ№ ќ эффект от этого пол€
                    if (!SystemAPI.HasBuffer<SlowDebuff>(unitEntity))
                        continue;

                    var buffer = SystemAPI.GetBuffer<SlowDebuff>(unitEntity);

                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        if (buffer[i].Source == fieldEntity)
                        {
                            buffer.RemoveAt(i);
                        }
                    }
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
