using Unity.Entities;
using UnityEngine;



public class AbilityAuthoring : MonoBehaviour 
{
    public AbilityType Type;
    public GameObject Owner;          // Юнит, которому принадлежит способность
    public float TimeLeft;
    public float CooldownLeft;
    public bool Active;
    public bool IsTriggered;
    public AbilityTargetType TargetType;

    public class baker : Baker<AbilityAuthoring>
    {
        public override void Bake(AbilityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Ability
            {
                Type = authoring.Type,
                Owner = GetEntity(authoring.Owner, TransformUsageFlags.Dynamic),
                TimeLeft = authoring.TimeLeft,

                CooldownLeft = authoring.CooldownLeft,
                Active = authoring.Active,
                IsTriggered = authoring.IsTriggered,
                TargetType = authoring.TargetType,
            });
        }
    }

}

public struct Ability : IComponentData
{
    public AbilityType Type;
    public Entity Owner;          // Юнит, которому принадлежит способность
    public float TimeLeft;
    public float CooldownLeft;
    public bool Active;
    public bool IsTriggered;
    public AbilityTargetType TargetType;

}
public enum AbilityTargetType
{
    Self,       // на себя
    Ally,       // на союзников
    Enemy,      // на врагов
    Area        // область
}

public enum AbilityType
{
    AnabolikStimulator,
    AntiGravitationBarrier,
    Shield,
    Heal,
    None // Нихуя не делает
}

