using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public float healthAmount;
    public float healthAmountMax;

    public float armor;

    public class Baker : Baker<HealthAuthoring>
    {
        public override void Bake(HealthAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Health
            {
                healthAmount = authoring.healthAmount,
                healthAmountMax = authoring.healthAmountMax,
                OnHealthChanged = true,
                armor = authoring.armor,
            });
        }
    }
}

public struct Health : IComponentData 
{
    public float healthAmount;
    public float healthAmountMax;
    public bool OnHealthChanged;

    public float armor;
}