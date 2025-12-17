using System;
using Unity.Entities;
using UnityEngine;

public class AbilityEventListener : MonoBehaviour
{
    public static AbilityEventListener Instance;

    public event Action<Entity, AbilityType> AbilityStarted;
    public event Action<Entity, AbilityType> AbilityEnded;


    void Awake()
    {
        Instance = this;
    }



    public void RaiseAbilityStarted(Entity owner, AbilityType type)
    {
        AbilityStarted?.Invoke(owner, type);
        Debug.Log("Сработало");
    }

    public void RaiseAbilityEnded(Entity owner, AbilityType type)
    {
        AbilityEnded?.Invoke(owner, type);
        Debug.Log("Сработало выкл");
    }
}
