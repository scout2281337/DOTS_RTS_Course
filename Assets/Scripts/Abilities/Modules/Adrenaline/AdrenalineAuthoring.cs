using Unity.Entities;
using UnityEngine;

public class AdrenalineAuthoring : MonoBehaviour
{
    public float SpeedMultiplier;
    public float Timer;
    public float TimerMax;

    public float BuffDuration;
    public bool CanActivate;

    class AdrenalineAuthoringBaker : Baker<AdrenalineAuthoring>
    {
        public override void Bake(AdrenalineAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Adrenaline
            {
                SpeedMultiplier = authoring.SpeedMultiplier,
                Timer = authoring.TimerMax,
                TimerMax = authoring.TimerMax,
                BuffDuration = authoring.BuffDuration,
                CanActivate = authoring.CanActivate,
            });
        }
    }

}

public struct Adrenaline : IComponentData
{
    public float SpeedMultiplier;
    public float Timer;
    public float TimerMax;
    
    
    public bool CanActivate;
    public float BuffDuration;


}
