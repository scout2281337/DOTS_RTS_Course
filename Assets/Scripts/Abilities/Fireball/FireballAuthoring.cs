using Unity.Entities;
using UnityEngine;

class FireballAuthoring : MonoBehaviour
{
    public float Duration;
    public float Damage;
    public float Radius;
    public float MaxDistance;
    public float TimerMax;
    public class baker : Baker<FireballAuthoring>
    {
        public override void Bake(FireballAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Fireball
            {
                Duration = authoring.Duration,
                Damage = authoring.Damage,
                Radius = authoring.Radius,
                MaxDistance = authoring.MaxDistance,
                TimerMax = authoring.TimerMax,
                Owner = Entity.Null
            });
            
        } 
    }
    
}
public struct Fireball : IComponentData 
{
    public float Duration;
    public float Damage;
    public float Radius;
    public float MaxDistance;
    public float Timer;
    public float TimerMax;
    public Entity Owner;

}

