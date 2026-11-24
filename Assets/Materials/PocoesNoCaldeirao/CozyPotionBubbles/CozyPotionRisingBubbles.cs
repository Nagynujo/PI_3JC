using UnityEngine;

/// <summary>
/// Arraste o prefab CozyPotion_RisingBubbles para a Hierarchy e posicione
/// este transform na ALTURA da superficie do liquido.
/// O Particle System e o material sao criados automaticamente ao dar Play.
/// Nenhuma textura e necessaria.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("CozyPotion/Bolhas magicas subindo")]
public sealed class CozyPotionRisingBubbles : MonoBehaviour
{
    [Header("Distribuicao sobre a superficie")]
    [Min(0.01f)] public float radius = 0.34f;
    [Range(0, 20)] public float bubblesPerSecond = 6.0f;
    [Range(8, 80)] public int maxParticles = 36;

    [Header("Subida das bolhas")]
    [Min(0.01f)] public float minLifetime = 1.3f;
    [Min(0.01f)] public float maxLifetime = 2.2f;
    [Min(0.01f)] public float riseSpeed = 0.55f;
    [Range(0f, 0.4f)] public float horizontalDrift = 0.08f;
    [Range(0.01f, 0.4f)] public float minBubbleSize = 0.065f;
    [Range(0.01f, 0.4f)] public float maxBubbleSize = 0.16f;

    [Header("Visual magico")]
    public Color bubbleColor = new Color(1f, 0.72f, 0.93f, 1f);
    [Range(0.5f, 4f)] public float glow = 1.55f;

    private ParticleSystem _particles;
    private Material _material;
    private float _emitAccumulator;

    private void Start()
    {
        if (!Application.isPlaying) return;
        CreateRuntimeParticles();
    }

    private void CreateRuntimeParticles()
    {
        // Prefab leve: a propria instancia cria um Particle System ao iniciar a cena.
        GameObject particleObject = new GameObject("Bolhas_Visual_Runtime");
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = Vector3.zero;
        particleObject.transform.localRotation = Quaternion.identity;
        particleObject.transform.localScale = Vector3.one;

        _particles = particleObject.AddComponent<ParticleSystem>();
        var main = _particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 5f;
        main.startSpeed = 0f;
        main.startLifetime = 2f;
        main.startSize = 0.1f;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.Max(8, maxParticles);

        var emission = _particles.emission;
        emission.enabled = false; // Emissao manual = distribuicao circular garantida.

        var shape = _particles.shape;
        shape.enabled = false;

        var sizeOverLife = _particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.22f, 1f),
            new Keyframe(0.75f, 1.05f),
            new Keyframe(1f, 0.25f)
        ));

        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.95f, 0.77f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        var colorOverLife = _particles.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = new ParticleSystem.MinMaxGradient(fade);

        var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;

        Shader shader = Resources.Load<Shader>("CozyPotionBubble");
        if (shader == null)
        {
            Debug.LogError("CozyPotion: falta o shader Resources/CozyPotionBubble.shader. Reimporte o pacote completo.", this);
            return;
        }

        _material = new Material(shader);
        _material.name = "CozyPotion Bubble (runtime)";
        _material.SetColor("_Tint", bubbleColor);
        _material.SetFloat("_Brightness", glow);
        renderer.sharedMaterial = _material;

        _particles.Play();
    }

    private void Update()
    {
        if (_particles == null || !_particles.isPlaying || bubblesPerSecond <= 0f) return;

        _emitAccumulator += Time.deltaTime * bubblesPerSecond;
        int count = Mathf.Min(Mathf.FloorToInt(_emitAccumulator), 6);
        if (count <= 0) return;
        _emitAccumulator -= count;

        for (int i = 0; i < count; i++)
        {
            // sqrt(U) distribui bolhas uniformemente pelo disco sem concentrar no centro.
            float angle = Random.value * Mathf.PI * 2f;
            float distance = Mathf.Sqrt(Random.value) * radius;
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;

            ParticleSystem.EmitParams bubble = new ParticleSystem.EmitParams();
            bubble.position = new Vector3(x, 0f, z);
            bubble.velocity = new Vector3(
                Random.Range(-horizontalDrift, horizontalDrift),
                riseSpeed * Random.Range(0.85f, 1.15f),
                Random.Range(-horizontalDrift, horizontalDrift)
            );
            bubble.startLifetime = Random.Range(
                Mathf.Max(0.05f, Mathf.Min(minLifetime, maxLifetime)),
                Mathf.Max(0.06f, Mathf.Max(minLifetime, maxLifetime))
            );
            bubble.startSize = Random.Range(
                Mathf.Min(minBubbleSize, maxBubbleSize),
                Mathf.Max(minBubbleSize, maxBubbleSize)
            );
            bubble.startColor = Color.white;
            _particles.Emit(bubble, 1);
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
            _material = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.52f, 0.88f, 0.9f);
        const int segments = 40;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 from = transform.TransformPoint(new Vector3(
                Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            Vector3 to = transform.TransformPoint(new Vector3(
                Mathf.Cos(b) * radius, 0f, Mathf.Sin(b) * radius));
            Gizmos.DrawLine(from, to);
        }
        Gizmos.DrawLine(transform.position, transform.position + transform.up * riseSpeed);
    }
}
