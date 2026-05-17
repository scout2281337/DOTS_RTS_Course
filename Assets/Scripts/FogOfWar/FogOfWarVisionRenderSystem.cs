using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class FogOfWarVisionRenderSystem : SystemBase
{
    private const int RayCount = 96;
    private const float RebuildInterval = 0.05f;

    private readonly List<Vector3> maskVertices = new();
    private readonly List<Color> colors = new();
    private readonly List<int> triangles = new();

    private Mesh maskMesh;
    private Mesh overlayMesh;
    private Material maskMaterial;
    private Material overlayMaterial;
    private RenderTexture visibilityTexture;
    private FogOfWarSettings cachedSettings;
    private float rebuildTimer;

    protected override void OnCreate()
    {
        maskMesh = new Mesh
        {
            name = "Fog Of War Visibility Mask Mesh"
        };
        maskMesh.MarkDynamic();

        overlayMesh = new Mesh
        {
            name = "Fog Of War Overlay Plane"
        };

        maskMaterial = CreateMaterial("Hidden/DOTSRTS/FogOfWarMask", "Fog Of War Mask Material");
        overlayMaterial = CreateMaterial("Hidden/DOTSRTS/FogOfWarOverlay", "Fog Of War Overlay Material");
    }

    protected override void OnDestroy()
    {
        ReleaseTexture();

        if (maskMesh != null)
            Object.Destroy(maskMesh);

        if (overlayMesh != null)
            Object.Destroy(overlayMesh);

        if (maskMaterial != null)
            Object.Destroy(maskMaterial);

        if (overlayMaterial != null)
            Object.Destroy(overlayMaterial);
    }

    protected override void OnUpdate()
    {
        FogOfWarSettings settings = GetSettings();
        EnsureResources(settings);

        rebuildTimer -= SystemAPI.Time.DeltaTime;
        if (rebuildTimer <= 0f)
        {
            rebuildTimer = RebuildInterval;
            RebuildMaskMesh(settings);
            RenderVisibilityMask();
        }

        if (overlayMaterial != null && overlayMesh != null && visibilityTexture != null)
        {
            overlayMaterial.SetTexture("_VisibilityTex", visibilityTexture);
            overlayMaterial.SetFloat("_FogAlpha", math.saturate(settings.FogAlpha));
            overlayMaterial.SetFloat("_FlipVisibilityY", settings.FlipVisibilityY ? 1f : 0f);
            Graphics.DrawMesh(overlayMesh, Matrix4x4.identity, overlayMaterial, 0);
        }

        if (settings.DrawDebugVisionCenters)
            DrawDebugVisionCenters(settings);
    }

    private FogOfWarSettings GetSettings()
    {
        if (FogOfWarSettingsAuthoring.Active != null)
            return Sanitize(FogOfWarSettingsAuthoring.Active.ToSettings());

        if (SystemAPI.TryGetSingleton<FogOfWarSettings>(out FogOfWarSettings settings))
            return Sanitize(settings);

        return Sanitize(new FogOfWarSettings
        {
            WorldCenter = float2.zero,
            WorldSize = new float2(120f, 120f),
            TextureSize = 1024,
            ObstacleLayerMask = 0,
            PlaneY = 0.08f,
            RayHeight = 0.8f,
            EdgeSoftness = 0.18f,
            FogAlpha = 0.78f,
            DrawDebugVisionCenters = false,
            FlipVisibilityY = true,
            ForceFullCircleVision = false
        });
    }

    private static FogOfWarSettings Sanitize(FogOfWarSettings settings)
    {
        settings.WorldSize = math.max(settings.WorldSize, new float2(1f, 1f));
        settings.TextureSize = math.clamp(settings.TextureSize, 256, 4096);
        settings.EdgeSoftness = math.clamp(settings.EdgeSoftness, 0.01f, 0.8f);
        settings.FogAlpha = math.saturate(settings.FogAlpha);
        settings.ObstacleLayerMask = SanitizeObstacleMask(settings.ObstacleLayerMask);
        return settings;
    }

    private static int SanitizeObstacleMask(int mask)
    {
        if (mask == -1)
            return 0;

        return mask & ~(1 << GameAssets.UNITS_LAYER);
    }

    private void EnsureResources(FogOfWarSettings settings)
    {
        if (visibilityTexture == null || visibilityTexture.width != settings.TextureSize)
        {
            ReleaseTexture();
            visibilityTexture = new RenderTexture(settings.TextureSize, settings.TextureSize, 0, RenderTextureFormat.R8)
            {
                name = "Fog Of War Visibility Mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false
            };
            visibilityTexture.Create();
        }

        if (!SettingsMatch(cachedSettings, settings))
        {
            cachedSettings = settings;
            RebuildOverlayMesh(settings);
        }
    }

    private static bool SettingsMatch(FogOfWarSettings a, FogOfWarSettings b)
    {
        return math.all(a.WorldCenter == b.WorldCenter) &&
               math.all(a.WorldSize == b.WorldSize) &&
               a.PlaneY == b.PlaneY &&
               a.TextureSize == b.TextureSize;
    }

    private void ReleaseTexture()
    {
        if (visibilityTexture == null)
            return;

        visibilityTexture.Release();
        Object.Destroy(visibilityTexture);
        visibilityTexture = null;
    }

    private static Material CreateMaterial(string shaderName, string materialName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"Shader '{shaderName}' not found. Fog of war visuals will be disabled until Unity imports the shader.");
            return null;
        }

        return new Material(shader)
        {
            name = materialName
        };
    }

    private void RebuildOverlayMesh(FogOfWarSettings settings)
    {
        float2 half = settings.WorldSize * 0.5f;
        float minX = settings.WorldCenter.x - half.x;
        float maxX = settings.WorldCenter.x + half.x;
        float minZ = settings.WorldCenter.y - half.y;
        float maxZ = settings.WorldCenter.y + half.y;
        float y = settings.PlaneY;

        overlayMesh.Clear();
        overlayMesh.SetVertices(new[]
        {
            new Vector3(minX, y, minZ),
            new Vector3(maxX, y, minZ),
            new Vector3(maxX, y, maxZ),
            new Vector3(minX, y, maxZ)
        });
        overlayMesh.SetUVs(0, new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        });
        overlayMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        overlayMesh.RecalculateBounds();
    }

    private void RebuildMaskMesh(FogOfWarSettings settings)
    {
        maskVertices.Clear();
        colors.Clear();
        triangles.Clear();

        foreach ((RefRO<LocalTransform> transform, RefRO<VisionSource> visionSource, RefRO<Unit> unit) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<VisionSource>, RefRO<Unit>>()
                     .WithNone<DeadUnit>())
        {
            if (unit.ValueRO.faction != Faction.Friendly)
                continue;

            AppendVisionFan(transform.ValueRO, visionSource.ValueRO, settings);
        }

        IReadOnlyList<FogRevealSource> revealSources = FogRevealSource.ActiveSources;
        for (int i = 0; i < revealSources.Count; i++)
        {
            FogRevealSource source = revealSources[i];
            if (source == null || !source.IsRevealing)
                continue;

            AppendRevealCircle(source.Position, source.Radius, source.RespectObstacles, settings);
        }

        maskMesh.Clear();
        if (maskVertices.Count == 0)
            return;

        maskMesh.SetVertices(maskVertices);
        maskMesh.SetColors(colors);
        maskMesh.SetTriangles(triangles, 0);
        maskMesh.RecalculateBounds();
    }

    private void AppendVisionFan(in LocalTransform transform, in VisionSource visionSource, in FogOfWarSettings settings)
    {
        float radius = math.max(0f, visionSource.Radius);
        if (radius <= 0f)
            return;

        float angle = settings.ForceFullCircleVision
            ? 360f
            : math.clamp(visionSource.AngleDegrees, 1f, 360f);
        int rayCount = angle >= 359f ? RayCount : math.max(6, (int)math.ceil(RayCount * angle / 360f));
        int centerIndex = maskVertices.Count;

        float3 origin = transform.Position;
        AddMaskVertex(origin, 1f, settings);

        float3 forward = math.forward(transform.Rotation);
        forward.y = 0f;
        if (math.lengthsq(forward) < 0.0001f)
            forward = new float3(0f, 0f, 1f);
        else
            forward = math.normalize(forward);

        float baseAngle = math.atan2(forward.x, forward.z);
        float startAngle = angle >= 359f ? 0f : baseAngle - math.radians(angle) * 0.5f;
        float angleStep = angle >= 359f ? math.PI * 2f / rayCount : math.radians(angle) / rayCount;
        float innerT = 1f - settings.EdgeSoftness;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float3 dir = new float3(math.sin(currentAngle), 0f, math.cos(currentAngle));
            float3 edge = CastVisionEdge(origin, dir, radius, settings, true);
            float3 inner = math.lerp(origin, edge, innerT);

            AddMaskVertex(inner, 1f, settings);
            AddMaskVertex(edge, 0f, settings);

            if (i == 0)
                continue;

            int prevInner = centerIndex + 1 + (i - 1) * 2;
            int prevOuter = prevInner + 1;
            int innerIndex = centerIndex + 1 + i * 2;
            int outerIndex = innerIndex + 1;

            triangles.Add(centerIndex);
            triangles.Add(prevInner);
            triangles.Add(innerIndex);

            triangles.Add(prevInner);
            triangles.Add(prevOuter);
            triangles.Add(outerIndex);

            triangles.Add(prevInner);
            triangles.Add(outerIndex);
            triangles.Add(innerIndex);
        }
    }

    private void AppendRevealCircle(Vector3 sourcePosition, float radius, bool respectObstacles, in FogOfWarSettings settings)
    {
        radius = math.max(0f, radius);
        if (radius <= 0f)
            return;

        int centerIndex = maskVertices.Count;
        float3 origin = new float3(sourcePosition.x, sourcePosition.y, sourcePosition.z);
        AddMaskVertex(origin, 1f, settings);

        float innerT = 1f - settings.EdgeSoftness;

        for (int i = 0; i <= RayCount; i++)
        {
            float currentAngle = math.PI * 2f / RayCount * i;
            float3 dir = new float3(math.sin(currentAngle), 0f, math.cos(currentAngle));
            float3 edge = CastVisionEdge(origin, dir, radius, settings, respectObstacles);
            float3 inner = math.lerp(origin, edge, innerT);

            AddMaskVertex(inner, 1f, settings);
            AddMaskVertex(edge, 0f, settings);

            if (i == 0)
                continue;

            int prevInner = centerIndex + 1 + (i - 1) * 2;
            int prevOuter = prevInner + 1;
            int innerIndex = centerIndex + 1 + i * 2;
            int outerIndex = innerIndex + 1;

            triangles.Add(centerIndex);
            triangles.Add(prevInner);
            triangles.Add(innerIndex);

            triangles.Add(prevInner);
            triangles.Add(prevOuter);
            triangles.Add(outerIndex);

            triangles.Add(prevInner);
            triangles.Add(outerIndex);
            triangles.Add(innerIndex);
        }
    }

    private void AddMaskVertex(float3 worldPosition, float alpha, in FogOfWarSettings settings)
    {
        float2 min = settings.WorldCenter - settings.WorldSize * 0.5f;
        float u = (worldPosition.x - min.x) / settings.WorldSize.x;
        float v = (worldPosition.z - min.y) / settings.WorldSize.y;

        maskVertices.Add(new Vector3(u, v, 0f));
        colors.Add(new Color(alpha, alpha, alpha, alpha));
    }

    private static float3 CastVisionEdge(float3 origin, float3 dir, float radius, in FogOfWarSettings settings, bool respectObstacles)
    {
        Vector3 start = new Vector3(origin.x, origin.y + settings.RayHeight, origin.z);

        if (respectObstacles && Physics.Raycast(
                start,
                new Vector3(dir.x, 0f, dir.z),
                out RaycastHit hit,
                radius,
                settings.ObstacleLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return new float3(hit.point.x, origin.y, hit.point.z);
        }

        return origin + dir * radius;
    }

    private void DrawDebugVisionCenters(FogOfWarSettings settings)
    {
        foreach ((RefRO<LocalTransform> transform, RefRO<VisionSource> visionSource, RefRO<Unit> unit) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<VisionSource>, RefRO<Unit>>()
                     .WithNone<DeadUnit>())
        {
            if (unit.ValueRO.faction != Faction.Friendly)
                continue;

            Vector3 center = new Vector3(transform.ValueRO.Position.x, settings.PlaneY + 0.12f, transform.ValueRO.Position.z);
            float radius = math.max(0.25f, visionSource.ValueRO.Radius);
            Debug.DrawLine(center + Vector3.left * 0.5f, center + Vector3.right * 0.5f, Color.cyan);
            Debug.DrawLine(center + Vector3.back * 0.5f, center + Vector3.forward * 0.5f, Color.cyan);
            Debug.DrawLine(center, center + Vector3.forward * math.min(radius, 2f), Color.cyan);
        }

        IReadOnlyList<FogRevealSource> revealSources = FogRevealSource.ActiveSources;
        for (int i = 0; i < revealSources.Count; i++)
        {
            FogRevealSource source = revealSources[i];
            if (source == null || !source.IsRevealing)
                continue;

            Vector3 center = source.Position + Vector3.up * (settings.PlaneY + 0.15f);
            Debug.DrawLine(center + Vector3.left, center + Vector3.right, Color.yellow);
            Debug.DrawLine(center + Vector3.back, center + Vector3.forward, Color.yellow);
        }
    }

    private void RenderVisibilityMask()
    {
        if (visibilityTexture == null || maskMaterial == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = visibilityTexture;

        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Clear(true, true, Color.black);

        if (maskMesh.vertexCount > 0 && maskMaterial.SetPass(0))
            Graphics.DrawMeshNow(maskMesh, Matrix4x4.identity);

        GL.PopMatrix();
        RenderTexture.active = previous;
    }
}
