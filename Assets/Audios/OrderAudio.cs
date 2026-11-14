using FMODUnity;
using UnityEngine;

/// <summary>
/// Sons dos pedidos. Nao precisa mexer no OrderManager: ele escuta os eventos dele.
/// Pode ficar em qualquer objeto da cena (por exemplo, no proprio OrderManager).
/// Campos sem evento escolhido sao ignorados.
/// </summary>
public class OrderAudio : MonoBehaviour
{
    [Header("Eventos do FMOD")]
    [SerializeField] EventReference novoPedido;       // ex.: event:/sino_pedido
    [SerializeField] EventReference pedidoEntregue;   // ex.: event:/moeda

    OrderManager manager;

    void Start()
    {
        manager = OrderManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("OrderAudio: nenhum OrderManager encontrado na cena.");
            return;
        }

        manager.OnPedidoCriado += AoCriarPedido;
        manager.OnPedidoEntregue += AoEntregarPedido;
    }

    void OnDestroy()
    {
        if (manager == null) return;

        manager.OnPedidoCriado -= AoCriarPedido;
        manager.OnPedidoEntregue -= AoEntregarPedido;
    }

    void AoCriarPedido(Order pedido)
    {
        if (novoPedido.IsNull) return;
        RuntimeManager.PlayOneShot(novoPedido);
    }

    void AoEntregarPedido(Order pedido)
    {
        if (pedidoEntregue.IsNull) return;
        RuntimeManager.PlayOneShot(pedidoEntregue);
    }
}