using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PathNodeAuthoring : MonoBehaviour
{
    public class Baker : Baker<PathNodeAuthoring> 
    {
        

        public override void Bake(PathNodeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PathNode
            {


            });
        }
    }    
}

public struct PathNode : IComponentData 
{
    public int2 NodePosition;

    public int index;

    public int gCost;
    public int hCost;
    public int fCost;

    public bool isWalkable;
    public int cameFromNodeIndex;

}