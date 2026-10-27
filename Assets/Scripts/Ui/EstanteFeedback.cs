using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feedback visual/sonoro da Estante: ícones das poções guardadas, contador (ex: "2/3"),
/// mensagens ("Estante cheia!") e uma tremidinha quando tenta guardar com tudo cheio.
///
/// Segue o mesmo padrão do WorldToScreenProgressBar: o visual vive dentro do seu Canvas
/// normal (Screen Space - Overlay) e se posiciona sozinho acompanhando a estante 3D.
/// Este script fica num objeto do MUNDO (a própria Estante ou um filho dela), NUNCA dentro
/// do Canvas — assim ele continua rodando mesmo quando os ícones são desativados.
///
/// Tudo usa tempo "real" (unscaled), então as mensagens não congelam se o jogo pausar.
/// </summary>
public class EstanteFeedback : MonoBehaviour
{
    [System.Serializable]
    public class PotionIconEntry
    {
        public PotionType tipo;
        public Sprite icone;
    }

    [Header("Referência")]
    [Tooltip("A Estante. Se deixar vazio, procura no mesmo objeto.")]
    public Estante estante;

    [Header("UI na tela (Canvas Screen Space - Overlay)")]
    [Tooltip("Objeto do Canvas que acompanha a estante na tela (contém os ícones e a mensagem). Ele nunca é desativado por este script.")]
    public RectTransform raizUI;
    [Tooltip("Deslocamento acima da estante onde a UI deve aparecer")]
    public Vector3 offset = new Vector3(0f, 2f, 0f);
    [Tooltip("Deixe vazio pra usar a Camera.main")]
    public Camera cameraManual;

    [Header("Ícones das poções guardadas")]
    [Tooltip("Objeto que agrupa os ícones — só aparece quando tem pelo menos 1 poção na estante")]
    public GameObject grupoIcones;
    [Tooltip("Uma Image por vaga, na ordem (a 1ª é a poção mais antiga). Precisa de uma por vaga da estante.")]
    public Image[] iconesDosSlots;
    [Tooltip("Ícone de cada tipo de poção (pode usar os mesmos sprites do OrderUIManager)")]
    public List<PotionIconEntry> icones;
    [Tooltip("Opcional - texto tipo '2/3'")]
    public TextMeshProUGUI textoContador;

    [Header("Mensagens")]
    [Tooltip("Texto de aviso (começa desativado). Pode ficar dentro da Raiz UI.")]
    public TextMeshProUGUI textoMensagem;
    public float duracaoMensagem = 1.2f;
    public string mensagemCheia = "Estante cheia!";
    public string mensagemVazia = "Estante vazia!";
    public string mensagemInvalida = "Só cabem poções engarrafadas!";

    [Header("Tremida quando está cheia (opcional)")]
    [Tooltip("Normalmente o modelo 3D da estante (filho). Não use o objeto que tem o Collider/trigger.")]
    public Transform alvoDaTremida;
    public float duracaoDaTremida = 0.25f;
    public float forcaDaTremida = 0.05f;

    [Header("Som (opcional)")]
    public AudioSource audioSource;
    public AudioClip somGuardar;
    public AudioClip somPegar;
    public AudioClip somCheia;
    public AudioClip somVazia;
    public AudioClip somInvalido;

    private Camera cameraPrincipal;
    private Coroutine rotinaMensagem;
    private Coroutine rotinaTremida;
    private Vector3 origemTremida;
    private int ultimaQuantidade;

    void Awake()
    {
        if (estante == null) estante = GetComponent<Estante>();
        cameraPrincipal = cameraManual != null ? cameraManual : Camera.main;

        if (alvoDaTremida != null) origemTremida = alvoDaTremida.localPosition;
        if (textoMensagem != null) textoMensagem.gameObject.SetActive(false);

        if (estante == null)
            Debug.LogWarning($"{gameObject.name}: 'Estante' não foi preenchida e não achei uma no mesmo objeto.");

        if (estante != null && iconesDosSlots != null && iconesDosSlots.Length < estante.capacidadeMaxima)
            Debug.LogWarning($"{gameObject.name}: só tem {iconesDosSlots.Length} ícone(s) em 'Icones Dos Slots', mas a estante comporta {estante.capacidadeMaxima}.");
    }

    void OnEnable()
    {
        if (estante == null) return;

        estante.OnMudou += HandleMudou;
        estante.OnTentouGuardarCheia += HandleCheia;
        estante.OnTentouPegarVazia += HandleVazia;
        estante.OnItemInvalido += HandleInvalido;

        ultimaQuantidade = estante.Quantidade;
        AtualizarIcones();
    }

    void OnDisable()
    {
        if (estante != null)
        {
            estante.OnMudou -= HandleMudou;
            estante.OnTentouGuardarCheia -= HandleCheia;
            estante.OnTentouPegarVazia -= HandleVazia;
            estante.OnItemInvalido -= HandleInvalido;
        }

        // Se for desativado no meio da tremida, devolve a estante ao lugar certo
        if (alvoDaTremida != null) alvoDaTremida.localPosition = origemTremida;
    }

    void LateUpdate()
    {
        if (raizUI == null || estante == null) return;

        if (cameraPrincipal == null)
            cameraPrincipal = cameraManual != null ? cameraManual : Camera.main;
        if (cameraPrincipal == null) return;

        // Canvas "Screen Space - Overlay": a posição na tela já basta
        // (mesma lógica do WorldToScreenProgressBar)
        raizUI.position = cameraPrincipal.WorldToScreenPoint(estante.transform.position + offset);
    }

    // ---------- Reações aos eventos da Estante ----------

    private void HandleMudou()
    {
        int quantidade = estante.Quantidade;

        if (quantidade > ultimaQuantidade) Tocar(somGuardar);
        else if (quantidade < ultimaQuantidade) Tocar(somPegar);

        ultimaQuantidade = quantidade;
        AtualizarIcones();
    }

    private void HandleCheia()
    {
        MostrarMensagem(mensagemCheia);
        Tremer();
        Tocar(somCheia);
    }

    private void HandleVazia()
    {
        MostrarMensagem(mensagemVazia);
        Tocar(somVazia);
    }

    private void HandleInvalido()
    {
        MostrarMensagem(mensagemInvalida);
        Tocar(somInvalido);
    }

    // ---------- Visual ----------

    private void AtualizarIcones()
    {
        int quantidade = estante.Quantidade;

        if (grupoIcones != null) grupoIcones.SetActive(quantidade > 0);

        if (textoContador != null)
            textoContador.text = $"{quantidade}/{estante.capacidadeMaxima}";

        if (iconesDosSlots == null) return;

        for (int i = 0; i < iconesDosSlots.Length; i++)
        {
            if (iconesDosSlots[i] == null) continue;

            bool temPocao = i < quantidade;
            iconesDosSlots[i].gameObject.SetActive(temPocao);

            if (temPocao)
                iconesDosSlots[i].sprite = GetIcone(estante.TipoNoSlot(i));
        }
    }

    private Sprite GetIcone(PotionType tipo)
    {
        if (icones == null) return null;

        foreach (PotionIconEntry entry in icones)
        {
            if (entry.tipo == tipo) return entry.icone;
        }
        return null;
    }

    private void MostrarMensagem(string texto)
    {
        if (textoMensagem == null) return;

        // Se já tem uma mensagem na tela, reinicia o tempo em vez de empilhar
        if (rotinaMensagem != null) StopCoroutine(rotinaMensagem);
        rotinaMensagem = StartCoroutine(RotinaMensagem(texto));
    }

    private IEnumerator RotinaMensagem(string texto)
    {
        textoMensagem.text = texto;
        textoMensagem.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(duracaoMensagem);
        textoMensagem.gameObject.SetActive(false);
        rotinaMensagem = null;
    }

    private void Tremer()
    {
        if (alvoDaTremida == null) return;

        if (rotinaTremida != null) StopCoroutine(rotinaTremida);
        rotinaTremida = StartCoroutine(RotinaTremida());
    }

    private IEnumerator RotinaTremida()
    {
        float t = 0f;
        while (t < duracaoDaTremida)
        {
            t += Time.unscaledDeltaTime;
            Vector2 desvio = Random.insideUnitCircle * forcaDaTremida;
            alvoDaTremida.localPosition = origemTremida + new Vector3(desvio.x, 0f, desvio.y);
            yield return null;
        }

        alvoDaTremida.localPosition = origemTremida; // sempre volta pro lugar certo
        rotinaTremida = null;
    }

    private void Tocar(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
