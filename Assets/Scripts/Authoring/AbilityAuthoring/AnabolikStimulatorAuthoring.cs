using Unity.Entities;
using UnityEngine;

public class AnabolikStimulatorAuthoring : MonoBehaviour
{
    public float SpeedBonus;
    public float AbilityReload;
    public float Duration;
    
    public bool isTriggered;
    public bool Active;

    public float TimeLeft;
    public float CooldownLeft;
    public class Baker : Baker<AnabolikStimulatorAuthoring>
    {
        public override void Bake(AnabolikStimulatorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AnabolikStimulator
            {
                SpeedBonus = authoring.SpeedBonus,
                AbilityReload = authoring.AbilityReload,
                Duration = authoring.Duration,
                
                isTriggered = authoring.isTriggered,
                Active = authoring.Active,

                TimeLeft = authoring.Duration,
                CooldownLeft = authoring.AbilityReload,
            }) ;
        }
    }
}
public struct AnabolikStimulator : IComponentData 
{
    public float SpeedBonus;
    public float AbilityReload;
    public float Duration;
    
    public bool isTriggered;
    public bool Active;

    public float TimeLeft;
    public float CooldownLeft;

}
