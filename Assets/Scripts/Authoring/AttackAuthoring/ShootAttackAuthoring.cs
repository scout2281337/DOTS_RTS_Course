using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ShootAttackAuthoring : MonoBehaviour
{
    public WeaponConfig weaponConfig;
    public class Baker : Baker<ShootAttackAuthoring>
    {
        public override void Bake(ShootAttackAuthoring authoring)
        {
            Entity entity  = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ShootAttack
            {
                timerMax = 60f / authoring.weaponConfig.fireRate,
                damageAmount = authoring.weaponConfig.damage,
                attackDistance = authoring.weaponConfig.range,
                weaponType = authoring.weaponConfig.weaponType,
                maxPierceCount = authoring.weaponConfig.maxPierceCount,
                explosiveRange = authoring.weaponConfig.explosiveRange,
            });
        }
    }
}

public struct ShootAttack : IComponentData 
{
    public float timer;
    public float timerMax;
    public float damageAmount;
    public float attackDistance;
    public WeaponTypes weaponType;
    public int maxPierceCount;
    public float explosiveRange;
}