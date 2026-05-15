using Unity.Entities;
using UnityEngine;

public class FogRevealableAuthoring : MonoBehaviour
{
    public bool visibleOnStart;

    public class Baker : Baker<FogRevealableAuthoring>
    {
        public override void Bake(FogRevealableAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<FogRevealable>(entity);
            AddComponent<FogVisible>(entity);
            SetComponentEnabled<FogVisible>(entity, authoring.visibleOnStart);
        }
    }
}
