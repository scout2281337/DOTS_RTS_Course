using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ShootAttackSystem : ISystem
{
    EntityArchetype damageArchetype;

    public void OnCreate(ref SystemState state)
    {
        damageArchetype = state.EntityManager.CreateArchetype(
            typeof(DamageEvent)
        );
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences)) return;

        var physicsWorld = SystemAPI
            .GetSingleton<PhysicsWorldSingleton>()
            .CollisionWorld;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRW<ShootAttack> shootAttack,
            RefRW<LocalTransform> localTransform,
            RefRO<Target> target,
            RefRO<Unit> Unit,
            RefRO<BulletSpawnPosition> bulletSpawnPosition,
            RefRW<UnitMover> unitMover) 
            in SystemAPI.Query<
                RefRW<ShootAttack>,
                RefRW<LocalTransform>,
                RefRO<Target>,
                RefRO<Unit>,
                RefRO<BulletSpawnPosition>,
                RefRW<UnitMover>>().WithDisabled<MoveOverride>()) 
        {
            if (target.ValueRO.targetEntity == Entity.Null) continue;

            // Проверка на дистанцию до цели
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
            {
                unitMover.ValueRW.targetPosition = targetLocalTransform.Position; 
                continue;
            }

            // Разворот до цели и анимация разворота
            unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
            float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
            aimDirection = math.normalize(aimDirection);

            quaternion targetRotation = quaternion.LookRotation(aimDirection, math.up());
            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);

            shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;

            if (shootAttack.ValueRO.timer > 0f) continue;
            
            // Стрельба
            shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMax;

            float3 bulletSpawnWorldPosition = localTransform.ValueRO.TransformPoint(bulletSpawnPosition.ValueRO.bulletSpawnLocalPosition);
            RaycastInput input = new RaycastInput
            {
                Start = bulletSpawnWorldPosition,
                End = bulletSpawnWorldPosition + aimDirection * shootAttack.ValueRO.attackDistance,
                Filter = new CollisionFilter //реворк по настроению
                {
                    BelongsTo = ~0u,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            };

            var hub = SystemAPI
                .QueryBuilder()
                .WithAll<EventHub>()
                .Build()
                .GetSingletonEntity();

            if (physicsWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
            {
                Entity hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                Entity damageEntity = ecb.CreateEntity();
                Unit targetUnit = SystemAPI.GetComponent<Unit>(hitEntity);

                var damageBuffer = SystemAPI.GetBuffer<DamageEvent>(hub);

                damageBuffer.Add(new DamageEvent
                {
                    TargetEntity = hitEntity,
                    TargetEntityClass = targetUnit.Class,
                    DamageAmount = shootAttack.ValueRO.damageAmount,
                });
            }


            var bulletEvents = SystemAPI.GetBuffer<BulletShotEvent>(hub);

            bulletEvents.Add(new BulletShotEvent
            {
                From = bulletSpawnWorldPosition,
                To = hit.Position,
            });

        }
    }
}

/*[BurstCompile]
public partial struct ShootAttackJob : IJobEntity
{
    public float deltaTime;
    public LocalTransform targetLocalTransform;

    public void Execute(ref ShootAttack shootAttack, ref LocalTransform localTransform, in Target target, ref UnitMover unitMover) 
    {
        if (target.targetEntity == Entity.Null)
        {
            return;
        }

        //LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
        if (math.distance(localTransform.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
        {
            unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
            continue;
        }
        else
        {
            unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
        }

        float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
        aimDirection = math.normalize(aimDirection);

        quaternion targetRotation = quaternion.LookRotation(aimDirection, math.up());
        localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);

        shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;

        if (shootAttack.ValueRO.timer > 0f)
        {
            continue;
        }
        shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMax;

        Entity bulletEntity = state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
        float3 bulletSpawnWorldPosition = localTransform.ValueRO.TransformPoint(shootAttack.ValueRO.bulletSpawnLocalPosition);
        SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition));

        RefRW<Bullet> bulletBullet = SystemAPI.GetComponentRW<Bullet>(bulletEntity);
        bulletBullet.ValueRW.damageAmount = shootAttack.ValueRO.damageAmount;

        RefRW<Target> bulletTarget = SystemAPI.GetComponentRW<Target>(bulletEntity);
        bulletTarget.ValueRW.targetEntity = target.ValueRO.targetEntity;

        shootAttack.ValueRW.onShoot.isTriggered = true;
        shootAttack.ValueRW.onShoot.shootFromPosition = bulletSpawnWorldPosition;
    }
}
*/
