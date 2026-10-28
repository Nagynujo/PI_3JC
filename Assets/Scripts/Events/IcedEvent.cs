using System.Collections;
using UnityEngine;

/// <summary>
/// Evento Iced. Lentidão via EventModifiers (sem Time.timeScale). Resolvido apenas com Fire.
/// Os valores originais de movimento/ações nunca são sobrescritos: removemos só o NOSSO multiplicador.
/// </summary>
public class IcedEvent : GameEventBase
{
    [Header("Lentidão (multiplicadores)")]
    [Range(0.1f, 1f)] public float moveSpeedMultiplier = 0.55f;
    [Tooltip("Menor = ações mais demoradas. 0.5 => levam o dobro do tempo.")]
    [Range(0.1f, 1f)] public float actionSpeedMultiplier = 0.5f;

    [Header("Resolução (alvo da poção Fire)")]
    [Tooltip("Objeto com Collider (Is Trigger) + EventPotionTarget (Target Event = este IcedEvent), ex.: uma lareira/braseiro. Ativado só durante o evento.")]
    public GameObject fireTarget;

    [Header("Feedback visual")]
    [Tooltip("Pai com partículas de neve/névoa/cristais. Ligado durante o evento.")]
    public GameObject coldVisualRoot;
    [Tooltip("Imagem azulada em tela cheia (UI). Opcional.")]
    public CanvasGroup coldOverlay;
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.35f;
    [Tooltip("Efeito de gelo nas estações de trabalho. Opcional.")]
    public GameObject[] stationFrostEffects;
    [Tooltip("Indicador de lentidão na Bruna (aura azul, vapor, ícone). Opcional.")]
    public GameObject playerColdIndicator;

    [Header("Tonalidade do ambiente (RenderSettings)")]
    public bool tintAmbient = true;
    public Color coldAmbientColor = new Color(0.45f, 0.62f, 0.9f);
    public bool tintFog = true;
    public Color coldFogColor = new Color(0.7f, 0.85f, 1f);
    public float coldFogDensity = 0.03f;
    public float transitionDuration = 1.2f;

    // valores originais capturados na ativação
    private Color origAmbient, origFogColor;
    private bool origFog;
    private FogMode origFogMode;
    private float origFogDensity;
    private float t; // 0 = normal, 1 = congelado
    private Coroutine transition;

    protected override void OnActivated()
    {
        origAmbient = RenderSettings.ambientLight;
        origFog = RenderSettings.fog; origFogColor = RenderSettings.fogColor;
        origFogMode = RenderSettings.fogMode; origFogDensity = RenderSettings.fogDensity;

        EventModifiers.SetMove(this, moveSpeedMultiplier);
        EventModifiers.SetAction(this, actionSpeedMultiplier);

        SetVisuals(true);
        if (tintFog) { RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; }
        transition = StartCoroutine(Transition(1f));
    }

    protected override IEnumerator OnResolving()
    {
        if (fireTarget != null) fireTarget.SetActive(false);
        // Velocidade e duração voltam IMEDIATAMENTE ao aplicar Fire; o visual derrete suavemente.
        EventModifiers.RemoveAll(this);
        if (transition != null) StopCoroutine(transition);
        yield return StartCoroutine(Transition(0f));
        RestoreEnvironment();
        SetVisuals(false);
    }

    protected override void OnForceEnded()
    {
        EventModifiers.RemoveAll(this);
        RestoreEnvironment();
        SetVisuals(false);
    }

    private IEnumerator Transition(float to)
    {
        float from = t;
        float dur = Mathf.Max(0.01f, transitionDuration);
        for (float e = 0f; e < dur; e += Time.deltaTime)
        {
            t = Mathf.Lerp(from, to, e / dur);
            Apply();
            yield return null;
        }
        t = to; Apply();
    }

    private void Awake() { if (fireTarget != null) fireTarget.SetActive(false); }

    private void Apply()
    {
        if (tintAmbient) RenderSettings.ambientLight = Color.Lerp(origAmbient, coldAmbientColor, t);
        if (tintFog)
        {
            RenderSettings.fogColor = Color.Lerp(origFogColor, coldFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(origFogDensity, coldFogDensity, t);
        }
        if (coldOverlay != null) coldOverlay.alpha = overlayMaxAlpha * t;
    }

    private void RestoreEnvironment()
    {
        t = 0f;
        RenderSettings.ambientLight = origAmbient;
        RenderSettings.fog = origFog; RenderSettings.fogColor = origFogColor;
        RenderSettings.fogMode = origFogMode; RenderSettings.fogDensity = origFogDensity;
        if (coldOverlay != null) coldOverlay.alpha = 0f;
    }

    private void SetVisuals(bool on)
    {
        if (coldVisualRoot != null) coldVisualRoot.SetActive(on);
        if (playerColdIndicator != null) playerColdIndicator.SetActive(on);
        if (stationFrostEffects != null)
            foreach (var g in stationFrostEffects) if (g != null) g.SetActive(on);
        if (fireTarget != null) fireTarget.SetActive(on);
    }
}
