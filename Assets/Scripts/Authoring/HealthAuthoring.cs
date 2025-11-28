using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public float healthAmount;
    public float healthAmountMax;

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
            });
        }
    }
}

public struct Health : IComponentData 
{
    public float healthAmount;
    public float healthAmountMax;
    public bool OnHealthChanged;
}