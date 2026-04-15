using System;
using Unity.Entities;
using UnityEngine;

public class AbilityEventListener : Singleton<AbilityEventListener>
{
    public event Action<Entity, AbilityType> OnAbilityStarted;
    public event Action<Entity, AbilityType> OnAbilityEnded;
    public event Action<Entity, AbilityType> OnCooldownEnded;

    public event Action<UnitClass, SoldierAttributesConfig> OnUnitSpawned;
    public event Action<UnitClass, float> OnHealthChanged;
    public event Action<Vector3, Vector3> OnBulletShot;
    public event Action<UnitClass, ModuleBaseSO> OnNewModule;


    public void InvokeAbilityStarted(Entity owner, AbilityType type)
    {
        OnAbilityStarted?.Invoke(owner, type);
        Debug.Log("InvokeAbilityStarted \n" +
            owner + "\n"
            + type);
    }

    public void InvokeAbilityEnded(Entity owner, AbilityType type)
    {
        OnAbilityEnded?.Invoke(owner, type);
        Debug.Log("InvokeAbilityEnded \n" +
            owner + "\n"
            + type);
    }

    public void InvokeCooldownEnded(Entity owner, AbilityType type) 
    {
        OnCooldownEnded?.Invoke(owner, type);
        Debug.Log("InvokeCooldownEnded \n" +
            owner + "\n"
            + type);
    }

    public void InvokeUnitSpawned(UnitClass unitClass, SoldierAttributesConfig soldierConfig)
    {
        OnUnitSpawned?.Invoke(unitClass, soldierConfig);
        Debug.Log("InvokeUnitSpawned \n" +
            unitClass + "\n"
            + soldierConfig.bodyConfig + "\n"
            + soldierConfig.weaponConfigs[0] + "\n"
            + soldierConfig.skillConfigs[0]);
    }

    public void InvokeHealthChanged(UnitClass TargetUnitClass, float healthDelta)
    {
        OnHealthChanged?.Invoke(TargetUnitClass, healthDelta);
        Debug.Log("InvokeHealthChanged \n" +
            TargetUnitClass + "\n"
            + healthDelta);
    }

    public void InvokeBulletShot(Vector3 start, Vector3 end)
    {
        OnBulletShot?.Invoke(start, end);
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
}
