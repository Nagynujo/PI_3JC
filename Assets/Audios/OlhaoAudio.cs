using FMOD.Studio;
using FMODUnity;
using UnityEngine;

// [LUCCA - SOM] ARQUIVO NOVO: sons do evento Olhao. Mesmo padrao do OrderAudio / StationAudio / RatAudio.

/// <summary>
/// Sons do evento Olhao. Coloque no MESMO objeto do OlhaoEvent e escolha os eventos no Inspector.
/// Nao depende de mais nenhum script alem do proprio OlhaoEvent (de onde pega a posicao do
/// olho e da Bruna sozinho). Campos sem evento escolhido sao ignorados.
///
///   Aparecer()  -> o olho aparece (toca o som de aparecer e liga o loop do scanner)
///   Detectar()  -> a Bruna entrou no cone e foi paralisada
///   Liberar()   -> a paralisia terminou sozinha (opcional)
///   Sumir()     -> o evento foi resolvido com a pocao (desliga o loop e toca o som de sumir)
///   Parar()     -> o evento foi encerrado a forca (desliga o loop, sem som)
/// </summary>
public class OlhaoAudio : MonoBehaviour
{
    [Header("Eventos do FMOD")]
    [SerializeField] EventReference somAparecer;   // ex.: event:/olhao_aparecer
    [SerializeField] EventReference loopScanner;   // ex.: event:/olhao_loop (evento com loop, toca enquanto o olho esta ativo)
    [SerializeField] EventReference somDetectar;   // ex.: event:/olhao_detectou (toca na Bruna quando ela e pega)
    [SerializeField] EventReference somLiberar;    // opcional: quando a paralisia acaba
    [SerializeField] EventReference somSumir;      // ex.: event:/olhao_sumir

    OlhaoEvent evento;
    EventInstance loopInstance;
    bool loopLigado;

    void Awake()
    {
        evento = GetComponent<OlhaoEvent>();

        if (evento == null)
            Debug.LogWarning("OlhaoAudio: nenhum OlhaoEvent no mesmo objeto. Os sons vao tocar na posicao deste objeto.", this);
    }

    // Posicao do olho (se o OlhaoEvent tiver 'eye'); senao, a deste objeto.
    Vector3 PosicaoDoOlho() =>
        evento != null && evento.eye != null ? evento.eye.position : transform.position;

    // Posicao da Bruna (se o OlhaoEvent achou o player); senao, a do olho.
    Vector3 PosicaoDaBruna() =>
        evento != null && evento.player != null ? evento.player.position : PosicaoDoOlho();

    /// <summary>Chame quando o olho aparece. Toca o som de aparecer e liga o loop do scanner.</summary>
    public void Aparecer()
    {
        TocarUmaVez(somAparecer, PosicaoDoOlho());
        LigarLoop();
    }

    /// <summary>Chame quando a Bruna entra no cone e e paralisada. Toca na posicao dela.</summary>
    public void Detectar()
    {
        TocarUmaVez(somDetectar, PosicaoDaBruna());
    }

    /// <summary>Chame quando a paralisia termina sozinha (opcional).</summary>
    public void Liberar()
    {
        TocarUmaVez(somLiberar, PosicaoDaBruna());
    }

    /// <summary>Chame quando o evento e resolvido com a pocao: desliga o loop e toca o som de sumir.</summary>
    public void Sumir()
    {
        DesligarLoop(true);
        TocarUmaVez(somSumir, PosicaoDoOlho());
    }

    /// <summary>Chame quando o evento e encerrado a forca: desliga o loop, sem tocar som.</summary>
    public void Parar()
    {
        DesligarLoop(true);
    }

    void TocarUmaVez(EventReference som, Vector3 posicao)
    {
        if (som.IsNull) return;
        RuntimeManager.PlayOneShot(som, posicao);
    }

    void LigarLoop()
    {
        if (loopLigado || loopScanner.IsNull) return;

        loopInstance = RuntimeManager.CreateInstance(loopScanner);
        loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(PosicaoDoOlho()));
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
