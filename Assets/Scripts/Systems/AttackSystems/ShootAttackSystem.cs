using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;

partial struct ShootAttackSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        /*
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var job = new ShootAttackJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            localTransformLookup = state.GetComponentLookup<LocalTransform>(true), // ReadOnly
            bulletPrefab = entitiesReferences.bulletPrefabEntity,
            ecb = ecb
        };

        job.ScheduleParallel();
        */



        foreach ((
            RefRW<ShootAttack> shootAttack,
            RefRW<LocalTransform> localTransform,
            RefRO<Target> target,
            RefRW<UnitMover> unitMover) 
            in SystemAPI.Query<
                RefRW<ShootAttack>,
                RefRW<LocalTransform>,
                RefRO<Target>,
                RefRW<UnitMover>>().WithDisabled<MoveOverride>()) 
        {
            
            
            
            
            if (target.ValueRO.targetEntity == Entity.Null) 
            {
                continue;
            }

            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
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

            Entity bulletEntity =  state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
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

}
/*[BurstCompile]
public partial struct ShootAttackJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly]public ComponentLookup<LocalTransform> localTransformLookup;
    public Entity bulletPrefab;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity entity,
        ref ShootAttack shootAttack,
        ref LocalTransform localTransform,
        in Target target,
        ref UnitMover unitMover)
    {
        if (target.targetEntity == Entity.Null || !localTransformLookup.HasComponent(target.targetEntity))
            return;

        var targetLocalTransform = localTransformLookup[target.targetEntity];

        if (math.distance(localTransform.Position, targetLocalTransform.Position) > shootAttack.attackDistance)
        {
            unitMover.targetPosition = targetLocalTransform.Position;
            return;
        }
        else
        {
            unitMover.targetPosition = localTransform.Position;
        }

        float3 aimDirection = math.normalize(targetLocalTransform.Position - localTransform.Position);
        quaternion targetRotation = quaternion.LookRotation(aimDirection, math.up());
        localTransform.Rotation = math.slerp(localTransform.Rotation, targetRotation, deltaTime * unitMover.rotationSpeed);

        shootAttack.timer -= deltaTime;
        if (shootAttack.timer > 0f)
            return;

        shootAttack.timer = shootAttack.timerMax;

        Entity bulletEntity = ecb.Instantiate(entityInQueryIndex, bulletPrefab);
        float3 bulletSpawnWorldPosition = localTransform.TransformPoint(shootAttack.bulletSpawnLocalPosition);

        ecb.SetComponent(entityInQueryIndex, bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition));
        ecb.SetComponent(entityInQueryIndex, bulletEntity, new Bullet { damageAmount = shootAttack.damageAmount });
        ecb.SetComponent(entityInQueryIndex, bulletEntity, new Target { targetEntity = target.targetEntity });

        shootAttack.onShoot.isTriggered = true;
        shootAttack.onShoot.shootFromPosition = bulletSpawnWorldPosition;
    }
}
*/
