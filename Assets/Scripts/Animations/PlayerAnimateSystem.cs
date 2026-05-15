using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TMG.ECSAnimations
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PlayerAnimateSystem : ISystem
    {
        private static readonly int StateHash = Animator.StringToHash("State");

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Create Animator
            foreach (var (prefab, entity) in
                     SystemAPI.Query<PlayerGameObjectPrefab>()
                     .WithNone<PlayerAnimatorReference>()
                     .WithEntityAccess())
            {
                if (prefab.Value == null)
                    continue;

                var go = Object.Instantiate(prefab.Value);
                if (go == null)
                    continue;

                var animator = go.GetComponent<Animator>();
                if (animator == null)
                {
                    Object.Destroy(go);
                    continue;
                }

                var animatorRef = new PlayerAnimatorReference
                {
                    Value = animator
                };

                ecb.AddComponent(entity, animatorRef);
            }

            // Update position + animation
            foreach (var (transform, animatorRef, animState, entity) in
                     SystemAPI.Query<LocalTransform, PlayerAnimatorReference, AnimationStateComponent>()
                         .WithEntityAccess())
            {
                var animator = animatorRef.Value;
                if (animator == null)
                {
                    ecb.RemoveComponent<PlayerAnimatorReference>(entity);
                    continue;
                }

                bool isDead = SystemAPI.HasComponent<DeadUnit>(entity);
                bool hiddenByFog =
                    SystemAPI.HasComponent<FogRevealable>(entity) &&
                    SystemAPI.HasComponent<FogVisible>(entity) &&
                    !SystemAPI.IsComponentEnabled<FogVisible>(entity);

                bool shouldBeActive = !isDead && !hiddenByFog;
                if (animator.gameObject.activeSelf != shouldBeActive)
                {
                    animator.gameObject.SetActive(shouldBeActive);
                }

                animator.transform.position = transform.Position;
                animator.transform.rotation = transform.Rotation;

                if (shouldBeActive)
                {
                    animator.SetInteger(StateHash, (int)animState.Value);
                }
            }

            // Cleanup visual object for destroyed/invalid entities
            foreach (var (animatorRef, entity) in
                     SystemAPI.Query<PlayerAnimatorReference>()
                     .WithNone<PlayerGameObjectPrefab, LocalTransform>()
                     .WithEntityAccess())
            {
                if (animatorRef.Value != null)
                {
                    Object.Destroy(animatorRef.Value.gameObject);
                }

                ecb.RemoveComponent<PlayerAnimatorReference>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
