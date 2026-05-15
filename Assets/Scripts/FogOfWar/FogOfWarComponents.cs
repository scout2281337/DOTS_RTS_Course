using Unity.Entities;
using Unity.Mathematics;

public struct VisionSource : IComponentData
{
    public float Radius;
    public float AngleDegrees;
}

public struct FogRevealable : IComponentData
{
}

public struct FogVisible : IComponentData, IEnableableComponent
{
}

public struct FogOfWarSettings : IComponentData
{
    public float2 WorldCenter;
    public float2 WorldSize;
    public int TextureSize;
    public int ObstacleLayerMask;
    public float PlaneY;
    public float RayHeight;
    public float EdgeSoftness;
    public float FogAlpha;
    public bool DrawDebugVisionCenters;
    public bool FlipVisibilityY;
}
