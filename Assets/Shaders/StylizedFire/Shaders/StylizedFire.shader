// Stylized Fire v2 - fogo cartoon em anel com linguas em gancho (curl), faixa de chamas na base e gotas de fogo.
// Shader unlit procedural (sem texturas). Use com a malha FireRing (UV: x = volta, y = altura 0..1).
Shader "Custom/StylizedFire"
{
    Properties
    {
        // Paleta: #68097D (roxo) / #C91B7A (magenta) / #E7675C (coral) / #FFCB06 (amarelo)
        [HDR] _ColorBase  ("Base (roxo #68097D)", Color)      = (0.408, 0.035, 0.490, 1)
        [HDR] _ColorOuter ("Externa (magenta #C91B7A)", Color) = (0.788, 0.106, 0.478, 1)
        [HDR] _ColorMid   ("Meio (coral #E7675C)", Color)      = (0.906, 0.404, 0.361, 1)
        [HDR] _ColorCore  ("Nucleo (amarelo #FFCB06)", Color)  = (1.0, 0.796, 0.024, 1)

        _Intensity   ("Intensidade (use com Bloom)", Range(0, 4)) = 1.0
        _Alpha       ("Opacidade", Range(0, 1)) = 1.0
        _BaseGlow    ("Brilho da base", Range(0, 3)) = 1.0

        _Tongues     ("Linguas grandes (inteiro)", Float) = 8
        _SmallAmount ("Tamanho das linguas pequenas (0 = desliga)", Range(0, 1)) = 0.55
        _Height      ("Altura das chamas", Range(0.3, 1.4)) = 1.0
        _HeightVar   ("Variacao de altura", Range(0, 0.8)) = 0.35
        _Width       ("Largura da lingua", Range(0.02, 0.2)) = 0.12
        _Taper       ("Afilamento da ponta", Range(0.3, 2)) = 0.6
        _CurlSize    ("Tamanho do gancho", Range(0.3, 2)) = 1.0
        _CurlAmount  ("Quanto o gancho enrola", Range(0.3, 1.6)) = 1.0
        _Sway        ("Balanco", Range(0, 0.2)) = 0.05
        _Wobble      ("Instabilidade", Range(0, 1)) = 0.5
        _Speed       ("Velocidade", Range(0, 4)) = 1.0
        _Softness    ("Suavidade da borda", Range(0.001, 0.05)) = 0.008

        _BandHeight  ("Altura da faixa da base", Range(0, 0.5)) = 0.18
        _SparkDensity("Densidade das gotas", Range(0, 1)) = 0.4
        _SparkSize   ("Tamanho das gotas", Range(0.005, 0.08)) = 0.03

        [HideInInspector] _CellAspect ("Largura/altura da celula (automatico)", Float) = 0.55
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define PI 3.14159265
            #define TAU 6.2831853

            float4 _ColorBase, _ColorOuter, _ColorMid, _ColorCore;
            float _Intensity, _Alpha, _BaseGlow;
            float _Tongues, _SmallAmount, _Height, _HeightVar, _Width, _Taper;
            float _CurlSize, _CurlAmount, _Sway, _Wobble, _Speed, _Softness;
            float _BandHeight, _SparkDensity, _SparkSize, _CellAspect;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash11(float p) { return frac(sin(p * 127.1 + 311.7) * 43758.5453); }
            float hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // ruido periodico em x (periodo inteiro) para nao ter emenda na volta do anel
            float pnoise(float2 p, float period)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float x0 = fmod(i.x + period * 4.0, period);
                float x1 = fmod(i.x + 1.0 + period * 4.0, period);
                float a = hash21(float2(x0, i.y));
                float b = hash21(float2(x1, i.y));
                float c = hash21(float2(x0, i.y + 1.0));
                float d = hash21(float2(x1, i.y + 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Cov(float d, float w, float aa)
            {
                float k = saturate((w - d) / aa);
                return k * k * (3.0 - 2.0 * k);
            }

            // Uma lingua de fogo: haste vertical + arco (gancho) que enrola para o lado.
            // p = posicao relativa a base da lingua, em unidades da altura do anel.
            // retorna (externa, meio, nucleo)
            float3 Tongue(float2 p, float id, float t, float S)
            {
                float r1 = hash11(id * 1.31 + 1.7);
                float r2 = hash11(id * 2.17 + 7.3);
                float r3 = hash11(id * 3.73 + 13.1);
                float r4 = hash11(id * 5.11 + 21.9);

                float dir   = (r3 > 0.5) ? 1.0 : -1.0;
                float pulse = 0.94 + 0.06 * sin(t * 2.6 + r2 * TAU);

                float L1  = _Height * 0.55 * (1.0 - _HeightVar * r1) * pulse * S;
                float R   = _Height * (0.11 + 0.07 * r2) * _CurlSize * S;
                float Phi = max((0.75 + 0.6 * r4) * PI * _CurlAmount + 0.25 * sin(t * 1.7 + r2 * TAU), 0.3);
                float total = L1 + R * Phi;

                // balanco + instabilidade crescendo com a altura
                float yn = saturate(p.y / max(L1 + R, 0.001));
                p.x += (r1 - 0.5) * 0.18 * yn * yn * S;                       // inclinacao propria de cada lingua
                p.x += _Sway * sin(t * 1.8 + r4 * TAU) * yn;
                p.x += (vnoise(float2(id * 3.1 + p.y * 5.0, t * 1.2)) - 0.5) * _Wobble * 0.06 * yn;

                float2 q = float2(p.x * dir, p.y);

                // haste
                float ys = clamp(q.y, 0.0, L1);
                float dS = length(q - float2(0.0, ys));
                float sS = ys / total;

                // arco (centro em (R, L1), comeca em theta = PI e gira no sentido horario)
                float2 C = float2(R, L1);
                float2 w = q - C;
                float rr = length(w);
                float alpha = PI - atan2(w.y, w.x);
                float dA, sA;
                if (alpha <= Phi)
                {
                    dA = abs(rr - R);
                    sA = (L1 + R * alpha) / total;
                }
                else
                {
                    float2 E = C + R * float2(cos(PI - Phi), sin(PI - Phi));
                    dA = length(q - E);
                    sA = 1.0;
                }

                float W0 = _Width * S;
                float wS = W0 * pow(saturate(1.0 - sS), _Taper);
                float wA = W0 * pow(saturate(1.0 - sA), _Taper);
                float aa = max(_Softness, 0.0005);

                float3 m;
                m.x = max(Cov(dS, wS, aa),        Cov(dA, wA, aa));
                m.y = max(Cov(dS, wS * 0.60, aa), Cov(dA, wA * 0.60, aa));
                m.z = max(Cov(dS, wS * 0.20, aa), Cov(dA, wA * 0.20, aa));
                return m;
            }

            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float N = max(1.0, round(_Tongues));
                float u = i.uv.x;
                float v = i.uv.y;
                float A = _CellAspect;

                // ---- linguas grandes ----
                float3 m = float3(0, 0, 0);
                float n1  = u * N;
                float id1 = floor(n1);
                [unroll] for (int o = -1; o <= 1; o++)
                {
                    float idw = fmod(id1 + o + N * 2.0, N);
                    float px  = (n1 - (id1 + o + 0.5)) * A;
                    m = max(m, Tongue(float2(px, v), idw, t, 1.0));
                }

                // ---- linguas pequenas (entre as grandes) ----
                if (_SmallAmount > 0.01)
                {
                    float n2  = u * N - 0.5;
                    float id2 = floor(n2);
                    [unroll] for (int k = -1; k <= 1; k++)
                    {
                        float idw = fmod(id2 + k + N * 2.0, N) + 100.0;
                        float px  = (n2 - (id2 + k + 0.5)) * A;
                        m = max(m, Tongue(float2(px, v), idw, t + 1.7, _SmallAmount));
                    }
                }

                // ---- faixa continua de chamas na base ----
                float nb1 = pnoise(float2(u * N * 2.0, t * 0.9), N * 2.0);
                float nb2 = pnoise(float2(u * N * 5.0, t * 1.6 + 3.0), N * 5.0);
                float top = _BandHeight * (0.55 + 0.9 * nb1 + 0.4 * nb2);
                float bandOuter = saturate((top - v) / 0.015);
                float bandMid   = saturate((top * 0.42 - v) / 0.015);
                float bandCore  = saturate((top * 0.16 - v) / 0.015);

                float st = sin(TAU * u * N + v * 10.0 - t * 2.0 + 2.0 * sin(TAU * u * N * 2.0 + t));
                float streak = smoothstep(0.55, 0.62, st) * bandOuter * (1.0 - bandCore);

                // ---- gotas de fogo flutuando ----
                float K    = N * 2.0;
                float rows = 3.0;
                float gv   = v * rows - t * 0.25;
                float2 gi  = float2(floor(u * K), floor(gv));
                float2 gf  = float2(frac(u * K), frac(gv));
                float present = step(1.0 - _SparkDensity, hash21(gi + 0.5));
                float2 c  = float2(0.25 + 0.5 * hash21(gi + 3.1), 0.25 + 0.5 * hash21(gi + 5.9));
                float2 dd = (gf - c) * float2(A * N / K, 1.0 / rows);
                dd.y *= 0.55;
                float sz  = _SparkSize * (0.6 + 0.8 * hash21(gi + 9.3));
                float dl  = length(dd);
                float vis = smoothstep(_BandHeight * 1.2, _BandHeight * 2.2, v) * (1.0 - smoothstep(0.85, 1.0, v)) * present;
                float dropOuter = (1.0 - smoothstep(sz * 0.8, sz, dl)) * vis;
                float dropMid   = (1.0 - smoothstep(sz * 0.35, sz * 0.5, dl)) * vis;

                // ---- composicao em camadas (cartoon) ----
                float outerM = max(max(m.x, bandOuter), dropOuter);
                float midM   = max(max(m.y, bandMid), dropMid);
                float coreM  = max(m.z, bandCore);

                float3 col = lerp(_ColorBase.rgb, _ColorOuter.rgb, smoothstep(0.0, 0.28, v));
                col = lerp(col, _ColorBase.rgb, streak * 0.85);
                col = lerp(col, _ColorMid.rgb,  midM  * (1.0 - streak * 0.5));
                col = lerp(col, _ColorCore.rgb, coreM);

                // brilho quente no centro da base
                col += _ColorCore.rgb * pow(saturate(1.0 - v * 6.0), 2.0) * _BaseGlow * 0.45 * outerM;
                col *= _Intensity;

                float a = outerM * _Alpha;
                clip(a - 0.01);
                return float4(col, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
