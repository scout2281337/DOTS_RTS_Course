using System;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.Playables;
using UnityEngine;

public class AbilityEventListener : MonoBehaviour
{
    public static AbilityEventListener Instance;

    public event Action<Entity, AbilityType> AbilityStarted;
    public event Action<Entity, AbilityType> AbilityEnded;

    EntityManager em;
    EntityQuery startQuery;
    EntityQuery endQuery;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;

        startQuery = em.CreateEntityQuery(
            typeof(AbilityStartEvent)
            , typeof(Ability)
        );

        endQuery = em.CreateEntityQuery(
            typeof(AbilityEndEvent),
            typeof(Ability)
        );
    }

    void Update()
    {
        ListenStart();
        ListenEnd();
    }

    void ListenStart()
    {
        if (startQuery.IsEmpty) return;

        var abilities = startQuery.ToComponentDataArray<Ability>(Allocator.Temp);

        foreach (var ability in abilities)
        {
            AbilityStarted?.Invoke(ability.Owner, ability.Type);
            Debug.Log("Сработало ивентус");
        }

        abilities.Dispose();
    }

    void ListenEnd()
    {
        if (endQuery.IsEmpty) return;

        var abilities = endQuery.ToComponentDataArray<Ability>(Allocator.Temp);

        foreach (var ability in abilities)
        {
            AbilityEnded?.Invoke(ability.Owner, ability.Type);
            Debug.Log("Сработало ивентус");
        }

        abilities.Dispose();
    }
}
