using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Presenter : Singleton<Presenter>
{
    public readonly List<Action> OnAbilityPress = new();
    private event Action OnEscBuffer;


    protected override void Awake()
    {
        base.Awake();
        EnsureDefaultAbilityBindings();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) 
        {
            InvokeAbilityPress(0);
            Debug.Log(" Абилка рейдера сработала ");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            InvokeAbilityPress(1);
            Debug.Log(" Абилка танка сработала ");
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            InvokeAbilityPress(2);
            Debug.Log(" Абилка огнемеьчика сработала ");
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            InvokeAbilityPress(3);
            Debug.Log(" Абилка снайпера сработала ");
        }
    }
    public void InvokeEscBuffer()
    {
        OnEscBuffer?.Invoke();
        Debug.Log("InvokeEscBuffer \n" +
            OnEscBuffer);
    }

    public void InvokeAbilityPress(int i)
    {
        EnsureDefaultAbilityBindings();
        if (i < 0 || i >= OnAbilityPress.Count)
        {
            Debug.LogWarning($"InvokeAbilityPress: invalid slot index {i}");
            return;
        }

        OnAbilityPress[i]?.Invoke();
        Debug.Log("InvokeAbilityPress \n" +
            i + "\n" +
            OnAbilityPress[i]);
    }

    private void EnsureDefaultAbilityBindings()
    {
        EnsureAbilitySlot(3);

        if (OnAbilityPress[0] == null)
            OnAbilityPress[0] = () => TriggerSelfAbility(UnitClass.Raider);
        if (OnAbilityPress[1] == null)
            OnAbilityPress[1] = () => TriggerTargetedAbility(UnitClass.Juggernaut);
        if (OnAbilityPress[2] == null)
            OnAbilityPress[2] = () => TriggerTargetedAbility(UnitClass.Arsonist);
        if (OnAbilityPress[3] == null)
            OnAbilityPress[3] = () => TriggerTargetedAbility(UnitClass.Sniper);
    }

    private void EnsureAbilitySlot(int slotIndex)
    {
        while (OnAbilityPress.Count <= slotIndex)
        {
            OnAbilityPress.Add(null);
        }
    }

    private void TriggerSelfAbility(UnitClass unitClass)
    {
        if (!TryGetAbility(unitClass, out var entityManager, out var entity, out var ability))
        {
            Debug.LogWarning($"TriggerSelfAbility: failed to resolve ability for {unitClass}");
            return;
        }

        if (ability.Active || ability.CooldownLeft > 0f)
        {
            Debug.Log($"TriggerSelfAbility: {unitClass} cannot cast. Active={ability.Active}, CooldownLeft={ability.CooldownLeft}");
            return;
        }
        
        if (entityManager.HasComponent<LocalTransform>(entity))
        {
            var localTransform = entityManager.GetComponentData<LocalTransform>(entity);

            if (ability.Type == AbilityType.Barricade)
            {
                ability.TargetPosition = localTransform.Position + math.forward(localTransform.Rotation) * 2.5f;
            }
            else if (ability.TargetType == AbilityTargetType.Self)
            {
                ability.TargetPosition = localTransform.Position;
            }
        }

        ability.IsTriggered = true;
        entityManager.SetComponentData(entity, ability);
        Debug.Log($"TriggerSelfAbility: ability flagged for {unitClass} on entity {entity}");
    }

    private void TriggerTargetedAbility(UnitClass unitClass)
    {
        var targetingService = AbilityTargetingServiceMono.Instance;
        if (targetingService != null && targetingService.StartTargeting(unitClass))
        {
            Debug.Log($"TriggerTargetedAbility: targeting mode enabled for {unitClass}");
            return;
        }

        Debug.LogWarning($"TriggerTargetedAbility: failed to start targeting mode for {unitClass}");
    }

    private static bool TryGetAbility(UnitClass unitClass, out EntityManager entityManager, out Entity entity, out Ability ability)
    {
        entityManager = default;
        entity = Entity.Null;
        ability = default;

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            Debug.LogWarning("TryGetAbility: DefaultGameObjectInjectionWorld is null");
            return false;
        }
        if (FriendlyUnitManager.Instance == null)
        {
            Debug.LogWarning("TryGetAbility: FriendlyUnitManager.Instance is null");
            return false;
        }
        if (!FriendlyUnitManager.Instance.unitEntityDict.TryGetValue(unitClass, out entity))
        {
            Debug.LogWarning($"TryGetAbility: entity for {unitClass} not found in unitEntityDict");
            return false;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<Ability>(entity))
        {
            Debug.LogWarning($"TryGetAbility: entity {entity} for {unitClass} is invalid or has no Ability component");
            return false;
        }

        ability = entityManager.GetComponentData<Ability>(entity);
        return true;
    }
}
