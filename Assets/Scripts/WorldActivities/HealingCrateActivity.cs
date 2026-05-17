using System;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class HealingCrateActivity : MonoBehaviour
{
    [SerializeField] private float activationRadius = 3.5f;
    [SerializeField] private float healAmount = 45f;
    [SerializeField] private bool healWholeSquadInRadius = true;
    [SerializeField] private bool consumeAfterUse = true;
    [SerializeField] private float consumeDelay = 0.75f;
    [SerializeField] private bool reportOnStart = true;
    [SerializeField] private string discoveredLabel = "MEDICAL CACHE";
    [SerializeField] private string usedLabel = "SQUAD HEALED";

    private bool used;
    private EntityQuery friendlyUnitsQuery;
    private World cachedWorld;
    private bool hasQuery;

    public event Action OnCrateUsed;
    public float ActivationRadius => activationRadius;
    public bool IsUsed => used;
    public float ConsumeDelay => consumeDelay;

    private void Start()
    {
        if (reportOnStart)
        {
            WorldEventUtility.Report(
                WorldEventType.ResourceFound,
                transform.position,
                WorldEventImportance.Low,
                WorldEventKnowledge.Approximate,
                activationRadius,
                4f,
                discoveredLabel);
        }
    }

    private void OnDestroy()
    {
        hasQuery = false;
    }

    private void Update()
    {
        if (used || !TryGetEntityManager(out EntityManager em))
            return;

        EnsureQuery(em);

        using var entities = friendlyUnitsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        bool healedAny = false;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                continue;

            Unit unit = em.GetComponentData<Unit>(entity);
            if (unit.faction != Faction.Friendly)
                continue;

            LocalTransform transformData = em.GetComponentData<LocalTransform>(entity);
            if (math.distancesq(transformData.Position, transform.position) > activationRadius * activationRadius)
                continue;

            if (TryHeal(em, entity))
                healedAny = true;

            if (!healWholeSquadInRadius)
                break;
        }

        if (!healedAny)
            return;

        used = true;
        OnCrateUsed?.Invoke();

        WorldEventUtility.Report(
            WorldEventType.ResourceFound,
            transform.position,
            WorldEventImportance.Medium,
            WorldEventKnowledge.Exact,
            activationRadius,
            3f,
            usedLabel);

        if (consumeAfterUse)
            StartCoroutine(DeactivateAfterUse());
    }

    private IEnumerator DeactivateAfterUse()
    {
        float delay = Mathf.Max(0f, consumeDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        gameObject.SetActive(false);
    }

    private bool TryHeal(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<Health>(entity))
            return false;

        Health health = em.GetComponentData<Health>(entity);
        if (health.healthAmount >= health.healthAmountMax)
            return false;

        health.healthAmount = math.min(health.healthAmountMax, health.healthAmount + healAmount);
        health.OnHealthChanged = true;
        em.SetComponentData(entity, health);
        return true;
    }

    private bool TryGetEntityManager(out EntityManager em)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            em = default;
            return false;
        }

        em = world.EntityManager;
        return true;
    }

    private void EnsureQuery(EntityManager em)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (hasQuery && cachedWorld == world)
            return;

        cachedWorld = world;
        friendlyUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadWrite<Health>());
        hasQuery = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 1f, 0.55f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
