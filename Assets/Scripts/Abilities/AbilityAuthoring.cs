using Unity.Entities;
using UnityEngine;

public class AbilityAuthoring : MonoBehaviour 
{
    public AbilityType Type;
    public GameObject Owner;          // ����, �������� ����������� �����������
    public bool Active;
    public bool IsTriggered;
    public AbilityTargetType TargetType;

    public SkillConfig ClassSkillConfig;


    public class baker : Baker<AbilityAuthoring>
    {
        public override void Bake(AbilityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Ability
            {
                Type = authoring.Type,
                Owner = GetEntity(authoring.Owner, TransformUsageFlags.Dynamic),
                TimeLeft = authoring.ClassSkillConfig.duration,

                CooldownLeft = authoring.ClassSkillConfig.cooldown,
                Active = authoring.Active,
                IsTriggered = authoring.IsTriggered,
                TargetType = authoring.TargetType,
                Cooldown = authoring.ClassSkillConfig.cooldown,
                Duration = authoring.ClassSkillConfig.cooldown,
            });
        }
    }
}

public struct Ability : IComponentData
{
    public AbilityType Type;
    public Entity Owner;          
    public float TimeLeft;
    public float CooldownLeft;
    public bool Active;
    public bool IsTriggered;
    public AbilityTargetType TargetType;

    public float Cooldown;
    public float Duration;
}

public enum AbilityTargetType
{
    Self,       // �� ����
    Ally,       // �� ���������
    Enemy,      // �� ������
    Area        // �������
}

public enum AbilityType
{
    AnabolicStimulator,
    AntiGravitationBarrier,
    Fireball,
    Heal,
    None 
}

