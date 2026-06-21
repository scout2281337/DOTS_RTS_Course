using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class AbilityTargetingServiceMono : Singleton<AbilityTargetingServiceMono>, IAbilityTargetingService
{
    [Header("Input")]
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

    [Header("Visual")]
    [SerializeField] private float targetMarkerRadius = 0.35f;
    [SerializeField] private Color rangeColor = new(0f, 1f, 1f, 0.5f);
    [SerializeField] private Color validTargetColor = new(0f, 1f, 0f, 0.7f);
    [SerializeField] private Color invalidTargetColor = new(1f, 0f, 0f, 0.7f);
    [SerializeField] private Color lineColor = new(1f, 0.92f, 0.016f, 0.8f);

    [Header("Fallback")]
    [SerializeField] private float fallbackRange = 10f;

    [Header("Runtime Visuals")]
    [SerializeField] private bool enableRuntimeVisuals = false;
    [SerializeField] private float visualLineWidth = 0.06f;
    [SerializeField] private int rangeSegments = 48;

    private EntityManager entityManager;
    private FriendlyUnitManager friendlyUnitManager;

    private LineRenderer rangeRenderer;
    private LineRenderer aimLineRenderer;
    private MeshRenderer targetMarkerRenderer;
    private Transform targetMarkerTransform;

    private bool isTargeting;
    private UnitClass activeUnitClass;

    public bool IsTargeting => isTargeting;
    public UnitClass ActiveUnitClass => activeUnitClass;

    private void Start()
    {
        TryInitializeEntityManager();
        EnsureRuntimeVisualObjects();
        SetRuntimeVisualVisible(false);
    }

    private void Update()
    {
        if (!isTargeting)
        {
            SetRuntimeVisualVisible(false);
            return;
        }

        if (TryBuildCastContext(activeUnitClass, out _, out _, out var casterTransform, out var castRange))
        {
            UpdateRuntimeVisuals(casterTransform.Position, castRange);
        }
        else
        {
            CancelTargeting();
            return;
        }

        if (Input.GetKeyDown(cancelKey) || Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryCommitTargetPoint();
        }
    }

    public bool StartTargeting(UnitClass unitClass)
    {
        if (!TryBuildCastContext(unitClass, out var caster, out var ability, out var casterTransform, out var castRange))
            return false;

        activeUnitClass = unitClass;
        isTargeting = true;
        EnsureRuntimeVisualObjects();
        float effectRadius = ability.Area > 0f ? ability.Area : ResolveSkillArea(unitClass);
        PublishPointerStarted(caster, ability, casterTransform.Position, castRange, effectRadius);
        return true;
    }

    public void CancelTargeting()
    {
        if (isTargeting)
        {
            PublishPointerEnded(activeUnitClass);
        }

        isTargeting = false;
        SetRuntimeVisualVisible(false);
    }

    private bool TryCommitTargetPoint()
    {
        if (!TryBuildCastContext(activeUnitClass, out var caster, out var ability, out var casterTransform, out var castRange))
        {
            CancelTargeting();
            return false;
        }

        if (!TryGetMouseWorldPoint(out var targetPoint))
            return false;

        float distance = math.distance(casterTransform.Position, targetPoint);
        if (distance > castRange)
            return false;

        ability.TargetPosition = targetPoint;
        ability.IsTriggered = true;
        entityManager.SetComponentData(caster, ability);

        CancelTargeting();
        return true;
    }

    private bool TryBuildCastContext(UnitClass unitClass, out Entity caster, out Ability ability, out LocalTransform casterTransform, out float castRange)
    {
        caster = Entity.Null;
        ability = default;
        casterTransform = default;
        castRange = fallbackRange;

        if (!TryInitializeEntityManager())
            return false;

        if (!TryResolveUnitEntity(unitClass, out caster))
            return false;

        if (!entityManager.Exists(caster) ||
            entityManager.HasComponent<DeadUnit>(caster) ||
            !entityManager.HasComponent<Ability>(caster))
        {
            return false;
        }

        ability = entityManager.GetComponentData<Ability>(caster);
        if (!CanActivateAbility(caster, ability))
            return false;

        if (!entityManager.HasComponent<LocalTransform>(caster))
            return false;

        casterTransform = entityManager.GetComponentData<LocalTransform>(caster);

        if (TryResolveSkillRange(unitClass, out var rangeFromConfig))
        {
            castRange = rangeFromConfig;
        }

        return true;
    }

    private bool TryResolveUnitEntity(UnitClass unitClass, out Entity entity)
    {
        entity = Entity.Null;

        if (!TryGetFriendlyUnitManager(out var manager))
            return false;

        return manager.unitEntityDict.TryGetValue(unitClass, out entity);
    }

    private bool TryResolveSkillRange(UnitClass unitClass, out float range)
    {
        range = fallbackRange;

        if (!TryGetFriendlyUnitManager(out var manager))
            return false;

        SoldierAttributesConfig config = unitClass switch
        {
            UnitClass.Raider => manager.raiderConfig,
            UnitClass.Arsonist => manager.arsonistConfig,
            UnitClass.Juggernaut => manager.juggernautConfig,
            UnitClass.Sniper => manager.sniperConfig,
            _ => null
        };

        if (config == null || config.skillConfigs == null || config.skillConfigs.Length == 0 || config.skillConfigs[0] == null)
            return false;

        range = math.max(0f, config.skillConfigs[0].range);
        return range > 0f;
    }

    private float ResolveSkillArea(UnitClass unitClass)
    {
        if (!TryGetFriendlyUnitManager(out var manager))
            return 0f;

        SoldierAttributesConfig config = unitClass switch
        {
            UnitClass.Raider => manager.raiderConfig,
            UnitClass.Arsonist => manager.arsonistConfig,
            UnitClass.Juggernaut => manager.juggernautConfig,
            UnitClass.Sniper => manager.sniperConfig,
            _ => null
        };

        if (config == null || config.skillConfigs == null || config.skillConfigs.Length == 0 || config.skillConfigs[0] == null)
            return 0f;

        return math.max(0f, config.skillConfigs[0].area);
    }

    private static AbilityPointerType ResolvePointerType(AbilityType abilityType)
    {
        return abilityType switch
        {
            AbilityType.Barricade => AbilityPointerType.PointFromCaster,
            AbilityType.Scorcher => AbilityPointerType.PointFromCaster,
            AbilityType.Gauss => AbilityPointerType.LineFromCaster,
            _ => AbilityPointerType.None
        };
    }

    private bool CanActivateAbility(Entity caster, in Ability ability)
    {
        if (ability.Active)
            return false;

        if (ability.CooldownLeft <= 0f)
            return true;

        if (!entityManager.HasComponent<ExtraBatteryModule>(caster))
            return false;

        var battery = entityManager.GetComponentData<ExtraBatteryModule>(caster);
        return battery.Charges > 0;
    }

    private bool TryInitializeEntityManager()
    {
        if (entityManager != default)
            return true;

        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return true;
    }

    private void PublishPointerStarted(Entity caster, in Ability ability, float3 startPosition, float range, float area)
    {
        if (!TryInitializeEntityManager())
            return;

        var hubQuery = entityManager.CreateEntityQuery(typeof(EventHub));
        if (hubQuery.IsEmptyIgnoreFilter)
            return;

        AbilityPointerType pointerType = ResolvePointerType(ability.Type);
        if (pointerType == AbilityPointerType.None)
            return;

        var hubEntity = hubQuery.GetSingletonEntity();
        var pointerEvents = entityManager.GetBuffer<AbilityPointerEvent>(hubEntity);
        pointerEvents.Add(new AbilityPointerEvent
        {
            Caster = caster,
            Type = ability.Type,
            PointerType = pointerType,
            Range = range,
            Area = area,
            Start = startPosition
        });
    }

    private void PublishPointerEnded(UnitClass unitClass)
    {
        if (!TryInitializeEntityManager())
            return;

        var hubQuery = entityManager.CreateEntityQuery(typeof(EventHub));
        if (hubQuery.IsEmptyIgnoreFilter)
            return;

        Entity caster = Entity.Null;
        AbilityType type = AbilityType.None;

        if (TryResolveUnitEntity(unitClass, out caster) && entityManager.Exists(caster) && entityManager.HasComponent<Ability>(caster))
        {
            type = entityManager.GetComponentData<Ability>(caster).Type;
        }

        var hubEntity = hubQuery.GetSingletonEntity();
        var pointerEndedEvents = entityManager.GetBuffer<AbilityPointerEndedEvent>(hubEntity);
        pointerEndedEvents.Add(new AbilityPointerEndedEvent
        {
            Caster = caster,
            Type = type
        });
    }

    private bool TryGetFriendlyUnitManager(out FriendlyUnitManager manager)
    {
        if (friendlyUnitManager == null)
        {
            friendlyUnitManager = FindFirstObjectByType<FriendlyUnitManager>();
        }

        manager = friendlyUnitManager;
        return manager != null;
    }

    private static bool TryGetMouseWorldPoint(out float3 point)
    {
        point = default;

        var cameraMain = Camera.main;
        if (cameraMain == null)
            return false;

        Ray ray = cameraMain.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return false;

        point = hit.point;
        return true;
    }

    private void EnsureRuntimeVisualObjects()
    {
        if (!enableRuntimeVisuals)
            return;

        if (rangeRenderer != null && aimLineRenderer != null && targetMarkerTransform != null)
            return;

        var spriteShader = Shader.Find("Sprites/Default");

        if (rangeRenderer == null)
        {
            var rangeGo = new GameObject("AbilityRangeCircle");
            rangeGo.transform.SetParent(transform, false);

            rangeRenderer = rangeGo.AddComponent<LineRenderer>();
            rangeRenderer.loop = true;
            rangeRenderer.useWorldSpace = true;
            rangeRenderer.startWidth = visualLineWidth;
            rangeRenderer.endWidth = visualLineWidth;
            rangeRenderer.positionCount = math.max(8, rangeSegments);
            rangeRenderer.material = new Material(spriteShader);
            rangeRenderer.material.color = rangeColor;
            rangeRenderer.enabled = false;
        }

        if (aimLineRenderer == null)
        {
            var lineGo = new GameObject("AbilityAimLine");
            lineGo.transform.SetParent(transform, false);

            aimLineRenderer = lineGo.AddComponent<LineRenderer>();
            aimLineRenderer.loop = false;
            aimLineRenderer.useWorldSpace = true;
            aimLineRenderer.startWidth = visualLineWidth;
            aimLineRenderer.endWidth = visualLineWidth;
            aimLineRenderer.positionCount = 2;
            aimLineRenderer.material = new Material(spriteShader);
            aimLineRenderer.material.color = lineColor;
            aimLineRenderer.enabled = false;
        }

        if (targetMarkerTransform == null)
        {
            var markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerGo.name = "AbilityTargetMarker";
            markerGo.transform.SetParent(transform, false);
            markerGo.transform.localScale = Vector3.one * (targetMarkerRadius * 2f);

            var collider = markerGo.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            targetMarkerRenderer = markerGo.GetComponent<MeshRenderer>();
            if (targetMarkerRenderer != null)
            {
                targetMarkerRenderer.material = new Material(spriteShader);
                targetMarkerRenderer.material.color = validTargetColor;
            }

            targetMarkerTransform = markerGo.transform;
            markerGo.SetActive(false);
        }
    }

    private void SetRuntimeVisualVisible(bool isVisible)
    {
        if (!enableRuntimeVisuals)
            return;

        if (rangeRenderer != null)
            rangeRenderer.enabled = isVisible;

        if (aimLineRenderer != null)
            aimLineRenderer.enabled = isVisible;

        if (targetMarkerTransform != null)
            targetMarkerTransform.gameObject.SetActive(isVisible);
    }

    private void UpdateRuntimeVisuals(float3 casterPosition, float castRange)
    {
        if (!enableRuntimeVisuals)
            return;

        EnsureRuntimeVisualObjects();

        DrawRangeCircle(casterPosition, castRange);

        if (!TryGetMouseWorldPoint(out var mouseTarget))
        {
            if (aimLineRenderer != null)
                aimLineRenderer.enabled = false;
            if (targetMarkerTransform != null)
                targetMarkerTransform.gameObject.SetActive(false);
            return;
        }

        float distance = math.distance(casterPosition, mouseTarget);
        bool inRange = distance <= castRange;

        if (aimLineRenderer != null)
        {
            aimLineRenderer.enabled = true;
            aimLineRenderer.material.color = lineColor;
            aimLineRenderer.SetPosition(0, casterPosition);
            aimLineRenderer.SetPosition(1, mouseTarget);
        }

        if (targetMarkerTransform != null)
        {
            targetMarkerTransform.gameObject.SetActive(true);
            targetMarkerTransform.position = mouseTarget;
        }

        if (targetMarkerRenderer != null)
        {
            targetMarkerRenderer.material.color = inRange ? validTargetColor : invalidTargetColor;
        }
    }

    private void DrawRangeCircle(float3 center, float radius)
    {
        if (rangeRenderer == null)
            return;

        int segments = math.max(8, rangeSegments);
        if (rangeRenderer.positionCount != segments)
            rangeRenderer.positionCount = segments;

        rangeRenderer.enabled = true;
        rangeRenderer.material.color = rangeColor;

        float step = math.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = step * i;
            float x = math.cos(angle) * radius;
            float z = math.sin(angle) * radius;
            rangeRenderer.SetPosition(i, center + new float3(x, 0.05f, z));
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!enableRuntimeVisuals || !Application.isPlaying || !isTargeting)
            return;

        if (!TryBuildCastContext(activeUnitClass, out _, out _, out var casterTransform, out var castRange))
            return;

        Vector3 casterPos = casterTransform.Position;

        Gizmos.color = rangeColor;
        Gizmos.DrawWireSphere(casterPos, castRange);

        if (!TryGetMouseWorldPoint(out var mouseTarget))
            return;

        float distance = math.distance(casterTransform.Position, mouseTarget);
        bool inRange = distance <= castRange;

        Gizmos.color = lineColor;
        Gizmos.DrawLine(casterPos, mouseTarget);

        Gizmos.color = inRange ? validTargetColor : invalidTargetColor;
        Gizmos.DrawSphere(mouseTarget, targetMarkerRadius);
    }
#endif
}
