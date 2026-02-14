using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class UnitMoverAuthoring : MonoBehaviour
{
    public float CurrentMoveSpeed;
    public float BaseSpeed;
    public float rotationSpeed;
    public float separationRadius;
    public float separationForce;

    public class Baker : Baker<UnitMoverAuthoring>
    {
        public override void Bake(UnitMoverAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitMover {
                CurrentMoveSpeed = authoring.BaseSpeed,
                BaseSpeed = authoring.BaseSpeed, 
                rotationSpeed = authoring.rotationSpeed,
                separationForce = authoring.separationForce,
                separationRadius = authoring.separationRadius,

            });
        }
    }
}

public struct UnitMover : IComponentData
{
    public float CurrentMoveSpeed;
    public float BaseSpeed;
    public float rotationSpeed;
    public float3 targetPosition;
    public float separationRadius;
    public float separationForce;
}