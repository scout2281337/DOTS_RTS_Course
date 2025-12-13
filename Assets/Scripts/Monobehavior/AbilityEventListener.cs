using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class AbilityEventListener : MonoBehaviour
{
    EntityManager em;
    EntityQuery startQuery;
    EntityQuery endQuery;

    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;

        startQuery = em.CreateEntityQuery(
            typeof(Ability),
            typeof(AbilityStartEvent)
        );

        endQuery = em.CreateEntityQuery(
            typeof(Ability),
            typeof(AbilityEndEvent)
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
            OnAbilityStart(ability);
            //Ивент при старте абилки
        }

        abilities.Dispose();
    }

    void ListenEnd()
    {
        if (endQuery.IsEmpty) return;

        var abilities = endQuery.ToComponentDataArray<Ability>(Allocator.Temp);

        foreach (var ability in abilities)
        {
            OnAbilityEnd(ability);
            //Ивент в конце абилки
        }

        abilities.Dispose();
    }

    void OnAbilityStart(Ability ability) //нужно не нужно
    {
        switch (ability.Type)
        {
            case AbilityType.AnabolikStimulator:
                Debug.Log("Speed boost start by " + ability.Owner);
                break;
        }
    }

    void OnAbilityEnd(Ability ability)//нужно не нужно
    {
        switch (ability.Type)
        {
            case AbilityType.AnabolikStimulator:
                Debug.Log("Speed boost end");
                break;
        }
    }
}
