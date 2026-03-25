using Unity.Entities;
using UnityEngine;

public class AGBAuthoring : MonoBehaviour
{
    public SkillConfig JuggernautSkillCfg;
    public class Baker : Baker<AGBAuthoring>
    {
        public override void Bake(AGBAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AGB
            {
                SpeedDebuff = authoring.JuggernautSkillCfg.power,
                Cooldown = authoring.JuggernautSkillCfg.cooldown,
                Duration = authoring.JuggernautSkillCfg.duration,
                Range = authoring.JuggernautSkillCfg.range,

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