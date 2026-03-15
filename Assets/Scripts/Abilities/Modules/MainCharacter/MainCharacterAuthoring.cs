using Unity.Entities;
using UnityEngine;


public class MainCharacterAuthoring : MonoBehaviour
{
    public float FireRateBoost;
    public float MaxPercent;
    public float Range;
    public float FireRatePerscentBoost;

    public class baker : Baker<MainCharacterAuthoring>
    {
        public override void Bake(MainCharacterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MainCharacter
            {
                FireRateBoost = authoring.FireRateBoost,
                MaxPercent = authoring.MaxPercent,
                Range= authoring.Range,
                FireRatePerscentBoost = authoring.FireRatePerscentBoost,
            });
            ;
        }
    }
}

public struct MainCharacter : IComponentData
{
    public float FireRateBoost;
    public float FireRatePerscentBoost;
    public float MaxPercent;
    public float Range;
}
