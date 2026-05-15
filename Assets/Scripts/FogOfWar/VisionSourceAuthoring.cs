using Unity.Entities;
using UnityEngine;

public class VisionSourceAuthoring : MonoBehaviour
{
    public float radius = 12f;
    [Range(1f, 360f)] public float angleDegrees = 130f;

    public class Baker : Baker<VisionSourceAuthoring>
    {
        public override void Bake(VisionSourceAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VisionSource
            {
                Radius = authoring.radius,
                AngleDegrees = authoring.angleDegrees
            });
        }
    }
}
