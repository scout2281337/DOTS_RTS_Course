using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct StaticShootAttackSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        foreach ((
            RefRW<ShootAttack> shootAttack,
            RefRW<LocalTransform> localTransform,
            RefRO<Target> target,
            RefRO <BulletSpawnPosition> bulletSpawnPosition,
            RefRO <Building> building)
            in SystemAPI.Query<
                RefRW<ShootAttack>,
                RefRW<LocalTransform>,
                RefRO<Target>,
                RefRO<BulletSpawnPosition>,
                RefRO <Building>>())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }

            //LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            //float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
            //aimDirection = math.normalize(aimDirection);

            //rotation of shooter on building
            //quaternion targetRotation = quaternion.LookRotation(aimDirection, math.up());
            //localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);

            shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;

            if (shootAttack.ValueRO.timer > 0f)
            {
                continue;
            }
            shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMax;

            Entity bulletEntity = state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
            float3 bulletSpawnWorldPosition = localTransform.ValueRO.TransformPoint(bulletSpawnPosition.ValueRO.bulletSpawnLocalPosition);
            SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition));

            RefRW<Bullet> bulletBullet = SystemAPI.GetComponentRW<Bullet>(bulletEntity);
            bulletBullet.ValueRW.damageAmount = shootAttack.ValueRO.damageAmount;

            RefRW<Target> bulletTarget = SystemAPI.GetComponentRW<Target>(bulletEntity);
            bulletTarget.ValueRW.targetEntity = target.ValueRO.targetEntity;

            shootAttack.ValueRW.onShoot.isTriggered = true;
            shootAttack.ValueRW.onShoot.shootFromPosition = bulletSpawnWorldPosition;
        }
    }


}
