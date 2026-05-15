Shader "Hidden/DOTSRTS/FogOfWarOverlay"
{
    Properties
    {
        _VisibilityTex ("Visibility Texture", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0.025, 0.035, 0.045, 1)
        _FogAlpha ("Fog Alpha", Range(0, 1)) = 0.78
        _FlipVisibilityY ("Flip Visibility Y", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+80"
            "RenderType" = "Transparent"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _VisibilityTex;
            float4 _VisibilityTex_TexelSize;
            fixed4 _FogColor;
            float _FogAlpha;
            float _FlipVisibilityY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 visibilityUv = i.uv;

                if (_FlipVisibilityY > 0.5)
                {
                    visibilityUv.y = 1.0 - visibilityUv.y;
                }

                float visible = saturate(tex2D(_VisibilityTex, visibilityUv).r);
                fixed4 col = _FogColor;
                col.a = _FogAlpha * (1.0 - visible);
                return col;
            }
            ENDCG
        }
    }
}
