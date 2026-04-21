using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ShootAttackSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI
            .GetSingleton<PhysicsWorldSingleton>()
            .CollisionWorld;
        EntityCommandBuffer ecb =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var hub = SystemAPI
            .QueryBuilder()
            .WithAll<EventHub>()
            .Build()
            .GetSingletonEntity();

        var damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hub);
        var bulletEvents = SystemAPI.GetBuffer<BulletShotEvent>(hub);

        foreach ((
            RefRW<ShootAttack> shootAttack,
            RefRW<LocalTransform> localTransform,
            RefRO<Target> target,
            RefRO<Unit> unit,
            RefRO<BulletSpawnPosition> bulletSpawnPosition,
            RefRW<UnitMover> unitMover,
            Entity entity)
            in SystemAPI.Query<
                RefRW<ShootAttack>,
                RefRW<LocalTransform>,
                RefRO<Target>,
                RefRO<Unit>,
                RefRO<BulletSpawnPosition>,
                RefRW<UnitMover>>()
            .WithDisabled<MoveOverride>().WithEntityAccess())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
                continue;

            if (SystemAPI.HasComponent<StunEffect>(entity))
                continue;

            LocalTransform targetTransform =
                SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);

            float distance = math.distance(
                localTransform.ValueRO.Position,
                targetTransform.Position);

            if (distance > shootAttack.ValueRO.attackDistance)
            {
                unitMover.ValueRW.targetPosition = targetTransform.Position;
                continue;
            }

            unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;

            float3 aimDir = math.normalize(
                targetTransform.Position - localTransform.ValueRO.Position);

            quaternion targetRot =
                quaternion.LookRotation(aimDir, math.up());

            localTransform.ValueRW.Rotation =
                math.slerp(
                    localTransform.ValueRO.Rotation,
                    targetRot,
                    SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);

            shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
            if (shootAttack.ValueRO.timer > 0f)
                continue;

            if (SystemAPI.HasComponent<MainCharacter>(entity))
            {
                RefRO<MainCharacter> MainCharacter = SystemAPI.GetComponentRO<MainCharacter>(entity);
                float fireRateMultiplier = 1f + math.max(0f, MainCharacter.ValueRO.FireRateBoost) * 0.01f;
                shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMax / fireRateMultiplier;
            }
            else 
            {
                shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMax;
            
            }

            float3 bulletSpawnWorldPos =
                localTransform.ValueRO.TransformPoint(
                    bulletSpawnPosition.ValueRO.bulletSpawnLocalPosition);

            RaycastInput rayInput = new RaycastInput
            {
                Start = bulletSpawnWorldPos,
                End = bulletSpawnWorldPos + aimDir * shootAttack.ValueRO.attackDistance,
                Filter = CollisionFilter.Default
            };




            // ==============================
            // СТРЕЛЬБА
            // ==============================

            float damage = shootAttack.ValueRO.damageAmount;
            if (shootAttack.ValueRO.attackMode == AttackMode.Charged)
            {
                damage = shootAttack.ValueRO.ChargedAttackDamage;
            }
            else 
            {
                damage = shootAttack.ValueRO.damageAmount;
            }


            // ===================
            //проверка на наличие компонента для анимации атаки
            //====================
            if (!SystemAPI.HasComponent<AttackRequest>(entity))
            {
                ecb.AddComponent<AttackRequest>(entity);
            }




            // ==============================
            //Модуль берсерк

            if (SystemAPI.HasComponent<BerserkerICD>(entity))
            {
                RefRO<Health> health = SystemAPI.GetComponentRO<Health>(entity);
                float hpPercent = health.ValueRO.healthAmount / health.ValueRO.healthAmountMax;
                if (hpPercent <= 0.5f)
                {
                    float bonus = (0.5f - hpPercent) * 2f;
                    damage *= 1f + bonus * 0.5f;
                }
            }
            // ==============================



            switch (shootAttack.ValueRO.weaponType)
            {
                case WeaponType.SingleRay:
                    {
                        if (physicsWorld.CastRay(rayInput, out var hit))
                        {
                            Entity hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                            if (SystemAPI.HasComponent<Unit>(hitEntity))
                            {
                                Unit targetUnit = SystemAPI.GetComponent<Unit>(hitEntity);
                                ApplyDamage(hit, physicsWorld, shootAttack, damageBuffer, targetUnit, unit.ValueRO.faction, damage, entity);
                            }

                            bulletEvents.Add(new BulletShotEvent
                            {
                                Start = bulletSpawnWorldPos,
                                End = hit.Position,
                                WeaponType = shootAttack.ValueRO.weaponType,
                            });
                        }
                        break;
                    }

                case WeaponType.PiercingRay:
                    {
                        var hits = new NativeList<RaycastHit>(Allocator.Temp);
                        physicsWorld.CastRay(rayInput, ref hits);

                        int pierceLeft = shootAttack.ValueRO.maxPierceCount;

                        foreach (var hit in hits)
                        {
                            if (pierceLeft-- <= 0)
                                break;

                            Entity hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                            if (!SystemAPI.HasComponent<Unit>(hitEntity))
                                continue;

                            Unit targetUnit = SystemAPI.GetComponent<Unit>(hitEntity);
                            ApplyDamage(hit, physicsWorld, shootAttack, damageBuffer, targetUnit, unit.ValueRO.faction, damage, entity);
                        }

                        hits.Dispose();
                        break;
                    }

                case WeaponType.Explosive:
                    {
                        if (!physicsWorld.CastRay(rayInput, out var hit))
                            break;

                        float3 hitPos = hit.Position;
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);

                        physicsWorld.OverlapSphere(
                            hitPos,
                            shootAttack.ValueRO.explosiveRange,
                            ref hits,
                            CollisionFilter.Default);

                        foreach (var h in hits)
                        {
                            Entity e =
                                physicsWorld.Bodies[h.RigidBodyIndex].Entity;
                            if (!SystemAPI.HasComponent<Unit>(e))
                                continue;

                            if (e == entity)
                                continue;

                            Unit targetUnit = SystemAPI.GetComponent<Unit>(e);
                            if (targetUnit.faction == unit.ValueRO.faction)
                                continue;

                            damageBuffer.Add(new DamageEvent
                            {
                                SourceEntity = entity,
                                TargetEntity = e,
                                TargetEntityClass = targetUnit.Class,
                                DamageAmount = damage
                            });
                        }

                        hits.Dispose();

                        bulletEvents.Add(new BulletShotEvent
                        {
                            Start = bulletSpawnWorldPos,
                            End = hitPos,
                            WeaponType = shootAttack.ValueRO.weaponType,
                        });
                        break;
                    }

                case WeaponType.Dispersive:
                    {
                        float3 center =
                            bulletSpawnWorldPos + aimDir;
                        quaternion boxRotation = quaternion.LookRotationSafe(aimDir, math.up());
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);

                        physicsWorld.OverlapBox(
                            center,
                            boxRotation,
                            new float3 (5f, 1f,5f), //shootAttack.ValueRO.attackDistance / 2f
                            ref hits,
                            CollisionFilter.Default);



                        bulletEvents.Add(new BulletShotEvent
                        {
                            Start = bulletSpawnWorldPos,
                            End = center,
                            WeaponType = shootAttack.ValueRO.weaponType,
                        });
                        foreach (var h in hits)
                        {
                            Entity e =
                                physicsWorld.Bodies[h.RigidBodyIndex].Entity;
                            if (!SystemAPI.HasComponent<Unit>(e))
                                continue;


                            if (e == entity)
                                continue;

                            Unit targetUnit =
                                SystemAPI.GetComponent<Unit>(e);
                            if (targetUnit.faction == unit.ValueRO.faction)
                                continue;

                            damageBuffer.Add(new DamageEvent
                            {
                                SourceEntity = entity,
                                TargetEntity = e,
                                TargetEntityClass = targetUnit.Class,
                                DamageAmount = damage
                            });
                        }

                        hits.Dispose();
                        break;
                    }
            }
        }
    }

    // ==============================
    // APPLY DAMAGE HELPER
    // ==============================
    private static void ApplyDamage(RaycastHit hit,CollisionWorld physicsWorld,RefRW<ShootAttack> shootAttack,DynamicBuffer<DamageEvent> damageBuffer, Unit targetUnit, Faction sourceFaction, float Damage, Entity SourceEntity)
    {
        Entity hitEntity =
            physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

        if (hitEntity == SourceEntity)
            return;
        if (targetUnit.faction == sourceFaction)
            return;

        damageBuffer.Add(new DamageEvent
        {
            SourceEntity = SourceEntity,
            TargetEntity = hitEntity,
            TargetEntityClass = targetUnit.Class,
            DamageAmount = Damage
        });
    }
}



