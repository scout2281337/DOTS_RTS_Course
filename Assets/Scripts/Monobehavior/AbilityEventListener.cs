using System;
using Unity.Entities;
using UnityEngine;

public class AbilityEventListener : Singleton<AbilityEventListener>
{
    public event Action<Entity, AbilityType> OnAbilityStarted;
    public event Action<Entity, AbilityType> OnAbilityEnded;
    public event Action<Entity, AbilityType> OnCooldownEnded;

    public event Action<UnitClass, ClassConfig, WeaponConfig, SkillConfig> OnUnitSpawned;
    public event Action<UnitClass, float> OnHealthChanged;
    public event Action<Vector3, Vector3> OnBulletShot;


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

    public void InvokeUnitSpawned(UnitClass unitClass, 
        ClassConfig classConfig, WeaponConfig weaponConfig, SkillConfig skillConfig)
    {
        OnUnitSpawned?.Invoke(unitClass, classConfig, weaponConfig, skillConfig);
        Debug.Log("InvokeUnitSpawned \n" +
            unitClass + "\n"
            + classConfig + "\n"
            + weaponConfig + "\n"
            + skillConfig);
    }

    public void InvokeHealthChanged(UnitClass TargetUnitClass, float currentHealth)
    {
        OnHealthChanged?.Invoke(TargetUnitClass, currentHealth);
        Debug.Log("InvokeHealthChanged \n" +
            TargetUnitClass + "\n"
            + currentHealth);
    }

    public void InvokeBulletShot(Vector3 start, Vector3 end)
    {
        OnBulletShot?.Invoke(start, end);
        Debug.Log("OnBulletShot \n" +
            start + "\n"
            + end);
    }
}
