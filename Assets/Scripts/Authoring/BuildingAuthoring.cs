using Unity.Entities;
using UnityEngine;

public class BuildingAuthoring : MonoBehaviour
{
    public class Baker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Building());
        }
    }
}

public struct Building : IComponentData 
{

}