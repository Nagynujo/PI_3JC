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
    [Tooltip("RectTransform do painel embaixo da tela. NÃO precisa de Layout Group — a posição de cada card é calculada manualmente, em fileira.")]
    public RectTransform container;
    public OrderSlotUI slotPrefab;
    public List<PotionIconEntry> icones;

    [Header("Layout em fileira (estilo Overcooked)")]
    [Tooltip("Espaço horizontal (em pixels) entre um pedido e o próximo")]
    public float espacamentoHorizontal = 160f;
    [Tooltip("O primeiro pedido (mais antigo) fica à esquerda (-1) ou pode inverter pra direita (1)")]
    public float direcao = 1f;

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
        // Agora TODOS os pedidos visíveis mostram o cronômetro rodando, não só o mais antigo.
        foreach (Order pedido in pedidosNaOrdem)
        {
            if (slots.TryGetValue(pedido, out OrderSlotUI slot) && slot != null)
            {
                slot.AtualizarProgresso(pedido.Progresso01, pedido.tempoRestante);
            }
        }
    }

    private void HandlePedidoCriado(Order pedido) => HandlePedidoCriado(pedido, tocarSom: true);

    private void HandlePedidoCriado(Order pedido, bool tocarSom)
    {
        if (slotPrefab == null || container == null) return;

        OrderSlotUI slot = Instantiate(slotPrefab, container);
        slot.Configurar(GetIcone(pedido.tipo), PotionNomes.EmPortugues(pedido.tipo));
        slots[pedido] = slot;
        pedidosNaOrdem.Add(pedido);

        if (tocarSom && audioSource != null && somNovoPedido != null)
            audioSource.PlayOneShot(somNovoPedido);

        ReconstruirFileira();
    }

    private void HandlePedidoRemovido(Order pedido)
    {
        if (slots.TryGetValue(pedido, out OrderSlotUI slot))
        {
            if (slot != null) Destroy(slot.gameObject);
            slots.Remove(pedido);
        }
        pedidosNaOrdem.Remove(pedido);

        ReconstruirFileira();
    }

    /// <summary>
    /// Reposiciona todos os pedidos em fileira: o mais antigo primeiro, os
    /// seguintes do lado, todos em tamanho normal e totalmente visíveis —
    /// igual o Overcooked, pra dar pra ver o que vem a seguir.
    /// </summary>
    private void ReconstruirFileira()
    {
        for (int i = 0; i < pedidosNaOrdem.Count; i++)
        {
            Order pedido = pedidosNaOrdem[i];
            if (!slots.TryGetValue(pedido, out OrderSlotUI slot) || slot == null) continue;

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(espacamentoHorizontal * i * direcao, 0f);
            rect.localScale = Vector3.one;

            slot.DefinirComoUrgente(i == 0);
            slot.transform.SetSiblingIndex(i);
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
