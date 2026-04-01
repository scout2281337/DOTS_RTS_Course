using Unity.Entities;
using UnityEngine;


public class AnimationStateAuthoring : MonoBehaviour
{
    
    public class Baker : Baker<AnimationStateAuthoring>
    {
        public override void Bake(AnimationStateAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AnimationStateComponent
            {
                Value = 0
            });
        }
    }
}

public struct AnimationStateComponent : IComponentData
{
    public AnimationState Value;
}

public enum AnimationState
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    Jump = 3
}

public struct AttackRequest : IComponentData { }