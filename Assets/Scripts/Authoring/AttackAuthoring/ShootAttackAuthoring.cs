using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ShootAttackAuthoring : MonoBehaviour
{
    public WeaponConfig weaponConfig;

    private static float CooldownFromFireRate(float fireRate)
    {
        return fireRate > 0f ? 60f / fireRate : float.MaxValue;
    }

    public class Baker : Baker<ShootAttackAuthoring>
    {
        public override void Bake(ShootAttackAuthoring authoring)
        {
            Entity entity  = GetEntity(TransformUsageFlags.Dynamic);
            float cooldown = CooldownFromFireRate(authoring.weaponConfig.fireRate);
            AddComponent(entity, new ShootAttack
            {
                timer = cooldown,
                timerMax = cooldown,
                damageAmount = authoring.weaponConfig.damage,
                attackDistance = authoring.weaponConfig.range,
                weaponType = authoring.weaponConfig.weaponType,
                maxPierceCount = authoring.weaponConfig.maxPierceCount,
                explosiveRange = authoring.weaponConfig.explosiveRange,
                attackMode = AttackMode.Normal,
                ChargedAttackDamage = authoring.weaponConfig.ChargedAttackDamage,
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
    public WeaponType weaponType;
    public int maxPierceCount;
    public float explosiveRange;

    public AttackMode attackMode;
    public float ChargedAttackDamage;
}

public enum AttackMode
{
    Normal,
    Charged
}
