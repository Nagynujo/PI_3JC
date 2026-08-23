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
    [Tooltip("RectTransform do painel embaixo da tela (com um Horizontal Layout Group)")]
    public RectTransform container;
    public OrderSlotUI slotPrefab;
    public List<PotionIconEntry> icones;

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

        // Caso já existam pedidos ativos antes desse objeto acordar
        foreach (Order pedido in OrderManager.Instance.pedidosAtivos)
        {
            HandlePedidoCriado(pedido);
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
        foreach (var kvp in slots)
        {
            Order pedido = kvp.Key;
            kvp.Value.AtualizarProgresso(pedido.Progresso01, pedido.tempoRestante);
        }
    }

    private void HandlePedidoCriado(Order pedido)
    {
        if (slotPrefab == null || container == null) return;

        OrderSlotUI slot = Instantiate(slotPrefab, container);
        slot.Configurar(GetIcone(pedido.tipo));
        slots[pedido] = slot;
    }

    private void HandlePedidoRemovido(Order pedido)
    {
        if (slots.TryGetValue(pedido, out OrderSlotUI slot))
        {
            if (slot != null) Destroy(slot.gameObject);
            slots.Remove(pedido);
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