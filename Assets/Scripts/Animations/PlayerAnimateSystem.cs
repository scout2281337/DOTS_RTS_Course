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

                var animator = go.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Object.Destroy(go);
                    continue;
                }

                var animatorRef = new PlayerAnimatorReference
                {
                    Value = animator
                };

                GameObject visualRoot = animator.transform.root.gameObject;
                if (SystemAPI.HasComponent<LocalTransform>(entity))
                {
                    LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(entity);
                    visualRoot.transform.position = transform.Position;
                    visualRoot.transform.rotation = transform.Rotation;
                }

                bool isDead = SystemAPI.HasComponent<DeadUnit>(entity);
                bool hiddenByFog =
                    SystemAPI.HasComponent<FogRevealable>(entity) &&
                    SystemAPI.HasComponent<FogVisible>(entity) &&
                    !SystemAPI.IsComponentEnabled<FogVisible>(entity);

                visualRoot.SetActive(!isDead && !hiddenByFog);

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
                GameObject visualRoot = animator.transform.root.gameObject;
                if (visualRoot.activeSelf != shouldBeActive)
                {
                    visualRoot.SetActive(shouldBeActive);
                }

                visualRoot.transform.position = transform.Position;
                visualRoot.transform.rotation = transform.Rotation;

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
                    Object.Destroy(animatorRef.Value.transform.root.gameObject);
                }

                ecb.RemoveComponent<PlayerAnimatorReference>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
