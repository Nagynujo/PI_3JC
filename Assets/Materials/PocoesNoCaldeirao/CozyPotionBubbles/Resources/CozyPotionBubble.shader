Shader "CozyPotion/Rising Bubble URP"
{
    Properties
    {
        _Tint ("Bubble Color", Color) = (1, 0.72, 0.93, 1)
        _Brightness ("Glow", Range(0.5, 4)) = 1.55
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "RisingBubble"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Brightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float r = length(p);

                // Formato circular: os vertices sao um billboard, nao uma esfera 3D.
                float cutout = 1.0 - smoothstep(0.88, 1.0, r);
                float ring = smoothstep(0.54, 0.82, r) * cutout;
                float core = 1.0 - smoothstep(0.0, 0.98, r);

                // Reflexos suaves deslocados para o canto superior esquerdo.
                float2 glintUV = (p - float2(-0.34, 0.38)) / float2(0.23, 0.13);
                float glint = exp(-dot(glintUV, glintUV) * 2.4);
                float2 tinyUV = (p - float2(-0.15, 0.59)) / float2(0.065, 0.065);
                float tinyGlint = exp(-dot(tinyUV, tinyUV) * 2.0);

                float alpha = saturate((0.16 * core + 0.68 * ring + 0.55 * glint + 0.35 * tinyGlint) * cutout);
                alpha *= IN.color.a * _Tint.a;

                float3 baseColor = _Tint.rgb * (0.78 + 0.24 * core);
                float3 sparkle = (ring * 0.26 + glint * 0.7 + tinyGlint * 0.75) * float3(1.0, 0.95, 0.98);
                float3 rgb = (baseColor + sparkle) * IN.color.rgb * _Brightness;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
