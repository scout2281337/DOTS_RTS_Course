using Unity.Entities;
using UnityEngine;

public class AbilityAuthoring : MonoBehaviour 
{
    public AbilityType Type;
    public GameObject Owner;          // ����, �������� ����������� �����������
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
    public Entity Owner;          // ����, �������� ����������� �����������
    public float TimeLeft;
    public float CooldownLeft;
    public bool Active;
    public bool IsTriggered;
    public AbilityTargetType TargetType;
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
    Shield,
    Heal,
    None // ����� �� ������
}

