using System;
using Unity.Entities;
using UnityEngine;

public class AbilityEventListener : Singleton<AbilityEventListener>
{
    public event Action<Entity, AbilityType> AbilityStarted;
    public event Action<Entity, AbilityType> AbilityEnded;
    public event Action<Entity, AbilityType> CooldownEnded;

    public event Action<Vector3, Vector3> BulletShot;


    public void RaiseAbilityStarted(Entity owner, AbilityType type)
    {
        AbilityStarted?.Invoke(owner, type);
        Debug.Log("RaiseAbilityStarted \n" +
            owner + "\n"
            + type);
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
