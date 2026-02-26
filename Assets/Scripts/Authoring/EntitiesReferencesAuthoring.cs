using Unity.Entities;
using UnityEngine;

public class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject zombiePrefabGameObject;
    public GameObject unitPrefabGameObject;
    public GameObject AntiGravitationBarrier;
    public GameObject FireballPrefab;

    public class Baker : Baker<EntitiesReferencesAuthoring>
    {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences 
            {
                zombiePrefabEntity = GetEntity(authoring.zombiePrefabGameObject, TransformUsageFlags.Dynamic),
                unitPrefabEntity = GetEntity(authoring.unitPrefabGameObject, TransformUsageFlags.Dynamic),
                AntiGravitationBarrier = GetEntity(authoring.AntiGravitationBarrier, TransformUsageFlags.Dynamic),
                FireballPrefabEntity = GetEntity(authoring.FireballPrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct EntitiesReferences : IComponentData 
{
    public Entity zombiePrefabEntity; //
    public Entity unitPrefabEntity;
    public Entity AntiGravitationBarrier;

    public Entity FireballPrefabEntity;
}
