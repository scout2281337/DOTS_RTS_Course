using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(GeneratorActivity))]
public class GeneratorVisualEffect : MonoBehaviour
{
    [Header("Palette")]
    [SerializeField] private Color idleColor = new Color(1f, 0.48f, 0.08f, 0.55f);
    [SerializeField] private Color chargeColor = new Color(1f, 0.78f, 0.16f, 0.95f);
    [SerializeField] private Color activeColor = new Color(0.16f, 0.88f, 1f, 0.95f);

    [Header("Shape")]
    [SerializeField] private float ringHeight = 0.08f;
    [SerializeField] private float coreLightHeight = 1.25f;
    [SerializeField, Range(24, 160)] private int ringSegments = 96;
    [SerializeField] private float activationRingWidth = 0.08f;
    [SerializeField] private float revealRingWidth = 0.16f;
    [SerializeField] private float sweepLineWidth = 0.08f;

    [Header("Pulse")]
    [SerializeField] private float activationPulseDuration = 0.9f;
    [SerializeField] private float activePulseInterval = 2.75f;
    [SerializeField] private int activationSparkBurst = 42;

    [Header("Light")]
    [SerializeField] private bool enablePointLight = true;
    [SerializeField] private float idleLightIntensity = 0.45f;
    [SerializeField] private float chargeLightIntensity = 1.55f;
    [SerializeField] private float activeLightIntensity = 2.2f;
    [SerializeField] private float flickerSpeed = 9f;
    [SerializeField] private float flickerAmount = 0.12f;

    [Header("Sparks")]
    [SerializeField] private float idleSparkRate = 0.5f;
    [SerializeField] private float chargeSparkRate = 28f;
    [SerializeField] private float activeSparkRate = 9f;

    private const string VisualRootName = "Generated Generator VFX";

    private GeneratorActivity generator;
    private Transform visualRoot;
    private LineRenderer activationRing;
    private LineRenderer chargeRing;
    private LineRenderer revealRing;
    private LineRenderer pulseRing;
    private LineRenderer sweepLine;
    private ParticleSystem sparks;
    private Light coreLight;
    private Material lineMaterial;
    private Material particleMaterial;
    private bool wasActivated;
    private float pulseTimer;
    private float nextPulseTimer;
    private float sweepAngle;

    private void Awake()
    {
        generator = GetComponent<GeneratorActivity>();
        BuildVisuals();
    }

    private void OnEnable()
    {
        if (generator == null)
            generator = GetComponent<GeneratorActivity>();

        BuildVisuals();
        wasActivated = generator != null && generator.IsActivated;
    }

    private void Update()
    {
        if (generator == null)
            return;

        UpdateVisuals(Time.deltaTime);
    }

    private void OnDestroy()
    {
        DestroyGenerated(lineMaterial);
        DestroyGenerated(particleMaterial);
    }

    private void OnValidate()
    {
        ringSegments = Mathf.Max(24, ringSegments);
        activationPulseDuration = Mathf.Max(0.05f, activationPulseDuration);
        activePulseInterval = Mathf.Max(0.1f, activePulseInterval);
    }

    private void BuildVisuals()
    {
        if (visualRoot != null)
            return;

        Transform existingRoot = transform.Find(VisualRootName);
        if (existingRoot != null)
        {
            visualRoot = existingRoot;
        }
        else
        {
            GameObject rootObject = new GameObject(VisualRootName);
            rootObject.transform.SetParent(transform, false);
            visualRoot = rootObject.transform;
        }

        lineMaterial = CreateMaterial("Sprites/Default", "Generator VFX Lines");
        particleMaterial = CreateMaterial("Universal Render Pipeline/Particles/Unlit", "Generator VFX Sparks");

        activationRing = CreateRing("Activation Ring", activationRingWidth);
        chargeRing = CreateRing("Charge Ring", activationRingWidth * 1.8f);
        revealRing = CreateRing("Reveal Ring", revealRingWidth);
        pulseRing = CreateRing("Pulse Ring", revealRingWidth * 1.35f);
        sweepLine = CreateLine("Sweep Line", sweepLineWidth);
        sparks = CreateSparks();
        coreLight = CreateCoreLight();
    }

    private Material CreateMaterial(string preferredShaderName, string materialName)
    {
        Shader shader = Shader.Find(preferredShaderName);
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");

        Material material = new Material(shader)
        {
            name = materialName,
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        return material;
    }

    private LineRenderer CreateRing(string objectName, float width)
    {
        LineRenderer line = CreateLine(objectName, width);
        line.loop = true;
        line.positionCount = ringSegments;
        return line;
    }

    private LineRenderer CreateLine(string objectName, float width)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(visualRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.material = lineMaterial;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private ParticleSystem CreateSparks()
    {
        GameObject sparkObject = new GameObject("Energy Sparks");
        sparkObject.transform.SetParent(visualRoot, false);
        sparkObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        sparkObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem particleSystem = sparkObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.gravityModifier = 0.15f;
        main.maxParticles = 160;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = idleSparkRate;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.25f;

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        particleSystem.Play();
        return particleSystem;
    }

    private Light CreateCoreLight()
    {
        GameObject lightObject = new GameObject("Core Glow");
        lightObject.transform.SetParent(visualRoot, false);
        lightObject.transform.localPosition = new Vector3(0f, coreLightHeight, 0f);

        Light lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = LightType.Point;
        lightComponent.shadows = LightShadows.None;
        lightComponent.enabled = enablePointLight;
        return lightComponent;
    }

    private void UpdateVisuals(float deltaTime)
    {
        bool active = generator.IsActivated;
        bool starting = generator.IsActivationInProgress;
        float progress = generator.ActivationProgress01;
        float activationRadius = Mathf.Max(0.05f, generator.ActivationRadius);
        float revealRadius = Mathf.Max(activationRadius, generator.RevealRadius);

        if (active && !wasActivated)
            TriggerPulse();

        wasActivated = active;

        if (active)
        {
            nextPulseTimer -= deltaTime;
            if (nextPulseTimer <= 0f)
            {
                TriggerPulse();
                nextPulseTimer = activePulseInterval;
            }
        }
        else
        {
            nextPulseTimer = 0f;
        }

        Color stateColor = active ? activeColor : starting ? Color.Lerp(idleColor, chargeColor, progress) : idleColor;
        UpdateLight(stateColor, active, starting, progress);
        UpdateSparks(stateColor, active, starting, progress);
        UpdateRings(stateColor, active, starting, progress, activationRadius, revealRadius, deltaTime);
    }

    private void UpdateLight(Color stateColor, bool active, bool starting, float progress)
    {
        if (coreLight == null)
            return;

        coreLight.enabled = enablePointLight;
        if (!enablePointLight)
            return;

        float baseIntensity = active
            ? activeLightIntensity
            : starting
                ? Mathf.Lerp(idleLightIntensity, chargeLightIntensity, progress)
                : idleLightIntensity;

        float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        coreLight.color = stateColor;
        coreLight.intensity = Mathf.Max(0f, baseIntensity * flicker);
        coreLight.range = active ? Mathf.Max(7f, generator.RevealRadius * 0.35f) : Mathf.Max(3f, generator.ActivationRadius * 0.9f);
    }

    private void UpdateSparks(Color stateColor, bool active, bool starting, float progress)
    {
        if (sparks == null)
            return;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = active
            ? activeSparkRate
            : starting
                ? Mathf.Lerp(idleSparkRate, chargeSparkRate, progress)
                : idleSparkRate;

        ParticleSystem.MainModule main = sparks.main;
        main.startColor = stateColor;

        if (!sparks.isPlaying)
            sparks.Play();
    }

    private void UpdateRings(
        Color stateColor,
        bool active,
        bool starting,
        float progress,
        float activationRadius,
        float revealRadius,
        float deltaTime)
    {
        float idlePulse = 0.65f + Mathf.Sin(Time.time * 2.1f) * 0.18f;
        UpdateCircle(activationRing, activationRadius, idleColor, active ? 0f : idlePulse * 0.42f, activationRingWidth);

        float chargeRadius = Mathf.Lerp(activationRadius * 0.22f, activationRadius, EaseOutCubic(progress));
        UpdateCircle(chargeRing, chargeRadius, chargeColor, starting ? Mathf.Lerp(0.15f, 0.95f, progress) : 0f, activationRingWidth * 1.8f);

        float revealAlpha = active ? 0.32f + Mathf.Sin(Time.time * 3.6f) * 0.08f : 0f;
        UpdateCircle(revealRing, revealRadius, activeColor, revealAlpha, revealRingWidth);

        UpdatePulseRing(activationRadius, revealRadius, deltaTime);
        UpdateSweepLine(stateColor, active, starting, progress, activationRadius, revealRadius, deltaTime);
    }

    private void UpdatePulseRing(float activationRadius, float revealRadius, float deltaTime)
    {
        if (pulseTimer <= 0f)
        {
            SetLineVisible(pulseRing, false);
            return;
        }

        pulseTimer -= deltaTime;
        float normalized = Mathf.Clamp01(1f - pulseTimer / activationPulseDuration);
        float eased = EaseOutCubic(normalized);
        float radius = Mathf.Lerp(activationRadius, revealRadius, eased);
        float alpha = Mathf.Lerp(0.95f, 0f, normalized);
        UpdateCircle(pulseRing, radius, activeColor, alpha, revealRingWidth * 1.35f);
    }

    private void UpdateSweepLine(
        Color stateColor,
        bool active,
        bool starting,
        float progress,
        float activationRadius,
        float revealRadius,
        float deltaTime)
    {
        bool visible = active || starting;
        SetLineVisible(sweepLine, visible);
        if (!visible)
            return;

        sweepAngle += deltaTime * Mathf.Lerp(120f, 260f, active ? 1f : progress);
        float angle = sweepAngle * Mathf.Deg2Rad;
        float radius = active ? revealRadius : Mathf.Lerp(activationRadius * 0.35f, activationRadius, progress);
        Color color = stateColor;
        color.a = active ? 0.75f : Mathf.Lerp(0.25f, 0.85f, progress);

        sweepLine.loop = false;
        sweepLine.positionCount = 2;
        sweepLine.widthMultiplier = sweepLineWidth;
        sweepLine.startColor = color;
        sweepLine.endColor = new Color(color.r, color.g, color.b, 0f);
        sweepLine.SetPosition(0, new Vector3(0f, ringHeight + 0.04f, 0f));
        sweepLine.SetPosition(1, new Vector3(Mathf.Cos(angle) * radius, ringHeight + 0.04f, Mathf.Sin(angle) * radius));
    }

    private void UpdateCircle(LineRenderer line, float radius, Color color, float alpha, float width)
    {
        if (line == null)
            return;

        bool visible = alpha > 0.01f && radius > 0.01f;
        SetLineVisible(line, visible);
        if (!visible)
            return;

        color.a = Mathf.Clamp01(alpha);
        line.loop = true;
        line.positionCount = ringSegments;
        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (float)i / ringSegments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, ringHeight, Mathf.Sin(angle) * radius));
        }
    }

    private void SetLineVisible(LineRenderer line, bool visible)
    {
        if (line != null && line.enabled != visible)
            line.enabled = visible;
    }

    private void TriggerPulse()
    {
        pulseTimer = activationPulseDuration;
        if (sparks != null)
            sparks.Emit(Mathf.Max(0, activationSparkBurst));
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        float inverted = 1f - value;
        return 1f - inverted * inverted * inverted;
    }

    private static void DestroyGenerated(Object generatedObject)
    {
        if (generatedObject == null)
            return;

        if (Application.isPlaying)
            Destroy(generatedObject);
        else
            DestroyImmediate(generatedObject);
    }
}
