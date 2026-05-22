using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class EscortPlatformActivity : MonoBehaviour
{
    private const string AutoVisualPrefix = "EscortAuto_";

    [Header("Identity")]
    [SerializeField] private string activityId = "escort_capsule";

    [Header("Route")]
    [SerializeField] private Transform movingRoot;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField, Min(0.01f)] private float moveSpeed = 2.25f;
    [SerializeField] private bool resetToStartOnBegin = true;
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField, Min(0f)] private float rotationLerpSpeed = 8f;
    [SerializeField] private bool autoCreateMissingRoutePoints = true;
    [SerializeField, Min(1f)] private float defaultRouteLength = 16f;

    [Header("Escort Zone")]
    [SerializeField, Min(0.1f)] private float activationRadius = 5f;
    [SerializeField, Min(1)] private int requiredFriendlyUnits = 1;
    [SerializeField] private bool waitForMissionStart = true;

    [Header("Optional Visuals")]
    [SerializeField] private bool autoCreateDefaultVisuals = true;
    [SerializeField] private bool createPlaceholderBody = true;
    [SerializeField] private Transform zoneIndicator;
    [SerializeField] private LineRenderer zoneLine;
    [SerializeField] private LineRenderer routeLine;
    [SerializeField] private Light statusLight;
    [SerializeField] private ParticleSystem movementParticles;
    [SerializeField] private Color idleColor = new(0.35f, 0.85f, 1f, 0.9f);
    [SerializeField] private Color movingColor = new(0.25f, 1f, 0.55f, 1f);
    [SerializeField] private Color completedColor = new(1f, 0.82f, 0.25f, 1f);
    [SerializeField, Range(24, 160)] private int zoneSegments = 96;
    [SerializeField, Min(0.01f)] private float routeLineWidth = 0.16f;
    [SerializeField, Min(0.01f)] private float zoneLineWidth = 0.08f;
    [SerializeField, Min(0f)] private float visualGroundOffset = 0.08f;
    [SerializeField, Min(0f)] private float lightBaseIntensity = 1.5f;
    [SerializeField, Min(0f)] private float lightPulseIntensity = 0.8f;

    [Header("World Reports")]
    [SerializeField] private bool reportOnDiscovered = true;
    [SerializeField] private bool reportOnBegin = true;
    [SerializeField] private bool reportMilestones = true;
    [SerializeField] private bool reportOnCompleted = true;
    [SerializeField] private string discoveredLabel = "ESCORT SIGNAL";
    [SerializeField] private string beginLabel = "CAPSULE MOVING";
    [SerializeField] private string pausedLabel = "CAPSULE HOLDING";
    [SerializeField] private string completedLabel = "CAPSULE ARRIVED";
    [SerializeField, Min(0.1f)] private float reportDuration = 4f;

    private readonly bool[] milestoneReported = new bool[3];
    private readonly float[] milestones = { 0.25f, 0.5f, 0.75f };

    private EntityQuery friendlyUnitsQuery;
    private World cachedWorld;
    private bool hasQuery;
    private bool escortActive;
    private bool completed;
    private bool wasMoving;
    private float progress01;
    private int friendlyUnitsInZone;

    public event Action<EscortPlatformActivity> OnEscortCompleted;

    public string ActivityId => activityId;
    public float ActivationRadius => activationRadius;
    public int RequiredFriendlyUnits => requiredFriendlyUnits;
    public int FriendlyUnitsInZone => friendlyUnitsInZone;
    public bool IsEscortActive => escortActive;
    public bool IsMoving => escortActive && !completed && friendlyUnitsInZone >= requiredFriendlyUnits;
    public bool IsCompleted => completed;
    public float Progress01 => progress01;
    public Vector3 CurrentPosition => Root.position;
    public Vector3 StartPosition => startPoint != null ? startPoint.position : transform.position;
    public Vector3 EndPosition => endPoint != null ? endPoint.position : transform.position;

    private Transform Root => movingRoot != null ? movingRoot : transform;

    private void Awake()
    {
        EnsureAutoSetup();
    }

    private void Start()
    {
        EnsureAutoSetup();

        if (reportOnDiscovered)
        {
            Report(
                WorldEventImportance.Medium,
                WorldEventKnowledge.Approximate,
                discoveredLabel);
        }

        RefreshRouteVisual();
        RefreshZoneVisual();

        if (!waitForMissionStart)
            BeginEscort();
        else
            ApplyPositionFromProgress();
    }

    private void OnDestroy()
    {
        hasQuery = false;
    }

    private void Update()
    {
        EnsureAutoSetup();
        RefreshZoneVisual();
        RefreshRouteVisual();
        UpdateStatusLight();
        UpdateMovementParticles();

        if (!escortActive || completed)
            return;

        friendlyUnitsInZone = CountFriendlyUnitsInZone();
        bool shouldMove = friendlyUnitsInZone >= requiredFriendlyUnits;

        if (shouldMove)
            AdvancePlatform(Time.deltaTime);

        if (shouldMove != wasMoving)
        {
            wasMoving = shouldMove;
            Report(
                shouldMove ? WorldEventImportance.Medium : WorldEventImportance.Low,
                WorldEventKnowledge.Exact,
                shouldMove ? beginLabel : pausedLabel);
        }
    }

    [ContextMenu("Build Default Visuals")]
    public void BuildDefaultVisuals()
    {
        EnsureAutoSetup();
        RefreshZoneVisual();
        RefreshRouteVisual();
        UpdateStatusLight();
    }

    public void BeginEscort()
    {
        if (completed && !resetToStartOnBegin)
            return;

        completed = false;
        escortActive = true;
        wasMoving = false;

        if (resetToStartOnBegin)
        {
            progress01 = 0f;
            Array.Clear(milestoneReported, 0, milestoneReported.Length);
            ApplyPositionFromProgress();
        }

        if (reportOnBegin)
        {
            Report(
                WorldEventImportance.High,
                WorldEventKnowledge.Exact,
                beginLabel);
        }
    }

    public void PauseEscort()
    {
        escortActive = false;
        wasMoving = false;
    }

    public void ResetEscort()
    {
        escortActive = !waitForMissionStart;
        completed = false;
        wasMoving = false;
        progress01 = 0f;
        friendlyUnitsInZone = 0;
        Array.Clear(milestoneReported, 0, milestoneReported.Length);
        ApplyPositionFromProgress();
    }

    private void AdvancePlatform(float deltaTime)
    {
        float totalDistance = Vector3.Distance(StartPosition, EndPosition);
        if (totalDistance <= 0.001f)
        {
            CompleteEscort();
            return;
        }

        progress01 = Mathf.Clamp01(progress01 + moveSpeed * deltaTime / totalDistance);
        ApplyPositionFromProgress();
        ReportMilestones();

        if (progress01 >= 1f)
            CompleteEscort();
    }

    private void ApplyPositionFromProgress()
    {
        Transform root = Root;
        Vector3 start = StartPosition;
        Vector3 end = EndPosition;
        root.position = Vector3.Lerp(start, end, progress01);

        if (!faceMoveDirection)
            return;

        Vector3 direction = end - start;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        root.rotation = rotationLerpSpeed <= 0f
            ? targetRotation
            : Quaternion.Slerp(root.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
    }

    private void CompleteEscort()
    {
        if (completed)
            return;

        progress01 = 1f;
        completed = true;
        escortActive = false;
        wasMoving = false;
        friendlyUnitsInZone = CountFriendlyUnitsInZone();
        ApplyPositionFromProgress();

        if (reportOnCompleted)
        {
            Report(
                WorldEventImportance.High,
                WorldEventKnowledge.Exact,
                completedLabel);
        }

        OnEscortCompleted?.Invoke(this);
    }

    private void ReportMilestones()
    {
        if (!reportMilestones)
            return;

        for (int i = 0; i < milestones.Length; i++)
        {
            if (milestoneReported[i] || progress01 < milestones[i])
                continue;

            milestoneReported[i] = true;
            int percent = Mathf.RoundToInt(milestones[i] * 100f);
            Report(
                WorldEventImportance.Medium,
                WorldEventKnowledge.Exact,
                $"CAPSULE {percent}%");
        }
    }

    private int CountFriendlyUnitsInZone()
    {
        if (!TryGetEntityManager(out EntityManager em))
            return 0;

        EnsureQuery(em);

        using var entities = friendlyUnitsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        int count = 0;
        float radiusSq = activationRadius * activationRadius;
        float3 center = ToFloat3(CurrentPosition);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                continue;

            Unit unit = em.GetComponentData<Unit>(entity);
            if (unit.faction != Faction.Friendly)
                continue;

            LocalTransform transformData = em.GetComponentData<LocalTransform>(entity);
            float3 delta = transformData.Position - center;
            delta.y = 0f;

            if (math.lengthsq(delta) <= radiusSq)
                count++;
        }

        return count;
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

    private void EnsureAutoSetup()
    {
        EnsureRoutePoints();

        if (!autoCreateDefaultVisuals)
            return;

        bool hadBodyRenderer = HasNonGeneratedBodyRenderer();

        if (routeLine == null)
            routeLine = FindOrCreateLineRenderer($"{AutoVisualPrefix}RouteLine", transform, routeLineWidth, idleColor);
        else if (routeLine.sharedMaterial == null)
            ConfigureLineRenderer(routeLine, routeLineWidth, idleColor);

        if (zoneLine == null)
            zoneLine = FindOrCreateLineRenderer($"{AutoVisualPrefix}EscortZone", Root, zoneLineWidth, idleColor);
        else if (zoneLine.sharedMaterial == null)
            ConfigureLineRenderer(zoneLine, zoneLineWidth, idleColor);

        if (statusLight == null)
            statusLight = FindOrCreateStatusLight();

        if (movementParticles == null)
            movementParticles = FindOrCreateMovementParticles();

        if (createPlaceholderBody && !hadBodyRenderer)
            CreatePlaceholderBody();
    }

    private void EnsureRoutePoints()
    {
        if (!autoCreateMissingRoutePoints)
            return;

        Transform parent = transform.parent;

        if (startPoint == null)
        {
            GameObject start = new GameObject($"{name}_EscortStart");
            start.transform.SetParent(parent, true);
            start.transform.position = transform.position;
            startPoint = start.transform;
        }

        if (endPoint == null)
        {
            GameObject end = new GameObject($"{name}_EscortEnd");
            end.transform.SetParent(parent, true);
            Vector3 direction = transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                direction = Vector3.forward;

            end.transform.position = transform.position + direction.normalized * defaultRouteLength;
            endPoint = end.transform;
        }
    }

    private LineRenderer FindOrCreateLineRenderer(string childName, Transform parent, float width, Color color)
    {
        Transform existing = parent.Find(childName);
        if (existing != null && existing.TryGetComponent(out LineRenderer existingLine))
        {
            ConfigureLineRenderer(existingLine, width, color);
            return existingLine;
        }

        GameObject lineObject = new GameObject(childName);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(line, width, color);
        return line;
    }

    private void ConfigureLineRenderer(LineRenderer line, float width, Color color)
    {
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCornerVertices = 8;
        line.numCapVertices = 8;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = CreateRuntimeMaterial(color, true);
        line.startColor = color;
        line.endColor = color;
    }

    private Light FindOrCreateStatusLight()
    {
        Transform existing = Root.Find($"{AutoVisualPrefix}StatusLight");
        if (existing != null && existing.TryGetComponent(out Light existingLight))
            return existingLight;

        GameObject lightObject = new GameObject($"{AutoVisualPrefix}StatusLight");
        lightObject.transform.SetParent(Root, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.15f, 0f);

        Light lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = LightType.Point;
        lightComponent.range = Mathf.Max(activationRadius * 1.25f, 6f);
        lightComponent.intensity = lightBaseIntensity;
        lightComponent.color = idleColor;
        lightComponent.shadows = LightShadows.None;
        return lightComponent;
    }

    private ParticleSystem FindOrCreateMovementParticles()
    {
        Transform existing = Root.Find($"{AutoVisualPrefix}DriveParticles");
        if (existing != null && existing.TryGetComponent(out ParticleSystem existingParticles))
            return existingParticles;

        GameObject particleObject = new GameObject($"{AutoVisualPrefix}DriveParticles");
        particleObject.transform.SetParent(Root, false);
        particleObject.transform.localPosition = new Vector3(0f, 0.35f, -1.45f);
        particleObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ConfigureMovementParticles(particles);
        return particles;
    }

    private void ConfigureMovementParticles(ParticleSystem particles)
    {
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 90;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(movingColor.r, movingColor.g, movingColor.b, 0.55f),
            new Color(idleColor.r, idleColor.g, idleColor.b, 0.15f));

        var emission = particles.emission;
        emission.enabled = false;
        emission.rateOverTime = 28f;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.35f;
        shape.length = 0.8f;

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateRuntimeMaterial(movingColor, true);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void CreatePlaceholderBody()
    {
        if (Root.Find($"{AutoVisualPrefix}Base") != null || Root.Find($"{AutoVisualPrefix}Capsule") != null)
            return;

        Material baseMaterial = CreateRuntimeMaterial(new Color(0.08f, 0.13f, 0.14f, 1f), false);
        Material capsuleMaterial = CreateRuntimeMaterial(new Color(0.12f, 0.55f, 0.58f, 1f), false);
        Material glowMaterial = CreateRuntimeMaterial(new Color(0.25f, 1f, 0.78f, 1f), true);

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObject.name = $"{AutoVisualPrefix}Base";
        baseObject.transform.SetParent(Root, false);
        baseObject.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        baseObject.transform.localScale = new Vector3(2.6f, 0.26f, 3.35f);
        AssignMaterial(baseObject, baseMaterial);
        RemoveGeneratedCollider(baseObject);

        GameObject capsuleObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleObject.name = $"{AutoVisualPrefix}Capsule";
        capsuleObject.transform.SetParent(Root, false);
        capsuleObject.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        capsuleObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        capsuleObject.transform.localScale = new Vector3(0.75f, 1.45f, 0.75f);
        AssignMaterial(capsuleObject, capsuleMaterial);
        RemoveGeneratedCollider(capsuleObject);

        GameObject coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObject.name = $"{AutoVisualPrefix}CoreGlow";
        coreObject.transform.SetParent(Root, false);
        coreObject.transform.localPosition = new Vector3(0f, 1.05f, 0.85f);
        coreObject.transform.localScale = Vector3.one * 0.34f;
        AssignMaterial(coreObject, glowMaterial);
        RemoveGeneratedCollider(coreObject);
    }

    private bool HasNonGeneratedBodyRenderer()
    {
        Renderer[] renderers = Root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer is LineRenderer || renderer is ParticleSystemRenderer)
                continue;

            if (renderer.gameObject.name.StartsWith(AutoVisualPrefix, StringComparison.Ordinal))
                continue;

            return true;
        }

        return false;
    }

    private static void AssignMaterial(GameObject target, Material material)
    {
        if (target.TryGetComponent(out Renderer renderer))
            renderer.sharedMaterial = material;
    }

    private static void RemoveGeneratedCollider(GameObject target)
    {
        if (!target.TryGetComponent(out Collider generatedCollider))
            return;

        if (Application.isPlaying)
            Destroy(generatedCollider);
        else
            DestroyImmediate(generatedCollider);
    }

    private static Material CreateRuntimeMaterial(Color color, bool emissive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (emissive && material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", color * 2f);

        return material;
    }

    private void RefreshZoneVisual()
    {
        Color zoneColor = completed ? completedColor : IsMoving ? movingColor : idleColor;

        if (zoneIndicator != null)
        {
            float diameter = activationRadius * 2f;
            zoneIndicator.position = CurrentPosition + Vector3.up * visualGroundOffset;
            zoneIndicator.localScale = new Vector3(diameter, zoneIndicator.localScale.y, diameter);
            zoneIndicator.gameObject.SetActive(!completed);
        }

        if (zoneLine == null)
            return;

        zoneLine.enabled = !completed;
        zoneLine.useWorldSpace = true;
        zoneLine.loop = true;
        zoneLine.positionCount = Mathf.Max(24, zoneSegments);
        zoneLine.widthMultiplier = zoneLineWidth;
        zoneLine.startColor = zoneColor;
        zoneLine.endColor = zoneColor;

        Vector3 center = CurrentPosition + Vector3.up * visualGroundOffset;
        for (int i = 0; i < zoneLine.positionCount; i++)
        {
            float angle = i / (float)zoneLine.positionCount * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * activationRadius;
            zoneLine.SetPosition(i, point);
        }
    }

    private void RefreshRouteVisual()
    {
        if (routeLine == null)
            return;

        routeLine.positionCount = 2;
        routeLine.useWorldSpace = true;
        routeLine.widthMultiplier = routeLineWidth;
        routeLine.SetPosition(0, StartPosition + Vector3.up * visualGroundOffset);
        routeLine.SetPosition(1, EndPosition + Vector3.up * visualGroundOffset);

        Color routeColor = completed ? completedColor : IsMoving ? movingColor : idleColor;
        routeLine.startColor = routeColor;
        routeLine.endColor = routeColor;
    }

    private void UpdateStatusLight()
    {
        if (statusLight == null)
            return;

        Color color = completed ? completedColor : IsMoving ? movingColor : idleColor;
        float pulse = IsMoving ? Mathf.Sin(Time.time * 9f) * lightPulseIntensity : 0f;
        statusLight.color = color;
        statusLight.intensity = Mathf.Max(0f, lightBaseIntensity + pulse);
        statusLight.range = Mathf.Max(activationRadius * 1.25f, 6f);
    }

    private void UpdateMovementParticles()
    {
        if (movementParticles == null)
            return;

        var emission = movementParticles.emission;
        emission.enabled = IsMoving;

        if (IsMoving && !movementParticles.isPlaying)
            movementParticles.Play();
        else if (!IsMoving && movementParticles.isPlaying)
            movementParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Report(WorldEventImportance importance, WorldEventKnowledge knowledge, string label)
    {
        WorldEventUtility.Report(
            WorldEventType.EscortPlatform,
            CurrentPosition,
            importance,
            knowledge,
            activationRadius,
            reportDuration,
            label);
    }

    private static float3 ToFloat3(Vector3 value)
    {
        return new float3(value.x, value.y, value.z);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = StartPosition;
        Vector3 end = EndPosition;

        Gizmos.color = new Color(0.25f, 0.9f, 1f, 0.9f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.7f);

        Gizmos.color = new Color(1f, 0.82f, 0.25f, 0.9f);
        Gizmos.DrawWireSphere(end, 0.9f);

        Gizmos.color = IsMoving
            ? new Color(0.25f, 1f, 0.55f, 0.38f)
            : new Color(0.25f, 0.85f, 1f, 0.28f);
        Gizmos.DrawWireSphere(Application.isPlaying ? CurrentPosition : start, activationRadius);
    }
}
