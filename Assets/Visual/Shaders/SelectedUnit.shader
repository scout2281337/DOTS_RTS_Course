Shader "VFX/SelectedUnit"
{
    Properties
    {
        _HealthAmount ("Health", float) = 1
        _HighColor ("High Color", color) = (0, 1, 0, 1)
        _LowColor ("Low Color", color) = (1, 0, 0, 1)
        _HighEdge ("High Edge", float) = 0
        _LowEdge ("Low Edge", float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _HealthAmount;
                float4 _HighColor;
                float4 _LowColor;
                float _HighEdge;
                float _LowEdge;
            CBUFFER_END

            struct appdata 
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                float SDF = length((i.uv - 0.5) * 2) + 0.5;

                float mask = step(SDF - 0.5, _HighEdge) * (1 - step(SDF - 0.5, _LowEdge));
                float4 disk = lerp(_LowColor, _HighColor, _HealthAmount) * mask;
                
                clip(disk.a - 0.001);

                return disk;
            }

            ENDHLSL
        }
    }
}
