using System;
using UnityEngine;

public class EventMediator : Singleton<EventMediator>
{
    public event Action<UnitClass, SoldierAttributesConfig> OnUnitSpawned;
    public event Action<DamageEvent> OnDamageReceived;
    public event Action<BulletShotEvent> OnBulletShot;
    public event Action<UnitClass, ModuleBaseSO> OnNewModule;

    public event Action<AbilityPointerEvent> OnAbilityPointer;
    public event Action<AbilityStartedEvent> OnAbilityStarted;
    public event Action<AbilityEndedEvent> OnAbilityEnded;
    public event Action<AbilityCooldownEndedEvent> OnCooldownEnded;


    public void InvokeUnitSpawned(UnitClass unitClass, SoldierAttributesConfig soldierConfig)
    {
        OnUnitSpawned?.Invoke(unitClass, soldierConfig);
        Debug.Log("InvokeUnitSpawned \n" +
            unitClass + "\n"
            + soldierConfig.bodyConfig + "\n"
            + soldierConfig.weaponConfigs[0] + "\n"
            + soldierConfig.skillConfigs[0]);
    }

    public void InvokeDamageReceived(DamageEvent evt)
    {
        OnDamageReceived?.Invoke(evt);
        Debug.Log("InvokeDamageReceived \n" +
            evt.TargetEntityClass + "\n"
            + evt.DamageAmount);
    }

    public void InvokeBulletShot(BulletShotEvent evt)
    {
        OnBulletShot?.Invoke(evt);
        //Debug.Log("OnBulletShot \n" +
        //    start + "\n"
        //    + end);
    }

    public void InvokeNewModule(UnitClass unit, ModuleBaseSO module)
    {
        OnNewModule?.Invoke(unit, module);
        Debug.Log("New module \n" +
            unit + "\n"
            + module);
    }

    public void InvokeAbilityPointer(AbilityPointerEvent evt)
    {
        OnAbilityPointer?.Invoke(evt);
        Debug.Log("InvokeAbilityPointer \n" +
            evt.Caster + "\n"
            + evt.Type);
    }

    public void InvokeAbilityStarted(AbilityStartedEvent evt)
    {
        OnAbilityStarted?.Invoke(evt);
        Debug.Log("InvokeAbilityStarted \n" +
            evt.Caster+ "\n"
            + evt.Type);
    }

    public void InvokeAbilityEnded(AbilityEndedEvent evt)
    {
        OnAbilityEnded?.Invoke(evt);
        Debug.Log("InvokeAbilityEnded \n" +
            evt.Caster + "\n"
            + evt.Type);
    }

    public void InvokeCooldownEnded(AbilityCooldownEndedEvent evt)
    {
        OnCooldownEnded?.Invoke(evt);
        Debug.Log("InvokeCooldownEnded \n" +
            evt.Caster+ "\n"
            + evt.Type);
    }
}