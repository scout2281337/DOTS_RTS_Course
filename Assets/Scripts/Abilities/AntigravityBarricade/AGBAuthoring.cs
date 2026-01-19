using Unity.Entities;
using UnityEngine;

public class AGBAuthoring : MonoBehaviour
{
    public float SpeedDebuff = 0.6f;
    public float Cooldown = 40;
    public float Duration = 10;
    public float Range = 10;
    public class Baker : Baker<AGBAuthoring>
    {
        public override void Bake(AGBAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AGB
            {
                SpeedDebuff = authoring.SpeedDebuff,
                Cooldown = authoring.Cooldown,
                Duration = authoring.Duration,
                Range = authoring.Range,

            });

        }
    }
}

public struct AGB : IComponentData 
{
    public float SpeedDebuff;
    public float Cooldown;
    public float Duration;
    public float Range;
}