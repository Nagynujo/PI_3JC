using System.Collections.Generic;
using UnityEngine;

public class OrderUIManager : MonoBehaviour
{
    [System.Serializable]
    public class PotionIconEntry
    {
        public PotionType tipo;
        public Sprite icone;
    }

    [Header("Configuração")]
    [Tooltip("RectTransform do painel embaixo da tela. NÃO precisa mais de Horizontal Layout Group — a posição de cada carta agora é calculada manualmente (efeito de pilha).")]
    public RectTransform container;
    public OrderSlotUI slotPrefab;
    public List<PotionIconEntry> icones;

    [Header("Visual da pilha")]
    [Tooltip("Deslocamento (em pixels) de cada carta em relação à da frente")]
    public Vector2 deslocamentoPorCamada = new Vector2(10f, -10f);
    [Tooltip("Redução de escala por camada (0.06 = 6% menor a cada carta pra trás)")]
    public float reducaoEscalaPorCamada = 0.06f;
    [Tooltip("Redução de opacidade por camada")]
    public float reducaoOpacidadePorCamada = 0.2f;
    [Tooltip("Opacidade mínima, pra nunca sumir de vez")]
    public float opacidadeMinima = 0.25f;
    [Tooltip("A partir de quantas camadas as cartas param de se deslocar mais (evita a pilha sair da tela)")]
    public int maxCamadasDeslocamento = 4;

    [Header("Som")]
    public AudioSource audioSource;
    public AudioClip somNovoPedido;

    private readonly List<Order> pedidosNaOrdem = new List<Order>();
    private readonly Dictionary<Order, OrderSlotUI> slots = new Dictionary<Order, OrderSlotUI>();

    void Start()
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogWarning("OrderUIManager: não achei o OrderManager na cena. Confere se tem um GameObject com o script OrderManager.");
            return;
        }

        OrderManager.Instance.OnPedidoCriado += HandlePedidoCriado;
        OrderManager.Instance.OnPedidoEntregue += HandlePedidoRemovido;
        OrderManager.Instance.OnPedidoExpirado += HandlePedidoRemovido;

        // Caso já existam pedidos ativos antes desse objeto acordar (sem tocar som pros que já existiam)
        foreach (Order pedido in OrderManager.Instance.pedidosAtivos)
        {
            HandlePedidoCriado(pedido, tocarSom: false);
        }
    }

    void OnDestroy()
    {
        if (OrderManager.Instance == null) return;
        OrderManager.Instance.OnPedidoCriado -= HandlePedidoCriado;
        OrderManager.Instance.OnPedidoEntregue -= HandlePedidoRemovido;
        OrderManager.Instance.OnPedidoExpirado -= HandlePedidoRemovido;
    }

    void Update()
    {
        // Só a carta da frente (o pedido mais antigo) mostra o cronômetro rodando.
        if (pedidosNaOrdem.Count == 0) return;

        Order frente = pedidosNaOrdem[0];
        if (slots.TryGetValue(frente, out OrderSlotUI slot) && slot != null)
        {
            slot.AtualizarProgresso(frente.Progresso01, frente.tempoRestante);
        }
    }

    private void HandlePedidoCriado(Order pedido) => HandlePedidoCriado(pedido, tocarSom: true);

    private void HandlePedidoCriado(Order pedido, bool tocarSom)
    {
        if (slotPrefab == null || container == null) return;

        OrderSlotUI slot = Instantiate(slotPrefab, container);
        slot.Configurar(GetIcone(pedido.tipo));
        slots[pedido] = slot;
        pedidosNaOrdem.Add(pedido);

        if (tocarSom && audioSource != null && somNovoPedido != null)
            audioSource.PlayOneShot(somNovoPedido);

        ReconstruirPilha();
    }

    private void HandlePedidoRemovido(Order pedido)
    {
        if (slots.TryGetValue(pedido, out OrderSlotUI slot))
        {
            if (slot != null) Destroy(slot.gameObject);
            slots.Remove(pedido);
        }
        pedidosNaOrdem.Remove(pedido);

        ReconstruirPilha();
    }

    /// <summary>
    /// Reposiciona a pilha inteira: o pedido mais antigo (índice 0) fica na frente,
    /// em tamanho normal e totalmente visível. Os seguintes ficam empilhados atrás,
    /// cada vez mais deslocados/menores/apagados, só mostrando o "verso" do card.
    /// </summary>
    private void ReconstruirPilha()
    {
        // Percorre de trás pra frente e usa SetAsLastSibling a cada carta, assim a
        // última chamada (índice 0, a carta da frente) termina renderizada por cima.
        for (int i = pedidosNaOrdem.Count - 1; i >= 0; i--)
        {
            Order pedido = pedidosNaOrdem[i];
            if (!slots.TryGetValue(pedido, out OrderSlotUI slot) || slot == null) continue;

            RectTransform rect = slot.GetComponent<RectTransform>();
            int camada = Mathf.Min(i, maxCamadasDeslocamento);

            rect.anchoredPosition = deslocamentoPorCamada * camada;
            rect.localScale = Vector3.one * Mathf.Max(0.5f, 1f - reducaoEscalaPorCamada * camada);

            slot.DefinirOpacidade(Mathf.Max(opacidadeMinima, 1f - reducaoOpacidadePorCamada * camada));
            slot.DefinirComoFrente(i == 0);

            slot.transform.SetAsLastSibling();
        }
    }

    private Sprite GetIcone(PotionType tipo)
    {
        foreach (var entry in icones)
        {
            if (entry.tipo == tipo) return entry.icone;
        }
        return null;
    }
}
