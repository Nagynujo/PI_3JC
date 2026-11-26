using System.Collections;
using UnityEngine;

// =====================================================================================
// [LUCCA - SOM] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - SOM]")
//   1. Novo campo privado 'icedAudio' (referência ao componente IcedAudio, ARQUIVO NOVO).
//   2. Awake(): pega o IcedAudio que está no MESMO objeto deste IcedEvent.
//   3. OnActivated(): toca IcedAudio.Congelar() quando o evento começa (som de congelar +
//      liga o loop frio).
//   4. Apply(): manda a intensidade do gelo ('t', de 0 a 1) pro FMOD via IcedAudio.SetGelo().
//      É opcional: só faz efeito se o evento do loop tiver o parâmetro "Gelo".
//   5. OnResolving(): toca IcedAudio.Derreter() quando a poção de Fogo é aplicada e, DEPOIS
//      que o gelo termina de derreter, chama IcedAudio.Parar() (desliga o loop frio).
//   6. OnForceEnded(): chama IcedAudio.Parar() (desliga o loop, sem tocar som).
//
// COMO FUNCIONA:
//   O IcedAudio fica no MESMO objeto do IcedEvent. Ele lê daqui o 'fireTarget' pra saber
//   onde tocar o som de derreter. Se não houver IcedAudio no objeto, o evento funciona
//   normalmente, só sem som (nenhum erro).
//
// ONDE CONFIGURAR:
//   No objeto do IcedEvent: Add Component > IcedAudio e escolher os eventos do FMOD
//   no Inspector. Nada mais precisa mudar.
//
// Todo o resto do arquivo está igual à versão anterior.
// =====================================================================================

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
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.9f;
    [Tooltip("Escala inicial do overlay. Maior que 1 = o gelo 'avança' dos cantos para dentro enquanto aparece.")]
    public float overlayStartScale = 1.3f;
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
    [Tooltip("Segundos para o gelo aparecer por completo (gradual) e para derreter.")]
    public float transitionDuration = 5f;

    // valores originais capturados na ativação
    private Color origAmbient, origFogColor;
    private bool origFog;
    private FogMode origFogMode;
    private float origFogDensity;
    private float t; // 0 = normal, 1 = congelado
    private Coroutine transition;

    // [LUCCA - SOM] NOVO: sons do Iced (FMOD). Opcional: fica null se não houver IcedAudio no objeto.
    private IcedAudio icedAudio;

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

        // [LUCCA - SOM] NOVO: som de congelar + liga o loop frio. Fica junto do início da
        // transição (acima), pra o som acompanhar o gelo aparecendo aos poucos.
        if (icedAudio != null) icedAudio.Congelar();
    }

    protected override IEnumerator OnResolving()
    {
        if (fireTarget != null) fireTarget.SetActive(false);
        // Velocidade e duração voltam IMEDIATAMENTE ao aplicar Fire; o visual derrete suavemente.
        EventModifiers.RemoveAll(this);

        // [LUCCA - SOM] NOVO: som de derreter, no mesmo instante em que a lentidão some.
        // O loop frio continua durante o derretimento e é desligado mais abaixo.
        if (icedAudio != null) icedAudio.Derreter();

        if (transition != null) StopCoroutine(transition);
        yield return StartCoroutine(Transition(0f));

        // [LUCCA - SOM] NOVO: o gelo terminou de derreter, então desliga o loop frio.
        if (icedAudio != null) icedAudio.Parar();

        RestoreEnvironment();
        SetVisuals(false);
    }

    // [LUCCA - SOM] ALTERADO: ganhou uma linha no final pra desligar o loop frio, sem tocar
    // nenhum som (fim forçado/reinício).
    protected override void OnForceEnded()
    {
        EventModifiers.RemoveAll(this);
        RestoreEnvironment();
        SetVisuals(false);

        if (icedAudio != null) icedAudio.Parar();
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

    // [LUCCA - SOM] ALTERADO: antes era uma linha só ({ if (fireTarget != null) fireTarget.SetActive(false); }).
    // Agora também pega o IcedAudio do mesmo objeto (pode não existir).
    private void Awake()
    {
        if (fireTarget != null) fireTarget.SetActive(false);

        icedAudio = GetComponent<IcedAudio>();
    }

    private void Apply()
    {
        if (tintAmbient) RenderSettings.ambientLight = Color.Lerp(origAmbient, coldAmbientColor, t);
        if (tintFog)
        {
            RenderSettings.fogColor = Color.Lerp(origFogColor, coldFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(origFogDensity, coldFogDensity, t);
        }
        if (coldOverlay != null)
        {
            float k = Mathf.SmoothStep(0f, 1f, t); // começa devagar, termina suave
            coldOverlay.alpha = overlayMaxAlpha * k;
            coldOverlay.transform.localScale = Vector3.one * Mathf.Lerp(overlayStartScale, 1f, k);
        }

        // [LUCCA - SOM] NOVO: manda a intensidade do gelo (0 a 1) pro parâmetro do FMOD, pra o
        // loop frio crescer junto com o gelo e diminuir junto com o derretimento.
        // Sem o parâmetro no evento (ou com o campo vazio no IcedAudio), não faz nada.
        if (icedAudio != null) icedAudio.SetGelo(t);
    }

    private void RestoreEnvironment()
    {
        t = 0f;
        RenderSettings.ambientLight = origAmbient;
        RenderSettings.fog = origFog; RenderSettings.fogColor = origFogColor;
        RenderSettings.fogMode = origFogMode; RenderSettings.fogDensity = origFogDensity;
        if (coldOverlay != null) { coldOverlay.alpha = 0f; coldOverlay.transform.localScale = Vector3.one; }
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