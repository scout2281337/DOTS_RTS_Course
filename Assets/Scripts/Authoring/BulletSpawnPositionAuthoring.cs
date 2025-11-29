using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BulletSpawnPositionAuthoring : MonoBehaviour
{
    public Transform bulletSpawnPositionTransform;

    public class Baker : Baker<BulletSpawnPositionAuthoring>
    {
        public override void Bake(BulletSpawnPositionAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BulletSpawnPosition
            {
                
                bulletSpawnLocalPosition = authoring.bulletSpawnPositionTransform.localPosition,
            });
        }
    }
}

public struct BulletSpawnPosition : IComponentData
{
    public float3 bulletSpawnLocalPosition;
}
