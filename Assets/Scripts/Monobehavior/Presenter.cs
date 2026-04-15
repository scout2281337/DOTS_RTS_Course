using System;
using System.Collections.Generic;
using Unity.Entities;
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
            return;

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
            OnAbilityPress[1] = () => TriggerSelfAbility(UnitClass.Juggernaut);
        if (OnAbilityPress[2] == null)
            OnAbilityPress[2] = TriggerArsonistAbility;
        if (OnAbilityPress[3] == null)
            OnAbilityPress[3] = () => TriggerSelfAbility(UnitClass.Sniper);
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
            return;

        if (ability.Active || ability.CooldownLeft > 0f)
            return;

        ability.IsTriggered = true;
        entityManager.SetComponentData(entity, ability);
    }

    private void TriggerArsonistAbility()
    {
        var fireballActivation = FindFirstObjectByType<FireballActivationMono>();
        if (fireballActivation != null)
        {
            fireballActivation.AbilityUseMode = true;
            return;
        }

        TriggerSelfAbility(UnitClass.Arsonist);
    }

    private static bool TryGetAbility(UnitClass unitClass, out EntityManager entityManager, out Entity entity, out Ability ability)
    {
        entityManager = default;
        entity = Entity.Null;
        ability = default;

        if (World.DefaultGameObjectInjectionWorld == null)
            return false;
        if (FriendlyUnitManager.Instance == null)
            return false;
        if (!FriendlyUnitManager.Instance.unitEntityDict.TryGetValue(unitClass, out entity))
            return false;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<Ability>(entity))
            return false;

        ability = entityManager.GetComponentData<Ability>(entity);
        return true;
    }
}
