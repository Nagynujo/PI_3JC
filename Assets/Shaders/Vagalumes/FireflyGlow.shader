// Shader aditivo para vagalumes. Não precisa de textura: o brilho é desenhado proceduralmente.
// Funciona no Built-in e no URP (shader unlit). Para HDRP, use um Shader Graph equivalente.
Shader "Custom/FireflyGlow"
{
    Properties
    {
        _Intensity ("Intensidade (HDR, ajuda no Bloom)", Range(0, 10)) = 3
        _Core ("Tamanho do nucleo", Range(0.02, 0.6)) = 0.15
        _Falloff ("Queda do halo", Range(1, 8)) = 3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            float _Intensity;
            float _Core;
            float _Falloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float d = length(p);

                // halo suave + nucleo brilhante
                float halo = pow(saturate(1.0 - d), _Falloff);
                float core = saturate(1.0 - d / _Core);
                core *= core;

                float val = halo * 0.7 + core;
                float3 tint = lerp(i.color.rgb, float3(1, 1, 1), core * 0.6); // centro mais branco
                return float4(tint * val * _Intensity, i.color.a);
            }
            ENDCG
        }
    }
}
