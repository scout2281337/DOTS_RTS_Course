#ifndef MainLight_INCLUDED
#define MainLight_INCLUDED

#ifndef SHADERGRAPH_PREVIEW

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

float BlinLightMask(float3 normalWS, Light light)
{
    float NdotL = saturate(dot(normalWS, light.direction));
    float blinMask = NdotL * light.distanceAttenuation * length(light.color);
    
    return blinMask;
}

float3 CelShadedLight(float lightMask, Light light, float cutOffThreshold)
{
    float celShadedMask = step(cutOffThreshold, lightMask);
    
    return light.color * celShadedMask;
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

void AllAdditionalCelShadedLights_float(float3 positionWS, float3 normalWS, float2 screenUV, float cutOffThreshold, out float lightMask, out float3 lightColor)
{
    lightColor = 0.0;
    lightMask = 0.0;

    #ifndef SHADERGRAPH_PREVIEW
        InputData inputData = (InputData)0;
        AmbientOcclusionFactor aoFactor = (AmbientOcclusionFactor)0;
        half4 shadowMask = half4(1, 1, 1, 1);
        inputData.positionWS = positionWS;
        inputData.normalWS = normalWS;
        inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
        inputData.normalizedScreenSpaceUV = screenUV;
        aoFactor.directAmbientOcclusion = 1;
        aoFactor.indirectAmbientOcclusion = 1;

        #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP

            #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    float blinMask = BlinLightMask(inputData.normalWS, additionalLight);
                    lightMask += blinMask;
                    lightColor += CelShadedLight(blinMask, additionalLight, cutOffThreshold);
                }
            #endif

            uint pixelLightCount = GetAdditionalLightsCount();
            LIGHT_LOOP_BEGIN(pixelLightCount)
                Light additionalLight = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                float blinMask = BlinLightMask(inputData.normalWS, additionalLight);
                lightMask += blinMask;
                lightColor += CelShadedLight(blinMask, additionalLight, cutOffThreshold);
            LIGHT_LOOP_END
    
        #endif

    #endif
}
#endif