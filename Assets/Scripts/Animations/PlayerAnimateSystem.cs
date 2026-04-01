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

            // Создание Animator
            foreach (var (prefab, entity) in
                     SystemAPI.Query<PlayerGameObjectPrefab>()
                     .WithNone<PlayerAnimatorReference>()
                     .WithEntityAccess())
            {
                var go = Object.Instantiate(prefab.Value);

                var animatorRef = new PlayerAnimatorReference
                {
                    Value = go.GetComponent<Animator>()
                };

                ecb.AddComponent(entity, animatorRef);
            }

            // Обновление позиции + анимации
            foreach (var (transform, animatorRef, animState) in
                     SystemAPI.Query<LocalTransform, PlayerAnimatorReference, AnimationStateComponent>())
            {
                var animator = animatorRef.Value;

                // позиция
                animator.transform.position = transform.Position;
                animator.transform.rotation = transform.Rotation;

                // ВАЖНО: один параметр
                animator.SetInteger(StateHash, (int)animState.Value);
            }

            // Удаление
            foreach (var (animatorRef, entity) in
                     SystemAPI.Query<PlayerAnimatorReference>()
                     .WithNone<PlayerGameObjectPrefab, LocalTransform>()
                     .WithEntityAccess())
            {
                Object.Destroy(animatorRef.Value.gameObject);
                ecb.RemoveComponent<PlayerAnimatorReference>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}