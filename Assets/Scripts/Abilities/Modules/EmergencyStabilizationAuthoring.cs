using Unity.Entities;
using UnityEngine;

public class EmergencyStabilizationAuthoring : MonoBehaviour
{
    public bool CanActivate = true;
    
    
    class EmergencyStabilizationAuthoringBaker : Baker<EmergencyStabilizationAuthoring>
    {
        public override void Bake(EmergencyStabilizationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EmergencyStabilization
            {
                CanActivate = authoring.CanActivate,
            });
        }
    }
    
}

public struct EmergencyStabilization : IComponentData 
{
    public bool CanActivate;


}
