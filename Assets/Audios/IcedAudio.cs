using FMOD.Studio;
using FMODUnity;
using UnityEngine;

// [LUCCA - SOM] ARQUIVO NOVO: sons do evento Iced (gelo). Mesmo padrao do OlhaoAudio / RatAudio / StationAudio.

/// <summary>
/// Sons do evento Iced. Coloque no MESMO objeto do IcedEvent e escolha os eventos no Inspector.
/// Nao depende de mais nenhum script alem do proprio IcedEvent (de onde pega a posicao do
/// braseiro sozinho). Campos sem evento escolhido sao ignorados.
///
/// O gelo e um efeito do AMBIENTE inteiro, entao os eventos do FMOD devem ser 2D
/// (sem Spatializer). So o som de derreter sai na posicao do braseiro, se existir.
///
///   Congelar()    -> o evento comeca (toca o som de congelar e liga o loop frio)
///   SetGelo(0..1) -> opcional: intensidade do gelo. Sobe de 0 a 1 enquanto congela e volta
///                    a 0 enquanto derrete. Precisa de um parametro no evento do loop (veja abaixo)
///   Derreter()    -> a pocao de Fogo foi aplicada (toca o som de derreter)
///   Parar()       -> desliga o loop frio (fim do derretimento ou fim forcado, sem som)
/// </summary>
public class IcedAudio : MonoBehaviour
{
    [Header("Eventos do FMOD")]
    [SerializeField] EventReference somCongelar;   // ex.: event:/gelo_congelar (dura mais ou menos o transitionDuration)
    [SerializeField] EventReference loopFrio;      // ex.: event:/gelo_loop (evento com loop, vento/frio enquanto o evento esta ativo)
    [SerializeField] EventReference somDerreter;   // ex.: event:/gelo_derreter (toca no braseiro, se houver)

    [Header("Parametro do FMOD (opcional)")]
    [Tooltip("Nome do parametro do evento do loop que acompanha o gelo (0 = normal, 1 = congelado). Deixe VAZIO se o evento nao tiver esse parametro.")]
    [SerializeField] string parametroGelo = "Gelo";

    IcedEvent evento;
    EventInstance loopInstance;
    bool loopLigado;

    void Awake()
    {
        evento = GetComponent<IcedEvent>();

        if (evento == null)
            Debug.LogWarning("IcedAudio: nenhum IcedEvent no mesmo objeto. O som de derreter vai tocar sem posicao (2D).", this);
    }

    /// <summary>Chame quando o evento comeca. Toca o som de congelar e liga o loop frio.</summary>
    public void Congelar()
    {
        if (!somCongelar.IsNull)
            RuntimeManager.PlayOneShot(somCongelar);

        LigarLoop();
    }

    /// <summary>Opcional: intensidade do gelo de 0 a 1 (o IcedEvent chama todo frame durante a transicao).</summary>
    public void SetGelo(float normalizado)
    {
        if (!loopLigado || string.IsNullOrEmpty(parametroGelo)) return;
        loopInstance.setParameterByName(parametroGelo, Mathf.Clamp01(normalizado));
    }

    /// <summary>Chame quando a pocao de Fogo e aplicada. Toca no braseiro (se houver); senao, 2D.</summary>
    public void Derreter()
    {
        if (somDerreter.IsNull) return;

        if (evento != null && evento.fireTarget != null)
            RuntimeManager.PlayOneShot(somDerreter, evento.fireTarget.transform.position);
        else
            RuntimeManager.PlayOneShot(somDerreter);
    }

    /// <summary>Chame quando o gelo acaba de derreter ou quando o evento e encerrado a forca: desliga o loop frio.</summary>
    public void Parar()
    {
        DesligarLoop(true);
    }

    void LigarLoop()
    {
        if (loopLigado || loopFrio.IsNull) return;

        loopInstance = RuntimeManager.CreateInstance(loopFrio);
        loopInstance.start();
        loopLigado = true;
    }

    void DesligarLoop(bool comFade)
    {
        if (!loopLigado) return;

        loopInstance.stop(comFade ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        loopInstance.release();
        loopInstance.clearHandle();
        loopLigado = false;
    }

    void OnDisable() => DesligarLoop(false);
    void OnDestroy() => DesligarLoop(false);
}
