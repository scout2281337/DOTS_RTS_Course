using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ArtilleryStrikeZone : MonoBehaviour
{
    private const int RingSegments = 96;
    private const float GroundOffset = 0.08f;

    private float radius;
    private float warningDuration;
    private float damage;
    private float edgeDamageMultiplier;
    private bool useDamageFalloff;
    private GameObject impactPrefab;
    private float impactPrefabLifetime;
    private AudioCueSO warningAudioCue;
    private AudioCueSO impactAudioCue;
    private bool shakeCameraOnImpact;
    private float elapsed;
    private bool initialized;
    private bool impacted;

    private Transform visualRoot;
    private MeshRenderer diskRenderer;
    private LineRenderer outerRing;
    private LineRenderer pulseRing;
    private LineRenderer sweepLine;
    private LineRenderer crossLineA;
    private LineRenderer crossLineB;
    private Mesh diskMesh;
    private Material diskMaterial;
    private Material lineMaterial;

    public void Initialize(
        float strikeRadius,
        float delay,
        float strikeDamage,
        float damageEdgeMultiplier,
        bool damageFalloff,
        GameObject explosionPrefab,
        float explosionLifetime,
        AudioCueSO incomingAudio,
        AudioCueSO explosionAudio,
        bool cameraShake)
    {
        radius = Mathf.Max(0.1f, strikeRadius);
        warningDuration = Mathf.Max(0.05f, delay);
        damage = Mathf.Max(0f, strikeDamage);
        edgeDamageMultiplier = Mathf.Clamp01(damageEdgeMultiplier);
        useDamageFalloff = damageFalloff;
        impactPrefab = explosionPrefab;
        impactPrefabLifetime = Mathf.Max(0.1f, explosionLifetime);
        warningAudioCue = incomingAudio;
        impactAudioCue = explosionAudio;
        shakeCameraOnImpact = cameraShake;

        BuildVisuals();
        PlayWarningAudio();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || impacted)
            return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / warningDuration);
        UpdateWarningVisual(progress);

        if (elapsed >= warningDuration)
            Impact();
    }

    private void OnDestroy()
    {
        if (diskMaterial != null)
            Destroy(diskMaterial);

        if (lineMaterial != null)
            Destroy(lineMaterial);

        if (diskMesh != null)
            Destroy(diskMesh);
    }

    private void BuildVisuals()
    {
        visualRoot = new GameObject("Generated Artillery Warning").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.up * GroundOffset;
        visualRoot.localRotation = Quaternion.identity;

        diskMaterial = CreateTransparentMaterial("Artillery Warning Disc");
        lineMaterial = CreateTransparentMaterial("Artillery Warning Lines");

        CreateDisk();
        outerRing = CreateRing("Outer Ring", 0.13f);
        pulseRing = CreateRing("Pulse Ring", 0.08f);
        sweepLine = CreateLine("Sweep Line", 0.05f);
        crossLineA = CreateLine("Cross Line A", 0.04f);
        crossLineB = CreateLine("Cross Line B", 0.04f);

        SetCrossLine(crossLineA, Quaternion.identity);
        SetCrossLine(crossLineB, Quaternion.Euler(0f, 90f, 0f));
        UpdateWarningVisual(0f);
    }

    private void CreateDisk()
    {
        GameObject disk = new("Danger Disc");
        disk.transform.SetParent(visualRoot, false);

        MeshFilter meshFilter = disk.AddComponent<MeshFilter>();
        diskMesh = CreateCircleMesh();
        meshFilter.sharedMesh = diskMesh;

        diskRenderer = disk.AddComponent<MeshRenderer>();
        diskRenderer.sharedMaterial = diskMaterial;
        diskRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        diskRenderer.receiveShadows = false;

        disk.transform.localScale = new Vector3(radius, radius, radius);
    }

    private LineRenderer CreateRing(string objectName, float width)
    {
        LineRenderer line = CreateLine(objectName, width);
        line.loop = true;
        line.positionCount = RingSegments;

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i / (float)RingSegments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        return line;
    }

    private LineRenderer CreateLine(string objectName, float width)
    {
        GameObject lineObject = new(objectName);
        lineObject.transform.SetParent(visualRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = lineMaterial;
        line.widthMultiplier = width;
        line.numCapVertices = 6;
        line.numCornerVertices = 6;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    private void SetCrossLine(LineRenderer line, Quaternion rotation)
    {
        line.positionCount = 2;
        line.transform.localRotation = rotation;
        line.SetPosition(0, new Vector3(-radius, 0f, 0f));
        line.SetPosition(1, new Vector3(radius, 0f, 0f));
    }

    private void UpdateWarningVisual(float progress)
    {
        float urgency = Mathf.SmoothStep(0f, 1f, progress);
        float pulseSpeed = Mathf.Lerp(4f, 15f, urgency);
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float flicker = Mathf.Lerp(0.65f, 1f, pulse);

        Color danger = Color.Lerp(new Color(1f, 0.08f, 0.02f), new Color(1f, 0.42f, 0.02f), urgency);
        Color diskColor = danger;
        diskColor.a = Mathf.Lerp(0.08f, 0.24f, urgency) * flicker;
        diskMaterial.color = diskColor;

        Color lineColor = danger;
        lineColor.a = Mathf.Lerp(0.45f, 1f, urgency);
        lineMaterial.color = lineColor;

        float breathingScale = 1f + Mathf.Lerp(0.015f, 0.055f, urgency) * pulse;
        outerRing.transform.localScale = Vector3.one * breathingScale;
        outerRing.widthMultiplier = Mathf.Lerp(0.08f, 0.18f, urgency);

        float pulseScale = Mathf.Lerp(0.25f, 1.05f, Mathf.Repeat(progress * 3.25f, 1f));
        pulseRing.transform.localScale = Vector3.one * pulseScale;
        pulseRing.widthMultiplier = Mathf.Lerp(0.035f, 0.12f, urgency);

        float sweepAngle = Time.time * Mathf.Lerp(140f, 320f, urgency);
        sweepLine.transform.localRotation = Quaternion.Euler(0f, sweepAngle, 0f);
        sweepLine.positionCount = 2;
        sweepLine.SetPosition(0, Vector3.zero);
        sweepLine.SetPosition(1, new Vector3(radius, 0f, 0f));
        sweepLine.widthMultiplier = Mathf.Lerp(0.035f, 0.09f, urgency);

        float crossAlpha = Mathf.Lerp(0.12f, 0.62f, urgency) * flicker;
        Color crossColor = new Color(danger.r, danger.g, danger.b, crossAlpha);
        crossLineA.startColor = crossColor;
        crossLineA.endColor = crossColor;
        crossLineB.startColor = crossColor;
        crossLineB.endColor = crossColor;
    }

    private void Impact()
    {
        impacted = true;
        visualRoot.gameObject.SetActive(false);

        ApplyDamage();
        SpawnImpactVisual();
        PlayImpactAudio();
        ShakeCamera();

        Destroy(gameObject, impactPrefabLifetime);
    }

    private void ApplyDamage()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        EntityQuery hubQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EventHub>());
        EntityQuery damageQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Health>(),
            ComponentType.ReadOnly<LocalTransform>());

        try
        {
            if (hubQuery.CalculateEntityCount() == 0)
                return;

            Entity hub = hubQuery.GetSingletonEntity();
            DynamicBuffer<DamageEvent> damageEvents = em.GetBuffer<DamageEvent>(hub);
            NativeArray<Entity> entities = damageQuery.ToEntityArray(Allocator.Temp);
            float radiusSq = radius * radius;
            float3 center = transform.position;

            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                        continue;

                    LocalTransform entityTransform = em.GetComponentData<LocalTransform>(entity);
                    float2 delta = new(entityTransform.Position.x - center.x, entityTransform.Position.z - center.z);
                    float distanceSq = math.lengthsq(delta);
                    if (distanceSq > radiusSq)
                        continue;

                    float finalDamage = damage;
                    if (useDamageFalloff)
                    {
                        float distance01 = math.saturate(math.sqrt(distanceSq) / radius);
                        finalDamage *= math.lerp(1f, edgeDamageMultiplier, distance01);
                    }

                    UnitClass targetClass = em.HasComponent<Unit>(entity)
                        ? em.GetComponentData<Unit>(entity).Class
                        : default;

                    damageEvents.Add(new DamageEvent
                    {
                        SourceEntity = Entity.Null,
                        TargetEntity = entity,
                        TargetEntityClass = targetClass,
                        DamageAmount = finalDamage,
                        IsAbilityDamage = false
                    });
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        finally
        {
            hubQuery.Dispose();
            damageQuery.Dispose();
        }
    }

    private void SpawnImpactVisual()
    {
        if (impactPrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject instance = Instantiate(impactPrefab, transform.position, rotation);
            Destroy(instance, impactPrefabLifetime);
            return;
        }

        SpawnFallbackImpact();
    }

    private void SpawnFallbackImpact()
    {
        GameObject fallback = new("Generated Artillery Impact");
        fallback.transform.position = transform.position;

        ParticleSystem particles = fallback.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.85f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.65f, 0.25f), new Color(0.45f, 0.08f, 0.02f));
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 96)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.5f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material particleMaterial = CreateParticleMaterial("Generated Artillery Impact Particles");
        renderer.sharedMaterial = particleMaterial;

        Destroy(fallback, impactPrefabLifetime);
        Destroy(particleMaterial, impactPrefabLifetime);
    }

    private void PlayWarningAudio()
    {
        if (warningAudioCue != null)
            GameAudioManager.Instance.Play3D(warningAudioCue, transform.position);
    }

    private void PlayImpactAudio()
    {
        if (impactAudioCue != null)
            GameAudioManager.Instance.Play3D(impactAudioCue, transform.position);
    }

    private void ShakeCamera()
    {
        if (!shakeCameraOnImpact)
            return;

        CameraShaker shaker = FindFirstObjectByType<CameraShaker>();
        if (shaker == null)
            return;

        shaker.PlayShake(
            0.45f,
            new Vector3(0.18f, 0.12f, 0.18f),
            new Vector3(1.2f, 0.8f, 2.4f),
            22f);
    }

    private static Mesh CreateCircleMesh()
    {
        Vector3[] vertices = new Vector3[RingSegments + 1];
        int[] triangles = new int[RingSegments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i / (float)RingSegments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        for (int i = 0; i < RingSegments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i == RingSegments - 1 ? 1 : i + 2;
        }

        Mesh mesh = new();
        mesh.name = "Generated Artillery Warning Disc Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateTransparentMaterial(string materialName)
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material material = new(shader);
        material.name = materialName;
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    private static Material CreateParticleMaterial(string materialName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new(shader);
        material.name = materialName;
        return material;
    }
}
