using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Pontos")]
    public int pontosPorEntrega = 100;
    public int pontosPerdidosPorExpirado = 50;

    [Header("Estado (só leitura, pra debug)")]
    public int pontuacao;
    public int pedidosEntregues;
    public int pedidosExpirados;

    public event Action<int> OnPontuacaoAlterada;

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
        if (OrderManager.Instance == null)
        {
            Debug.LogWarning("ScoreManager: não achei o OrderManager na cena.");
            return;
        }
        OrderManager.Instance.OnPedidoEntregue += HandlePedidoEntregue;
        OrderManager.Instance.OnPedidoExpirado += HandlePedidoExpirado;
    }

    void OnDestroy()
    {
        if (OrderManager.Instance == null) return;
        OrderManager.Instance.OnPedidoEntregue -= HandlePedidoEntregue;
        OrderManager.Instance.OnPedidoExpirado -= HandlePedidoExpirado;
    }

    private void HandlePedidoEntregue(Order pedido)
    {
        pedidosEntregues++;
        pontuacao += pontosPorEntrega;
        OnPontuacaoAlterada?.Invoke(pontuacao);
    }

    private void HandlePedidoExpirado(Order pedido)
    {
        pedidosExpirados++;
        pontuacao = Mathf.Max(0, pontuacao - pontosPerdidosPorExpirado);
        OnPontuacaoAlterada?.Invoke(pontuacao);
    }
}