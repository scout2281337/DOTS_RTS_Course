#ifndef MainLight_INCLUDED
#define MainLight_INCLUDED

#ifndef SHADERGRAPH_PREVIEW

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

float3 CelShadedLightFunction(float3 normalWS, Light light, float cutOffThreshold)
{
    float NdotL = saturate(dot(normalWS, light.direction));
    float lightMask = step(cutOffThreshold, NdotL * light.distanceAttenuation * length(light.color));
    return light.color * lightMask;
}
#endif

void MainLight_float(float3 worldPos, out float3 direction, out float3 baseColor, out float shadowAtten)
{
    #ifdef SHADERGRAPH_PREVIEW
        direction = normalize(float3(-0.5, 0.5, -0.5));
        baseColor = float3(1, 1, 1);
        shadowAtten = 1;

    #else
        #if defined (UNIVERSAL_PIPELINE_CORE_INCLUDED)
            float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
            Light mainLight = GetMainLight(shadowCoord);
            direction = mainLight.direction;
            baseColor = mainLight.color;
            shadowAtten = mainLight.shadowAttenuation;
        #else
            direction = normalize(float3(-0.5, 0.5, -0.5));
            baseColor = float3(1, 1, 1);
            shadowAtten = 1;
        #endif
    #endif
}

void AllAdditionalCelShadedLights_float(float3 positionWS, float3 normalWS, float3 viewWS, float cutOffThreshold, out float3 lightColor)
{
    lightColor = 0.0;

    #ifndef SHADERGRAPH_PREVIEW
        InputData inputData = (InputData)0;
        inputData.positionWS = positionWS;
        inputData.normalWS = normalWS;
        inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(viewWS);

        #ifdef _ADDITIONAL_LIGHTS

            #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    lightColor += CelShadedLightFunction(inputData.normalWS, additionalLight, cutOffThreshold);
                }
            #endif

            uint pixelLightCount = GetAdditionalLightsCount();
            LIGHT_LOOP_BEGIN(pixelLightCount)
                Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                lightColor += CelShadedLightFunction(inputData.normalWS, additionalLight, cutOffThreshold);
            LIGHT_LOOP_END
    
        #endif

    #endif
}
#endif