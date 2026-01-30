using System;
using Unity.Entities;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class AbilityEventListener : Singleton<AbilityEventListener>
{
    public event Action<Entity, AbilityType> AbilityStarted;
    public event Action<Entity, AbilityType> AbilityEnded;
    public event Action<Entity, AbilityType> CooldownEnded;

    public event Action<Vector3, Vector3> BulletShot;
    public event Action<UnitClass, float> HealthChanged;

    public void RaiseAbilityStarted(Entity owner, AbilityType type)
    {
        AbilityStarted?.Invoke(owner, type);
        Debug.Log("RaiseAbilityStarted \n" +
            owner + "\n"
            + type);
    }
    public void RaiseOnHealthChanged(UnitClass TargetUnitClass, float currentHealth) 
    {
        HealthChanged?.Invoke(TargetUnitClass, currentHealth);
        Debug.Log("RaiseOnHealthChanged \n" +
            TargetUnitClass + "\n"
            + currentHealth);
    }
    public void RaiseAbilityEnded(Entity owner, AbilityType type)
    {
        AbilityEnded?.Invoke(owner, type);
        Debug.Log("Ивент конец абилки");
    }

    public void RaiseCooldownEnded(Entity owner, AbilityType type) 
    {
        CooldownEnded?.Invoke(owner, type);
        Debug.Log("RaiseCooldownEnded \n" +
            owner + "\n"
            + type);
    }

    public void RaiseBulletShot(Vector3 start, Vector3 end)
    {
        BulletShot?.Invoke(start, end);
        Debug.Log("BulletShot \n" +
            start + "\n"
            + end);
    }
}
