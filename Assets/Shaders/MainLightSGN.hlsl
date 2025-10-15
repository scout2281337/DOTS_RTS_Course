#ifndef ADDITIONAL_LIGHT_INCLUDED
#define ADDITIONAL_LIGHT_INCLUDED

void MainLight_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
    direction = normalize(float3(1.0f, 1.0f, 0.0f));
    color = 1.0f;
    shadowAtten = 1.0f;
#else
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
        float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
        Light mainLight = GetMainLight(shadowCoord);
        direction = mainLight.direction;
        color = mainLight.color;
        shadowAtten = mainLight.shadowAttenuation;
    #else
        direction = normalize(float3(-0.5, 0.5, -0.5));
        color = float3(1, 1, 1);
        shadowAtten = 1;
    #endif
#endif
}

void MainLight_half(half3 WorldPos, out half3 Direction, out half3 Color, out half Attenuation)
{
#ifdef SHADERGRAPH_PREVIEW
    Direction = normalize(half3(1.0f, 1.0f, 0.0f));
    Color = 1.0f;
    Attenuation = 1.0f;
#else
    Light mainLight = GetMainLight();
    Direction = mainLight.direction;
    Color = mainLight.color;
    Attenuation = mainLight.shadowAttenuation;
#endif
}

void AdditionalLight_float(float3 WorldPos, int lightID, out float3 Direction, out float3 Color, out float Attenuation)
{
    Direction = normalize(float3(1.0f, 1.0f, 0.0f));
    Color = 0.0f;
    Attenuation = 0.0f;

#ifndef SHADERGRAPH_PREVIEW
    int lightCount = GetAdditionalLightsCount();
    if(lightID < lightCount)
    {
        Light light = GetAdditionalLight(lightID, WorldPos);
        Direction = light.direction;
        Color = light.color;
        Attenuation = light.distanceAttenuation;
    }
#endif
}

void AdditionalLight_half(half3 WorldPos, int lightID, out half3 Direction, out half3 Color, out half Attenuation)
{
    Direction = normalize(half3(1.0f, 1.0f, 0.0f));
    Color = 0.0f;
    Attenuation = 0.0f;

#ifndef SHADERGRAPH_PREVIEW
    int lightCount = GetAdditionalLightsCount();
    if(lightID < lightCount)
    {
        Light light = GetAdditionalLight(lightID, WorldPos);
        Direction = light.direction;
        Color = light.color;
        Attenuation = light.distanceAttenuation;
    }
#endif
}

void AllAdditionalLights_float(float3 PositionWS, float3 NormalWS, float2 CutoffThresholds, out float3 LightColor)
{
    LightColor = 0.0f;

#ifndef SHADERGRAPH_PREVIEW
    int lightCount = GetAdditionalLightsCount();

    for(int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, PositionWS);

        float3 color = dot(light.direction, normalize(NormalWS));
        color = step(CutoffThresholds.x, color);
        color *= light.color;
        color *= light.distanceAttenuation;

        LightColor += color;
    } 
#endif
}

void AllAdditionalLights_half(half3 WorldPos, half3 WorldNormal, half2 CutoffThresholds, out half3 LightColor)
{
    LightColor = 0.0f;

#ifndef SHADERGRAPH_PREVIEW
    int lightCount = GetAdditionalLightsCount();

    for(int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, WorldPos);
        
        float3 color = dot(light.direction, WorldNormal);
        color = smoothstep(CutoffThresholds.x, CutoffThresholds.y, color);
        color *= light.color;
        color *= light.distanceAttenuation;

        LightColor += color;
    } 
#endif
}

#endif // ADDITIONAL_LIGHT_INCLUDED