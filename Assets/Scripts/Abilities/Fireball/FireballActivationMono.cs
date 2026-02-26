using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FireballActivationMono : MonoBehaviour
{
    public float MaxRadius = 20f;

    public bool AbilityUseMode = false;

    EntityManager em;

    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            AbilityUseMode = !AbilityUseMode;
        }

        if (AbilityUseMode && Input.GetMouseButtonDown(0)) 
        {
            //LaunchFireball();
            Debug.Log("Спавн сработал");

        }
    }


    void LaunchFireball()
    {
        Entity arsonist = FriendlyUnitManager.Instance.EntitiesDictionary[UnitClass.Arsonist];

        if (!em.Exists(arsonist))
            return;

        if (!em.HasComponent<Ability>(arsonist))
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        float3 targetPos = hit.point;
        float3 unitPos = em.GetComponentData<LocalTransform>(arsonist).Position;

        float distance = Vector3.Distance(unitPos, targetPos);

        if (distance > MaxRadius)
            return;

        var ability = em.GetComponentData<Ability>(arsonist);
        ability.TargetPosition = targetPos;

        // проверка кулдауна
        if (ability.CooldownLeft > 0 || ability.Active)
            return;

        ability.IsTriggered = true;

        em.SetComponentData(arsonist, ability);
        Debug.Log("отправилась инфа в систему");
        AbilityUseMode = false;
    }


}
