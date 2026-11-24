using UnityEngine;

/// <summary>
/// Enxame de vagalumes: Particle System (visual) + pool de Point Lights reais (iluminacao).
/// Uso: crie um GameObject vazio, adicione este componente e aperte Play.
/// Tudo e criado por codigo; nenhum prefab/textura necessario.
/// </summary>
[DisallowMultipleComponent]
public class FireflySwarm : MonoBehaviour
{
    [Header("Area (centro = este objeto)")]
    public Vector3 areaSize = new Vector3(20f, 4f, 20f);

    [Header("Vagalumes")]
    [Range(1, 300)] public int count = 60;
    public Color color = new Color(0.75f, 1f, 0.25f, 1f);
    public Vector2 sizeRange = new Vector2(0.15f, 0.3f);
    public Vector2 lifetimeRange = new Vector2(20f, 40f);

    [Header("Movimento")]
    public float driftSpeed = 0.3f;
    public float wanderStrength = 1.2f;
    public float wanderFrequency = 0.25f;
    [Tooltip("Impede os vagalumes de sairem da caixa: eles voltam ao encostar nas paredes.")]
    public bool keepInsideArea = true;

    [Header("Piscar")]
    public Vector2 blinkSpeedRange = new Vector2(0.3f, 0.9f); // piscadas por segundo
    [Range(0.5f, 6f)] public float blinkSharpness = 2.5f;     // maior = pisca mais curto
    [Range(0f, 0.5f)] public float minGlow = 0.05f;           // brilho minimo entre piscadas

    [Header("Luzes reais")]
    public bool useLights = true;
    [Range(0, 16)] public int lightCount = 8;                 // mantenha baixo (6-8)
    public float lightRange = 4f;
    public float lightIntensity = 2f;
    public float maxLightDistance = 40f;                      // desliga luzes longe da camera

    [Header("Material")]
    [Tooltip("Arraste FireflyGlow.shader aqui (preenchido automaticamente no Editor). Necessario para builds.")]
    public Shader glowShader;
    [Range(0f, 10f)] public float glowIntensity = 3f;

    ParticleSystem ps;
    ParticleSystem.Particle[] particles;
    Light[] lights;
    Material mat;

#if UNITY_EDITOR
    void Reset()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("FireflyGlow t:Shader");
        if (guids.Length > 0)
            glowShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
    }
#endif

    void Awake()
    {
        BuildParticleSystem();
        BuildLights();
    }

    void BuildParticleSystem()
    {
        var go = new GameObject("Fireflies_Particles");
        go.transform.SetParent(transform, false);
        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float avgLife = (lifetimeRange.x + lifetimeRange.y) * 0.5f;

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, driftSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = color;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = count / avgLife;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;
        shape.randomDirectionAmount = 1f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = wanderStrength;
        noise.frequency = wanderFrequency;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.maxParticleSize = 1f;

        if (glowShader == null) glowShader = Shader.Find("Custom/FireflyGlow");
        if (glowShader == null)
        {
            Debug.LogError("FireflySwarm: shader 'Custom/FireflyGlow' nao encontrado. Importe FireflyGlow.shader e arraste no campo Glow Shader.");
            return;
        }
        mat = new Material(glowShader) { name = "Firefly (runtime)" };
        mat.SetFloat("_Intensity", glowIntensity);
        r.material = mat;

        particles = new ParticleSystem.Particle[count];
        ps.Play();
    }

    void BuildLights()
    {
        if (!useLights || lightCount <= 0) return;
        lights = new Light[lightCount];
        for (int i = 0; i < lightCount; i++)
        {
            var go = new GameObject("FireflyLight_" + i);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.shadows = LightShadows.None;
            l.range = lightRange;
            l.color = color;
            l.intensity = 0f;
            l.enabled = false;
            lights[i] = l;
        }
    }

    void LateUpdate()
    {
        if (ps == null || particles == null) return;

        int n = ps.GetParticles(particles);
        float t = Time.time;

        Vector3 half = areaSize * 0.5f;
        Quaternion invRot = Quaternion.Inverse(transform.rotation);

        for (int i = 0; i < n; i++)
        {
            if (keepInsideArea)
            {
                Vector3 lp = invRot * (particles[i].position - transform.position);
                Vector3 lv = invRot * particles[i].velocity;
                bool changed = false;

                if (Mathf.Abs(lp.x) > half.x) { lp.x = Mathf.Clamp(lp.x, -half.x, half.x); lv.x = -Mathf.Sign(lp.x) * Mathf.Abs(lv.x); changed = true; }
                if (Mathf.Abs(lp.y) > half.y) { lp.y = Mathf.Clamp(lp.y, -half.y, half.y); lv.y = -Mathf.Sign(lp.y) * Mathf.Abs(lv.y); changed = true; }
                if (Mathf.Abs(lp.z) > half.z) { lp.z = Mathf.Clamp(lp.z, -half.z, half.z); lv.z = -Mathf.Sign(lp.z) * Mathf.Abs(lv.z); changed = true; }

                if (changed)
                {
                    particles[i].position = transform.position + transform.rotation * lp;
                    particles[i].velocity = transform.rotation * lv;
                }
            }

            uint seed = particles[i].randomSeed;
            float h1 = (seed & 0xFFFF) / 65535f;
            float h2 = ((seed >> 16) & 0xFFFF) / 65535f;

            float freq = Mathf.Lerp(blinkSpeedRange.x, blinkSpeedRange.y, h1);
            float s = Mathf.Sin((t * freq + h2 * 100f) * Mathf.PI * 2f) * 0.5f + 0.5f;
            float blink = Mathf.Lerp(minGlow, 1f, Mathf.Pow(s, blinkSharpness));

            float age = 1f - particles[i].remainingLifetime / Mathf.Max(0.001f, particles[i].startLifetime);
            float fade = Smooth(0f, 0.12f, age) * (1f - Smooth(0.88f, 1f, age));

            float v = blink * fade;
            var c = color;
            c.a = v;
            particles[i].startColor = c;
            particles[i].startSize = Mathf.Lerp(sizeRange.x, sizeRange.y, h2) * (0.7f + 0.3f * blink);
        }

        ps.SetParticles(particles, n);

        if (lights == null) return;

        Camera cam = Camera.main;
        float maxSqr = maxLightDistance * maxLightDistance;

        for (int i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (i >= n) { l.enabled = false; continue; }

            Vector3 pos = particles[i].position;
            float v = particles[i].startColor.a / 255f; // startColor e Color32
            // Color32 -> alpha 0..255; converte para 0..1 (ja calculado acima)

            bool far = cam != null && (cam.transform.position - pos).sqrMagnitude > maxSqr;
            float intensity = far ? 0f : v * lightIntensity;

            l.transform.position = pos;
            l.intensity = intensity;
            l.enabled = intensity > 0.02f;
        }
    }

    static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
