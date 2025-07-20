using Unity.Entities;
using UnityEngine;

public class CubeAuthoring : MonoBehaviour
{
    public class Baker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Cube());
            AddComponent(entity, new NewSpawn());
        }
    }
}
public struct Cube : IComponentData 
{

}
public struct NewSpawn : IComponentData
{
}