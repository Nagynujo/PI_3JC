// CauldronLiquidMagic.shader  (v2: paleta de 2 cores)
// Líquido roxo cremoso de caldeirão: espiral suave, bolhas glossy, estrelas rosa e poeira dourada.
// O líquido usa APENAS duas cores: Light (#8A4EA8) e Dark (#5C2A75).
// 100% procedural (sem texturas), 1 pass, opaco. Unlit.
// As estrelas e a poeira saem em HDR (valor > 1): ligue HDR na câmera + Bloom para brilharem de verdade.
// Use em uma superfície horizontal (disco/plano) com UV de 0 a 1.
Shader "Custom/CauldronLiquidMagic"
{
    Properties
    {
        [Header(01 Color and Density)]
        _ColorLight ("Light Color (#8A4EA8)", Color) = (0.541, 0.306, 0.659, 1)
        _ColorDark  ("Dark Color (#5C2A75)",  Color) = (0.361, 0.165, 0.459, 1)
        _Density    ("Creamy Density", Range(0,1)) = 0.65

        [Header(02 Swirl)]
        _SwirlSpeed    ("Swirl Speed", Range(-3,3)) = 0.3
        _SwirlTwist    ("Swirl Twist", Range(0,8)) = 2.5
        [IntRange] _SwirlArms ("Spiral Arms", Range(1,6)) = 2
        _SwirlStrength ("Spiral Strength", Range(0,1)) = 0.55

        [Header(03 Turbulence)]
        _TurbScale    ("Turbulence Scale", Range(1,15)) = 4
        _TurbSpeed    ("Turbulence Speed", Range(0,2)) = 0.35
        _TurbStrength ("Turbulence Strength", Range(0,1)) = 0.35

        [Header(04 Bubbles)]
        _BubbleScale  ("Big Bubble Grid Scale", Range(1,6)) = 2.2
        _BubbleAmount ("Bubble Amount", Range(0,1)) = 0.5
        _BubbleSpeed  ("Bubble Speed", Range(0,2)) = 0.35

        [Header(05 Sparkles)]
        [HDR] _StarColor ("Star Color", Color) = (1.0, 0.55, 0.85, 1)
        _StarScale    ("Star Grid Scale", Range(2,12)) = 4.5
        _StarDensity  ("Star Density", Range(0,1)) = 0.22
        _StarSize     ("Star Size (max 0.25)", Range(0.05,0.25)) = 0.2
        _StarSpeed    ("Star Twinkle Speed", Range(0,8)) = 1.6
        [HDR] _DustColor ("Gold Dust Color", Color) = (1.0, 0.78, 0.35, 1)
        _DustScale    ("Dust Grid Scale", Range(5,30)) = 14
        _DustDensity  ("Dust Density", Range(0,1)) = 0.45
        _DustSpeed    ("Dust Twinkle Speed", Range(0,8)) = 2
        _DustDrift    ("Dust Drift", Range(0,1)) = 0.08
        _GlowIntensity("Glow Intensity (HDR)", Range(0,8)) = 2.5

        [Header(06 Finish)]
        _LightDir       ("Fake Light Direction", Vector) = (-0.4, 0.8, 0.4, 0)
        _SpecColor2     ("Sheen Color", Color) = (0.75, 0.55, 0.85, 1)
        _SpecPower      ("Sheen Power", Range(4,128)) = 28
        _SpecStrength   ("Sheen Strength", Range(0,2)) = 0.25
        _NormalStrength ("Ripple Normal Strength", Range(0,40)) = 10
        _RimColor       ("Rim Color", Color) = (0.541, 0.306, 0.659, 1)
        _RimPower       ("Rim Power", Range(0.5,8)) = 3
        _RimStrength    ("Rim Strength", Range(0,1)) = 0.1
        _EdgeColor      ("Edge Color", Color) = (0.361, 0.165, 0.459, 1)
        _EdgeWidth      ("Edge Width", Range(0,0.6)) = 0.3
        _Brightness     ("Brightness", Range(0.5,2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _ColorLight, _ColorDark;
            float  _Density;
            float  _SwirlSpeed, _SwirlTwist, _SwirlArms, _SwirlStrength;
            float  _TurbScale, _TurbSpeed, _TurbStrength;
            float  _BubbleScale, _BubbleAmount, _BubbleSpeed;
            float4 _StarColor, _DustColor;
            float  _StarScale, _StarDensity, _StarSize, _StarSpeed;
            float  _DustScale, _DustDensity, _DustSpeed, _DustDrift, _GlowIntensity;
            float4 _LightDir;
            float4 _SpecColor2;
            float  _SpecPower, _SpecStrength, _NormalStrength;
            float4 _RimColor;
            float  _RimPower, _RimStrength;
            float4 _EdgeColor;
            float  _EdgeWidth, _Brightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float3 wN  : TEXCOORD1;
                float3 wV  : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wN = UnityObjectToWorldNormal(v.normal);
                o.wV = _WorldSpaceCameraPos.xyz - wp;
                return o;
            }

            // ---------- hash / noise ----------
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                float n = hash21(p);
                return float2(n, hash21(p + n + 17.3));
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ---------- 04 Bolhas glossy (esferas sombreadas, cores da paleta) ----------
            float3 bubbleLayer(float3 col, float2 p, float scale, float amount,
                               float t, float seed, float bs, float bc)
            {
                float2 g  = p * scale + seed * 17.0;
                float2 id = floor(g);
                float2 f  = frac(g) - 0.5;

                float  r1 = hash21(id);
                float2 r2 = hash22(id + 7.7);

                float exists = step(r1, amount);
                float life   = frac(t * (0.5 + 0.5 * r2.x) + r2.y);
                float grow   = smoothstep(0.0, 0.18, life);
                float pop    = 1.0 - smoothstep(0.9, 1.0, life);

                float  R = (0.16 + 0.2 * r2.x) * grow;
                float2 q = f - (r2 - 0.5) * 0.2;
                float  d = length(q);

                float m  = (1.0 - smoothstep(R - 0.02, R, d)) * exists * pop;

                // sombra de contato no líquido ao redor da bolha
                float sh = (1.0 - smoothstep(R, R + 0.1, d)) * exists * pop * grow;
                col *= 1.0 - 0.3 * sh;

                // normal de esfera
                float2 u  = q / max(R, 0.001);
                float  uz = sqrt(saturate(1.0 - dot(u, u)));
                // desfaz a rotação da grade para a luz ficar fixa na tela
                float2 uw = float2(bc * u.x + bs * u.y, -bs * u.x + bc * u.y);
                float3 N  = float3(uw, uz);
                float3 L  = normalize(float3(-0.45, 0.55, 0.7));

                float diff = saturate(dot(N, L));
                float3 bcol = lerp(_ColorDark.rgb * 0.9, _ColorLight.rgb * 1.15, diff);
                bcol = lerp(bcol, _ColorDark.rgb * 0.7, pow(1.0 - uz, 2.5) * 0.5);
                // luz rebatida por baixo (dá volume cremoso)
                bcol += _ColorLight.rgb * pow(saturate(dot(N, normalize(float3(0.4, -0.7, 0.3)))), 3.0) * 0.2;

                float spec = pow(saturate(2.0 * dot(N, L) * N.z - L.z), 48.0);
                float hl   = 1.0 - smoothstep(0.0, 0.22, length(uw - float2(-0.38, 0.42)));

                col = lerp(col, bcol, m * 0.95);
                col += (spec * 0.8 + hl * 0.7) * m;
                return col;
            }

            // ---------- 05 Estrelas rosa de 4 pontas (HDR) ----------
            float3 starLayer(float2 p, float scale, float t, float seed)
            {
                float2 g  = p * scale + seed * 31.0;
                float2 id = floor(g);
                float2 f  = frac(g) - 0.5;

                float  r1 = hash21(id + 3.3);
                float2 r2 = hash22(id + 11.1);

                float exists = step(r1, _StarDensity);
                float2 q = f - (r2 - 0.5) * 0.4;

                float tw  = 0.5 + 0.5 * sin(t * _StarSpeed + r2.x * 40.0);
                float arm = _StarSize * (0.35 + 0.65 * tw) * (0.6 + 0.6 * r2.y);
                float k   = sqrt(arm);

                float2 a   = abs(q);
                float  sum = sqrt(a.x) + sqrt(a.y);
                float star = pow(saturate(1.0 - sum / k), 1.5);

                float d    = length(q);
                float halo = pow(saturate(1.0 - d / (arm * 0.9 + 0.001)), 2.0) * 0.35;

                return _StarColor.rgb * (star * 1.5 + halo) * exists * (0.4 + 0.6 * tw);
            }

            // ---------- 05 Poeira dourada brilhante (HDR) ----------
            float3 dustLayer(float2 p, float scale, float t, float seed)
            {
                float2 g  = p * scale + float2(0.0, t * _DustDrift) + seed * 53.0;
                float2 id = floor(g);
                float2 f  = frac(g) - 0.5;

                float  r1 = hash21(id + 5.1);
                float2 r2 = hash22(id + 2.7);

                float exists = step(r1, _DustDensity);
                float2 q = f - (r2 - 0.5) * 0.5;
                float  d = length(q);

                float tw   = 0.5 + 0.5 * sin(t * _DustSpeed + r1 * 60.0);
                float core = 1.0 - smoothstep(0.0, 0.06, d);
                float halo = pow(saturate(1.0 - d / 0.3), 3.0) * 0.6;

                return _DustColor.rgb * (core * 1.5 + halo) * exists * (0.3 + 0.7 * tw);
            }

            float4 frag (v2f i) : SV_Target
            {
                float t  = _Time.y;
                float2 p = (i.uv - 0.5) * 2.0;   // -1..1
                float  r = length(p);

                // ---------- 02 Espiral ----------
                float ang = t * _SwirlSpeed + _SwirlTwist * (1.0 - saturate(r));
                float s, c;
                sincos(ang, s, c);
                float2 sp = float2(c * p.x - s * p.y, s * p.x + c * p.y);

                float a    = atan2(p.y, p.x);
                float warp = vnoise(p * _TurbScale * 0.6 + t * 0.1);
                float ph   = a * _SwirlArms + r * _SwirlTwist * 3.0 - t * _SwirlSpeed * 2.0 + warp * 1.5;
                float bands = smoothstep(0.15, 0.85, 0.5 + 0.5 * sin(ph));

                // ---------- 03 Turbulência ----------
                float n = vnoise(sp * _TurbScale + float2(t, -t * 0.7) * _TurbSpeed) * 0.6
                        + vnoise(sp * _TurbScale * 2.1 - t * _TurbSpeed * 1.3) * 0.4;

                // ---------- 06 Relevo cremoso (luz falsa) ----------
                float  h    = bands * _SwirlStrength + n * _TurbStrength;
                float2 grad = float2(ddx(h), ddy(h));
                float3 N = normalize(normalize(i.wN) + float3(-grad.x, 0, -grad.y) * _NormalStrength);
                float3 V = normalize(i.wV);
                float3 L = normalize(_LightDir.xyz);
                float3 H = normalize(L + V);

                // ---------- 01 Cor: mistura só entre Dark e Light ----------
                float tone = bands * _SwirlStrength + n * _TurbStrength + (1.0 - r) * 0.1;
                tone += (dot(N, L) - 0.7) * 0.35;                       // relevo muda o tom, não a cor
                tone = saturate(tone * 1.25);
                tone = lerp(tone, smoothstep(0.1, 0.9, tone), _Density); // cremosidade = áreas mais lisas
                float3 col = lerp(_ColorDark.rgb, _ColorLight.rgb, tone);

                col += _SpecColor2.rgb * pow(saturate(dot(N, H)), _SpecPower) * _SpecStrength;
                col += _RimColor.rgb * pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;

                // ---------- 04 Bolhas ----------
                float bAng = t * _SwirlSpeed * 0.5;
                float bs, bc;
                sincos(bAng, bs, bc);
                float2 bp = float2(bc * p.x - bs * p.y, bs * p.x + bc * p.y);
                float  bt = t * _BubbleSpeed;

                col = bubbleLayer(col, bp, _BubbleScale,       _BubbleAmount,        bt, 0.0, bs, bc); // grandes
                col = bubbleLayer(col, bp, _BubbleScale * 2.6, _BubbleAmount * 0.8,  bt, 1.0, bs, bc); // pequenas

                // ---------- 06 Borda ----------
                float edge = smoothstep(1.0 - _EdgeWidth, 1.0, r);
                col = lerp(col, _EdgeColor.rgb, saturate(edge));

                col *= _Brightness;

                // ---------- 05 Brilhos (por cima de tudo, HDR p/ Bloom) ----------
                float3 glow = starLayer(bp, _StarScale, t, 0.0)
                            + starLayer(bp, _StarScale * 1.8, t, 1.0) * 0.6
                            + dustLayer(bp, _DustScale, t, 0.0)
                            + dustLayer(bp, _DustScale * 1.6, t, 1.0) * 0.7;
                col += glow * _GlowIntensity * (1.0 - edge * 0.7);

                return float4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
