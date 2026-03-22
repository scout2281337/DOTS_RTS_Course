using Unity.Entities;
using UnityEngine;

namespace TMG.ECSAnimations
{
    public class PlayerGameObjectPrefab : IComponentData
    {
        public GameObject Value;
    }

    public class PlayerAnimatorReference : ICleanupComponentData
    {
        public Animator Value;
    }

    public class PlayerAnimatorAuthoring : MonoBehaviour
    {
        public GameObject PlayerGameObjectPrefab;

        public class PlayerGameObjectPrefabBaker : Baker<PlayerAnimatorAuthoring>
        {
            public override void Bake(PlayerAnimatorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new PlayerGameObjectPrefab { Value = authoring.PlayerGameObjectPrefab });
            }
        }
    }
}