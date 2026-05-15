Shader "Hidden/DOTSRTS/FogOfWarMask"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.vertex.x * 2.0 - 1.0, v.vertex.y * 2.0 - 1.0, 0.0, 1.0);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(i.color.aaa, 1);
            }
            ENDCG
        }
    }
}
