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

void AllAdditionalLights_float(float3 PositionWS, float3 NormalWS, float CutoffThreshold, out float3 LightColor)
{

    LightColor = 0.0;

#ifndef SHADERGRAPH_PREVIEW
    
    uint pixelLightCount = GetAdditionalLightsCount();

#if USE_FORWARD_PLUS
    // for Foward+ LIGHT_LOOP_BEGIN macro uses inputData.normalizedScreenSpaceUV and inputData.positionWS
    InputData inputData = (InputData)0;
    float4 screenPos = ComputeScreenPos(TransformWorldToHClip(PositionWS));
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
    inputData.positionWS = PositionWS;
#endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
		#if !USE_FORWARD_PLUS
			lightIndex = GetPerObjectLightIndex(lightIndex);
		#endif
		Light light = GetAdditionalPerObjectLight(lightIndex, PositionWS);
        float NdotL = saturate(dot(NormalWS, light.direction));
        float thisDiffuse = step(CutoffThreshold, NdotL);
        LightColor += light.color * thisDiffuse;
    LIGHT_LOOP_END

#endif
}
#endif // ADDITIONAL_LIGHT_INCLUDED