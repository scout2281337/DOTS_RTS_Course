using System;
using Unity.Entities;
using UnityEngine;

public class AbilityEventListener : MonoBehaviour
{
    public static AbilityEventListener Instance;

    public event Action<Entity, AbilityType> AbilityStarted;
    public event Action<Entity, AbilityType> AbilityEnded;
    public event Action<Entity, AbilityType> CooldownEnded;



    void Awake()
    {
        Instance = this;//Доделать:)
    }



    public void RaiseAbilityStarted(Entity owner, AbilityType type)
    {
        AbilityStarted?.Invoke(owner, type);
        Debug.Log("Ивент старт абилки");
    }

    public void RaiseAbilityEnded(Entity owner, AbilityType type)
    {
        AbilityEnded?.Invoke(owner, type);
        Debug.Log("Ивент конец абилки");
    }

    public void RaiseCooldownEnded(Entity owner, AbilityType type) 
    {
        CooldownEnded?.Invoke(owner, type);
        Debug.Log("ивент конца кулдауна сработал:)");
    
    }
}
