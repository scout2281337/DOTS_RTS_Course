using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[RequireComponent(typeof(FogRevealSource))]
public class GeneratorActivity : MonoBehaviour
{
    [SerializeField] private float activationRadius = 4f;
    [SerializeField] private float activationDelay = 4f;
    [SerializeField] private bool requireUnitDuringActivation = true;
    [SerializeField] private float revealRadius = 24f;
    [SerializeField] private bool startsActivated;
    [SerializeField] private bool staysActivated = true;
    [SerializeField] private float activeDuration = 45f;
    [SerializeField] private bool reportOnStart = true;
    [SerializeField] private string discoveredLabel = "GENERATOR SIGNAL";
    [SerializeField] private string startingLabel = "GENERATOR STARTING";
    [SerializeField] private string activatedLabel = "GENERATOR ONLINE";
    [SerializeField] private string offlineLabel = "GENERATOR OFFLINE";

    private FogRevealSource revealSource;
    private EntityQuery friendlyUnitsQuery;
    private World cachedWorld;
    private bool hasQuery;
    private bool activated;
    private bool activationInProgress;
    private float activeTimer;
    private float activationTimer;

    [Header("Sounds")]


    public float ActivationRadius => activationRadius;
    public float RevealRadius => revealRadius;
    public bool IsActivated => activated;
    public bool IsActivationInProgress => activationInProgress;
    public float ActivationProgress01
    {
        get
        {
            if (!activationInProgress)
                return activated ? 1f : 0f;

            float delay = Mathf.Max(0.0001f, activationDelay);
            return Mathf.Clamp01(1f - activationTimer / delay);
        }
    }

    private void Awake()
    {
        revealSource = GetComponent<FogRevealSource>();
        revealSource.SetRadius(revealRadius);
        revealSource.SetRevealing(false);
    }

    private void Start()
    {
        if (reportOnStart)
        {
            WorldEventUtility.Report(
                WorldEventType.ObjectiveUpdated,
                transform.position,
                WorldEventImportance.Medium,
                WorldEventKnowledge.Approximate,
                revealRadius,
                5f,
                discoveredLabel);
        }

        if (startsActivated)
            Activate();
    }

    private void OnDestroy()
    {
        hasQuery = false;
    }

    private void Update()
    {
        if (activated)
        {
            if (!staysActivated)
            {
                activeTimer -= Time.deltaTime;
                if (activeTimer <= 0f)
                    Deactivate();
            }

            return;
        }

        bool hasFriendlyUnit = HasFriendlyUnitInActivationRadius();

        if (activationInProgress)
        {
            if (requireUnitDuringActivation && !hasFriendlyUnit)
            {
                CancelActivation();
                return;
            }

            activationTimer -= Time.deltaTime;
            if (activationTimer <= 0f)
                Activate();

            return;
        }

        if (hasFriendlyUnit)
            BeginActivation();
    }

    public void BeginActivation()
    {
        if (activated || activationInProgress)
            return;

        activationTimer = Mathf.Max(0f, activationDelay);
        if (activationTimer <= 0f)
        {
            Activate();
            return;
        }

        activationInProgress = true;

        WorldEventUtility.Report(
            WorldEventType.ObjectiveUpdated,
            transform.position,
            WorldEventImportance.Medium,
            WorldEventKnowledge.Exact,
            activationRadius,
            Mathf.Min(activationDelay, 5f),
            startingLabel);
    }

    public void Activate()
    {
        activated = true;
        activationInProgress = false;
        activationTimer = 0f;
        activeTimer = Mathf.Max(0.1f, activeDuration);
        revealSource.SetRadius(revealRadius);
        revealSource.SetRevealing(true);

        //GameAudioManager.Instance.Play3D

        WorldEventUtility.Report(
            WorldEventType.ObjectiveUpdated,
            transform.position,
            WorldEventImportance.High,
            WorldEventKnowledge.Exact,
            revealRadius,
            5f,
            activatedLabel);
    }

    public void Deactivate()
    {
        activated = false;
        activationInProgress = false;
        activationTimer = 0f;
        revealSource.SetRevealing(false);

        WorldEventUtility.Report(
            WorldEventType.ObjectiveUpdated,
            transform.position,
            WorldEventImportance.Medium,
            WorldEventKnowledge.Exact,
            revealRadius,
            4f,
            offlineLabel);
    }

    private void CancelActivation()
    {
        activationInProgress = false;
        activationTimer = 0f;
    }

    private bool HasFriendlyUnitInActivationRadius()
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;

        EnsureQuery(em);

        using var entities = friendlyUnitsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        float radiusSq = activationRadius * activationRadius;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                continue;

            Unit unit = em.GetComponentData<Unit>(entity);
            if (unit.faction != Faction.Friendly)
                continue;

            LocalTransform transformData = em.GetComponentData<LocalTransform>(entity);
            if (math.distancesq(transformData.Position, transform.position) <= radiusSq)
                return true;
        }

        return false;
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
            ComponentType.ReadOnly<LocalTransform>());
        hasQuery = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.25f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, revealRadius);
    }
}
