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
        

        if (AbilityUseMode && Input.GetMouseButtonDown(0))
        {
            LaunchFireball();
            //Debug.Log("Спавн сработал");

        }
    }


    void LaunchFireball()
    {
        Entity arsonist = FriendlyUnitManager.Instance.unitEntityDict[UnitClass.Arsonist];

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
        bool canUseByCooldown = ability.CooldownLeft <= 0f;
        bool canUseByExtraBattery = false;
        if (!canUseByCooldown && em.HasComponent<ExtraBatteryModule>(arsonist))
        {
            var battery = em.GetComponentData<ExtraBatteryModule>(arsonist);
            canUseByExtraBattery = battery.Charges > 0;
        }

        if (ability.Active || (!canUseByCooldown && !canUseByExtraBattery))
            return;

        ability.IsTriggered = true;

        em.SetComponentData(arsonist, ability);
        //Debug.Log("отправилась инфа в систему");
        AbilityUseMode = false;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (em == default)
            return;

        if (FriendlyUnitManager.Instance == null)
            return;

        if (!FriendlyUnitManager.Instance.unitEntityDict.ContainsKey(UnitClass.Arsonist))
            return;

        Entity arsonist = FriendlyUnitManager.Instance.unitEntityDict[UnitClass.Arsonist];

        if (!em.Exists(arsonist))
            return;

        if (!em.HasComponent<LocalTransform>(arsonist))
            return;

        float3 unitPos = em.GetComponentData<LocalTransform>(arsonist).Position;
        Vector3 worldPos = new Vector3(unitPos.x, unitPos.y, unitPos.z);

        // Радиус способности
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(worldPos, MaxRadius);

        if (AbilityUseMode)
        {
            // Луч из камеры
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 targetPos = hit.point;

                // Точка цели
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPos, 0.4f);

                // Линия к цели
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(worldPos, targetPos);
            }
        }
    }
#endif

}