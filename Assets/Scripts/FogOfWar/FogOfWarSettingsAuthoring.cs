using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FogOfWarSettingsAuthoring : MonoBehaviour
{
    public static FogOfWarSettingsAuthoring Active { get; private set; }

    public Vector2 worldCenter = Vector2.zero;
    public Vector2 worldSize = new Vector2(120f, 120f);
    public int textureSize = 1024;
    public LayerMask obstacleLayerMask = 0;
    public float planeY = 0.08f;
    public float rayHeight = 0.8f;
    [Range(0.01f, 0.8f)] public float edgeSoftness = 0.18f;
    [Range(0f, 1f)] public float fogAlpha = 0.78f;
    public bool flipVisibilityY = true;
    public bool drawDebugVisionCenters;

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    public FogOfWarSettings ToSettings()
    {
        return new FogOfWarSettings
        {
            WorldCenter = worldCenter,
            WorldSize = math.max(new float2(worldSize.x, worldSize.y), new float2(1f, 1f)),
            TextureSize = math.clamp(textureSize, 256, 4096),
            ObstacleLayerMask = obstacleLayerMask.value,
            PlaneY = planeY,
            RayHeight = rayHeight,
            EdgeSoftness = edgeSoftness,
            FogAlpha = fogAlpha,
            DrawDebugVisionCenters = drawDebugVisionCenters,
            FlipVisibilityY = flipVisibilityY
        };
    }

    public class Baker : Baker<FogOfWarSettingsAuthoring>
    {
        public override void Bake(FogOfWarSettingsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, authoring.ToSettings());
        }
    }
}
