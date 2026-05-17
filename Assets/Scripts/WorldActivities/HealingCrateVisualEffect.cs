using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(HealingCrateActivity))]
public class HealingCrateVisualEffect : MonoBehaviour
{
    [Header("Palette")]
    [SerializeField] private Color readyColor = new Color(0.2f, 1f, 0.48f, 0.75f);
    [SerializeField] private Color healColor = new Color(0.48f, 1f, 0.72f, 0.95f);
    [SerializeField] private Color spentColor = new Color(0.35f, 1f, 0.7f, 0.3f);

    [Header("Shape")]
    [SerializeField] private float ringHeight = 0.08f;
    [SerializeField] private float coreLightHeight = 0.9f;
    [SerializeField, Range(24, 160)] private int ringSegments = 88;
    [SerializeField] private float radiusRingWidth = 0.07f;
    [SerializeField] private float pulseRingWidth = 0.16f;
    [SerializeField] private float crossLineWidth = 0.13f;
    [SerializeField, Range(0.1f, 0.8f)] private float crossSizeInRadius = 0.34f;

    [Header("Pulse")]
    [SerializeField] private float ambientPulseSpeed = 2.25f;
    [SerializeField] private float healPulseDuration = 0.85f;
    [SerializeField] private int healBurstParticles = 55;

    [Header("Light")]
    [SerializeField] private bool enablePointLight = true;
    [SerializeField] private float readyLightIntensity = 0.75f;
    [SerializeField] private float healLightIntensity = 2.4f;
    [SerializeField] private float spentLightIntensity = 0.25f;
    [SerializeField] private float flickerSpeed = 5.5f;
    [SerializeField] private float flickerAmount = 0.08f;

    [Header("Particles")]
    [SerializeField] private float readyParticleRate = 4f;
    [SerializeField] private float healParticleRate = 45f;
    [SerializeField] private float spentParticleRate = 0f;

    private const string VisualRootName = "Generated Healing Crate VFX";

    private HealingCrateActivity crate;
    private Transform visualRoot;
    private LineRenderer radiusRing;
    private LineRenderer innerRing;
    private LineRenderer pulseRing;
    private LineRenderer crossHorizontal;
    private LineRenderer crossVertical;
    private ParticleSystem motes;
    private Light coreLight;
    private Material lineMaterial;
    private Material particleMaterial;
    private float healPulseTimer;

    private void Awake()
    {
        crate = GetComponent<HealingCrateActivity>();
        BuildVisuals();
    }

    private void OnEnable()
    {
        if (crate == null)
            crate = GetComponent<HealingCrateActivity>();

        if (crate != null)
        {
            crate.OnCrateUsed -= PlayHealBurst;
            crate.OnCrateUsed += PlayHealBurst;
        }

        BuildVisuals();
    }

    private void OnDisable()
    {
        if (crate != null)
            crate.OnCrateUsed -= PlayHealBurst;
    }

    private void Update()
    {
        if (crate == null)
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
        healPulseDuration = Mathf.Max(0.05f, healPulseDuration);
    }

    public void PlayHealBurst()
    {
        healPulseTimer = healPulseDuration;

        if (motes != null)
            motes.Emit(Mathf.Max(0, healBurstParticles));
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

        lineMaterial = CreateMaterial("Sprites/Default", "Healing Crate VFX Lines");
        particleMaterial = CreateMaterial("Universal Render Pipeline/Particles/Unlit", "Healing Crate VFX Particles");

        radiusRing = CreateRing("Pickup Radius Ring", radiusRingWidth);
        innerRing = CreateRing("Medical Core Ring", radiusRingWidth * 1.4f);
        pulseRing = CreateRing("Healing Pulse Ring", pulseRingWidth);
        crossHorizontal = CreateLine("Medical Cross Horizontal", crossLineWidth);
        crossVertical = CreateLine("Medical Cross Vertical", crossLineWidth);
        motes = CreateMotes();
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
        line.numCapVertices = 5;
        line.numCornerVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private ParticleSystem CreateMotes()
    {
        GameObject particleObject = new GameObject("Healing Motes");
        particleObject.transform.SetParent(visualRoot, false);
        particleObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        particleObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.gravityModifier = -0.08f;
        main.maxParticles = 150;
        main.startColor = readyColor;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = readyParticleRate;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 0.35f;
        shape.donutRadius = 0.08f;

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
        GameObject lightObject = new GameObject("Medical Glow");
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
        float radius = Mathf.Max(0.05f, crate.ActivationRadius);
        bool used = crate.IsUsed;
        bool pulsing = healPulseTimer > 0f;
        Color stateColor = pulsing ? healColor : used ? spentColor : readyColor;

        if (healPulseTimer > 0f)
            healPulseTimer -= deltaTime;

        UpdateAmbientLines(radius, used, pulsing);
        UpdatePulse(radius);
        UpdateLight(stateColor, radius, used, pulsing);
        UpdateParticles(stateColor, used, pulsing);
    }

    private void UpdateAmbientLines(float radius, bool used, bool pulsing)
    {
        float ambientPulse = 0.58f + Mathf.Sin(Time.time * ambientPulseSpeed) * 0.18f;
        float readyAlpha = used ? 0.08f : ambientPulse;
        Color ringColor = pulsing ? healColor : used ? spentColor : readyColor;

        UpdateCircle(radiusRing, radius, ringColor, readyAlpha * 0.45f, radiusRingWidth);
        UpdateCircle(innerRing, radius * 0.42f, ringColor, used ? 0.12f : 0.5f + ambientPulse * 0.25f, radiusRingWidth * 1.4f);

        float crossHalfSize = radius * crossSizeInRadius;
        float crossAlpha = used ? 0.12f : 0.65f + ambientPulse * 0.15f;
        UpdateCrossLine(crossHorizontal, new Vector3(-crossHalfSize, ringHeight + 0.03f, 0f), new Vector3(crossHalfSize, ringHeight + 0.03f, 0f), ringColor, crossAlpha);
        UpdateCrossLine(crossVertical, new Vector3(0f, ringHeight + 0.03f, -crossHalfSize), new Vector3(0f, ringHeight + 0.03f, crossHalfSize), ringColor, crossAlpha);
    }

    private void UpdatePulse(float radius)
    {
        if (healPulseTimer <= 0f)
        {
            SetLineVisible(pulseRing, false);
            return;
        }

        float normalized = Mathf.Clamp01(1f - healPulseTimer / healPulseDuration);
        float eased = EaseOutCubic(normalized);
        float pulseRadius = Mathf.Lerp(radius * 0.18f, radius * 1.25f, eased);
        float alpha = Mathf.Lerp(0.95f, 0f, normalized);
        UpdateCircle(pulseRing, pulseRadius, healColor, alpha, pulseRingWidth);
    }

    private void UpdateLight(Color stateColor, float radius, bool used, bool pulsing)
    {
        if (coreLight == null)
            return;

        coreLight.enabled = enablePointLight;
        if (!enablePointLight)
            return;

        float baseIntensity = pulsing ? healLightIntensity : used ? spentLightIntensity : readyLightIntensity;
        float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        coreLight.color = stateColor;
        coreLight.intensity = Mathf.Max(0f, baseIntensity * flicker);
        coreLight.range = Mathf.Max(2.5f, radius * (pulsing ? 1.25f : 0.8f));
    }

    private void UpdateParticles(Color stateColor, bool used, bool pulsing)
    {
        if (motes == null)
            return;

        ParticleSystem.EmissionModule emission = motes.emission;
        emission.rateOverTime = pulsing ? healParticleRate : used ? spentParticleRate : readyParticleRate;

        ParticleSystem.MainModule main = motes.main;
        main.startColor = stateColor;

        if (!motes.isPlaying)
            motes.Play();
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

    private void UpdateCrossLine(LineRenderer line, Vector3 start, Vector3 end, Color color, float alpha)
    {
        if (line == null)
            return;

        bool visible = alpha > 0.01f;
        SetLineVisible(line, visible);
        if (!visible)
            return;

        color.a = Mathf.Clamp01(alpha);
        line.loop = false;
        line.positionCount = 2;
        line.widthMultiplier = crossLineWidth;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private void SetLineVisible(LineRenderer line, bool visible)
    {
        if (line != null && line.enabled != visible)
            line.enabled = visible;
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
