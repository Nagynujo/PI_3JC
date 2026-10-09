using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("Configuração")]
    [Tooltip("Tipos de poção que podem ser pedidos (não coloque None aqui)")]
    public List<PotionType> tiposDisponiveis = new List<PotionType>
    {
        PotionType.Fire, PotionType.Acid, PotionType.Poison,
        PotionType.Invisibility, PotionType.Cure, PotionType.Fast
    };

    public int maxPedidosAtivos = 4;
    public float intervaloMinNovoPedido = 4f;
    public float intervaloMaxNovoPedido = 9f;

    [Header("Tempo de cada pedido (não é mais aleatório)")]
    [Tooltip("Tempo (segundos) do 1º pedido da fase — precisa dar pra fazer 1 poção com folga")]
    public float tempoBaseDoPedido = 20f;
    [Tooltip("A cada pedido novo, o tempo dobra em relação ao anterior (pedido 1 = X, pedido 2 = 2X, pedido 3 = 4X...), até esse número de dobras. Depois disso o tempo para de crescer, senão vira um valor absurdo com o tempo.")]
    public int limiteDeDobras = 4;

    [Header("Estado (só leitura, pra debug)")]
    public List<Order> pedidosAtivos = new List<Order>();

    public event Action<Order> OnPedidoCriado;
    public event Action<Order> OnPedidoEntregue;
    public event Action<Order> OnPedidoExpirado;

    private float tempoAteProximoPedido;
    private bool pausado;
    private int totalDePedidosCriados;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        AgendarProximoPedido();
    }

    public void PausarPedidos()
    {
        pausado = true;
    }

    void Update()
    {
        if (pausado) return;

        for (int i = pedidosAtivos.Count - 1; i >= 0; i--)
        {
            Order pedido = pedidosAtivos[i];
            pedido.tempoRestante -= Time.deltaTime;

            if (pedido.tempoRestante <= 0f)
            {
                pedidosAtivos.RemoveAt(i);
                Debug.Log($"Pedido expirou: {pedido.tipo}");
                OnPedidoExpirado?.Invoke(pedido);
            }
        }

        if (pedidosAtivos.Count < maxPedidosAtivos)
        {
            tempoAteProximoPedido -= Time.deltaTime;
            if (tempoAteProximoPedido <= 0f)
            {
                CriarPedido();
                AgendarProximoPedido();
            }
        }
    }

    private void AgendarProximoPedido()
    {
        tempoAteProximoPedido = UnityEngine.Random.Range(intervaloMinNovoPedido, intervaloMaxNovoPedido);
    }

    private void CriarPedido()
    {
        if (tiposDisponiveis.Count == 0) return;

        PotionType tipo = tiposDisponiveis[UnityEngine.Random.Range(0, tiposDisponiveis.Count)];
        float tempo = CalcularTempoDoPedido();

        Order pedido = new Order(tipo, tempo);
        pedidosAtivos.Add(pedido);
        totalDePedidosCriados++;

        Debug.Log($"Novo pedido: {tipo} ({tempo:0}s)");
        OnPedidoCriado?.Invoke(pedido);
    }

    /// <summary>
    /// Pedido 1 = tempoBaseDoPedido, pedido 2 = o dobro, pedido 3 = o dobro
    /// de novo, e assim por diante, até 'limiteDeDobras' — depois disso o
    /// tempo para de crescer (fica fixo no valor máximo).
    /// </summary>
    private float CalcularTempoDoPedido()
    {
        int dobras = Mathf.Min(totalDePedidosCriados, limiteDeDobras);
        float multiplicador = Mathf.Pow(2f, dobras);
        return tempoBaseDoPedido * multiplicador;
    }

    public bool TentarEntregar(PotionType tipo)
    {
        for (int i = 0; i < pedidosAtivos.Count; i++)
        {
            if (pedidosAtivos[i].tipo == tipo)
            {
                Order pedido = pedidosAtivos[i];
                pedidosAtivos.RemoveAt(i);
                Debug.Log($"Pedido entregue: {tipo}");
                OnPedidoEntregue?.Invoke(pedido);
                return true;
            }
        }
        return false;
    }
}
