using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class FogOfWarVisibilitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        FogOfWarSettings settings = GetSettings();

        foreach ((RefRO<LocalTransform> targetTransform, RefRO<Unit> targetUnit, Entity targetEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Unit>>()
                     .WithAll<FogRevealable>()
                     .WithEntityAccess())
        {
            if (!EntityManager.HasComponent<FogVisible>(targetEntity))
                continue;

            bool visible = false;

            foreach ((RefRO<LocalTransform> sourceTransform, RefRO<VisionSource> visionSource, RefRO<Unit> sourceUnit, Entity sourceEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<VisionSource>, RefRO<Unit>>()
                         .WithNone<DeadUnit>()
                         .WithEntityAccess())
            {
                if (sourceUnit.ValueRO.faction != Faction.Friendly)
                    continue;

                if (sourceUnit.ValueRO.faction == targetUnit.ValueRO.faction)
                    continue;

                if (CanSeeTarget(
                        sourceTransform.ValueRO,
                        targetTransform.ValueRO,
                        visionSource.ValueRO,
                        settings))
                {
                    visible = true;
                    break;
                }
            }

            EntityManager.SetComponentEnabled<FogVisible>(targetEntity, visible);
        }
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
        if (settings.ObstacleLayerMask == -1)
            settings.ObstacleLayerMask = 0;

        settings.ObstacleLayerMask &= ~(1 << GameAssets.UNITS_LAYER);
        return settings;
    }

    private static bool CanSeeTarget(
        in LocalTransform sourceTransform,
        in LocalTransform targetTransform,
        in VisionSource visionSource,
        in FogOfWarSettings settings)
    {
        float radius = math.max(0f, visionSource.Radius);
        if (radius <= 0f)
            return false;

        float3 sourcePos = sourceTransform.Position;
        float3 targetPos = targetTransform.Position;
        float3 flatToTarget = new float3(targetPos.x - sourcePos.x, 0f, targetPos.z - sourcePos.z);
        float distanceSq = math.lengthsq(flatToTarget);

        if (distanceSq > radius * radius)
            return false;

        if (!settings.ForceFullCircleVision && visionSource.AngleDegrees < 359f && distanceSq > 0.0001f)
        {
            float3 forward = math.forward(sourceTransform.Rotation);
            forward.y = 0f;

            if (math.lengthsq(forward) > 0.0001f)
            {
                forward = math.normalize(forward);
                float3 dir = math.normalize(flatToTarget);
                float halfAngle = math.clamp(visionSource.AngleDegrees, 1f, 360f) * 0.5f;
                float minDot = math.cos(math.radians(halfAngle));

                if (math.dot(forward, dir) < minDot)
                    return false;
            }
        }

        float3 rayStart = sourcePos + new float3(0f, settings.RayHeight, 0f);
        float3 rayEnd = targetPos + new float3(0f, settings.RayHeight, 0f);

        return !Physics.Linecast(
            rayStart,
            rayEnd,
            settings.ObstacleLayerMask,
            QueryTriggerInteraction.Ignore);
    }
}
