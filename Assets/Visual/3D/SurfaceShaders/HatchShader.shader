Shader "Custom/General/HatchShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTexture("Base Texture", 2D) = "white" {}
        _EmissionStrength("Emission Strength", Float) = 5.0
        _GlossinessStrength("Glossiness Strength", Range(0.0, 1.0)) = 0.9
        _ORMETexture("ORME Texture", 2D) = "red" {}
        _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 1.0
        [Normal] _NormalTexture("Normal Texture", 2D) = "bump" {}
        _HatchWidth("Hatch Width", Float) = 0.001
        _ShadowEdges("Shadow Edges", Vector) = (0.2, 0.6, 0.9)
        _FresnelPower("Fresnel Power", Range(1.0, 20.0)) = 4.0
        _FresnelColor("Fresnel Color", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags{"RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry"}

        Pass
        {
            Tags{"LightMode" = "UniversalForward"}

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Visual/3D/SurfaceShaders/CrossHatching.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseTexture_ST;
                float _EmissionStrength;
                float _NormalStrength;
                float _GlossinessStrength;
                float3 _ShadowEdges;
                float _HatchWidth;
                float _FresnelPower;
                float4 _FresnelColor;
            CBUFFER_END

            TEXTURE2D(_BaseTexture);
            SAMPLER(sampler_BaseTexture);
            
            TEXTURE2D(_ORMETexture);
            SAMPLER(sampler_ORMETexture);

            TEXTURE2D(_NormalTexture);
            SAMPLER(sampler_NormalTexture);
            
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 dynamicLightmapUV : TEXCOORD2;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float2 dynamicLightmapUV : TEXCOORD5;
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseTexture);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.viewWS = GetWorldSpaceViewDir(o.positionWS);
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                o.dynamicLightmapUV = v.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;

                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                float3 normalWS = NormalizeNormalPerPixel(i.normalWS);
                float3 viewWS = normalize(i.viewWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                float4 shadowMask = SAMPLE_SHADOWMASK(i.dynamicLightmapUV);
                
                float3 baseColor = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, i.uv) * _BaseColor;
                float4 ORME = SAMPLE_TEXTURE2D(_ORMETexture, sampler_ORMETexture, i.uv);
                float occlusion = ORME.r;
                float roughness = 1 - ORME.g;
                float metalness = ORME.b;
                float3 emission = ORME.a * _EmissionStrength * baseColor;

                // Sample normal map and convert to world normal.
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, i.uv), _NormalStrength);
                float3 binormalWS = cross(normalWS, i.tangentWS.xyz) * i.tangentWS.w * unity_WorldTransformParams.w;
                normalWS = normalize(
                    normalTS.x * i.tangentWS.xyz +
                    normalTS.y * binormalWS +
                    normalTS.z * normalWS);
                    
                //float3 ambientLighting = SampleSH(normalWS);

                // MAIN LIGHT
                Light mainLight = GetMainLight(shadowCoord);
                // Shadows
                float shadows = saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation * occlusion;
                // Color
                float3 mainLightColor = mainLight.color * step(_ShadowEdges.x, shadows);
                // Specular
                float3 reflectedVector = reflect(-mainLight.direction, normalWS);
                float specularMask = step(_GlossinessStrength, saturate(dot(reflectedVector, viewWS)));
                float3 specularLighting = (specularMask * roughness) * mainLightColor;
                // Specular metalic
                float3 metalShine = (specularMask * roughness * 5) * baseColor.rgb;
                specularLighting = lerp(specularLighting, metalShine, metalness);
                // Fresnel
                float3 fresnelLighting = pow(1.0f - saturate(dot(normalWS, viewWS)), _FresnelPower) * _FresnelColor.rgb;
                
                // ADDITIONAL LIGHTS
                float3 allAddLightColor = (float3)0;
                #ifdef _ADDITIONAL_LIGHTS
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                    uint lightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light light = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                        // Shadows
                        float NdotL = saturate(dot(normalWS, light.direction));
                        float distIntensity = light.distanceAttenuation * length(light.color);
                        float addShadow = NdotL * light.shadowAttenuation * distIntensity;
                        shadows = max(addShadow, shadows);

                        // Color
                        float3 addLightColor = light.color * step(_ShadowEdges.x, addShadow);
                        allAddLightColor += addLightColor;

                        // Specular
                        // reflectedVector = reflect(-light.direction, normalWS);
                        // specularMask = step(_GlossinessStrength, saturate(dot(reflectedVector, viewWS)));
                        // specularLighting += addLightColor * specularMask * roughness;
                    LIGHT_LOOP_END
                #endif //_ADDITIONAL_LIGHTS
                
                // Hatch Shadow
                float hatchShadow;
                float dist = distance(i.positionWS, _WorldSpaceCameraPos);
                float width = floor(log2(dist + 2)) * _HatchWidth;
                const float sin45 = 0.70710678;
                float2 centeredUV = i.uv - 0.5;
                float2 rotatedUV = float2(
                    centeredUV.x * sin45 - centeredUV.y * sin45,
                    centeredUV.x * sin45 + centeredUV.y * sin45
                ) + 0.5;
                CrossHatchingDithering_float(shadows, rotatedUV, width, _ShadowEdges, 0.1, hatchShadow);
                hatchShadow = 1 - hatchShadow;

                // Combine Base Color with lighting.
                float3 finalColor = (((mainLightColor + allAddLightColor) * baseColor.rgb)
                    + allAddLightColor * 0.3 + specularLighting) * hatchShadow + fresnelLighting + emission;

                return float4(finalColor, 1.0);
            }

            ENDHLSL
        }


        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex shadowPassVert
            #pragma fragment shadowPassFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(float3 positionOS, float3 normalOS)
            {
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);

                return positionCS;
            }

            v2f shadowPassVert(appdata v)
            {
                v2f o = (v2f)0;

                o.positionCS = GetShadowPositionHClip(v.positionOS, v.normalOS);

                return o;
            }

            float4 shadowPassFrag(v2f i) : SV_TARGET
            {
                return 0;
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex depthOnlyVert
            #pragma fragment depthOnlyFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
            };

            v2f depthOnlyVert(appdata v)
            {
                v2f o = (v2f)0;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                return o;
            }

            float depthOnlyFrag(v2f i) : SV_TARGET
            {
                return i.positionCS.z;
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex depthNormalsVert
            #pragma fragment depthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseTexture_ST;
                float _NormalStrength;
                float3 _AmbientLighting;
                float _Glossiness;
                float _FresnelPower;
                float _FresnelStrength;
            CBUFFER_END

            TEXTURE2D(_NormalTexture);
            SAMPLER(sampler_NormalTexture);

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
            };

            v2f depthNormalsVert(appdata v)
            {
                v2f o = (v2f)0;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseTexture);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.normalWS = NormalizeNormalPerVertex(normalWS);
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);

                return o;
            }

            float4 depthNormalsFrag(v2f i) : SV_TARGET
            {
                float3 normalWS = NormalizeNormalPerPixel(i.normalWS);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, i.uv), _NormalStrength);

                float3 binormalWS = cross(normalWS, i.tangentWS.xyz) * i.tangentWS.w * unity_WorldTransformParams.w;
                normalWS = normalize(
                    normalTS.x * i.tangentWS.xyz +
                    normalTS.y * binormalWS +
                    normalTS.z * normalWS);

                return float4(normalWS, 0.0f);
            }

            ENDHLSL
        }
    }
}
