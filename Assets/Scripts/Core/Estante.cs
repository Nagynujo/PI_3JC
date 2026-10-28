using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estante que guarda até 'capacidadeMaxima' poções engarrafadas (padrão: 3).
///
/// Com uma poção engarrafada na mão -> E guarda (se tiver vaga).
/// Com a mão vazia                  -> E pega uma poção guardada.
///
/// A poção guardada é o próprio objeto da garrafa (IngredientBase), então o visual
/// na estante é o mesmo modelo/material que a poção tem na cena. O objeto fica
/// sentado nos 'pontosDasPocoes' e é reorganizado sempre que alguém é retirado.
///
/// Mesmo padrão do StationBase/Caldeirao: o PlayerController acha essa estante pelo
/// OnTriggerStay, então o Collider (Is Trigger) precisa estar NO MESMO objeto deste script.
/// </summary>
public class Estante : MonoBehaviour, IInteractable
{
    [Header("Configuração")]
    [Tooltip("Quantas poções cabem na estante")]
    public int capacidadeMaxima = 3;

    [Tooltip("Pontos (objetos vazios filhos da estante) onde cada poção fica sentada visualmente. Precisa de 1 ponto por vaga. Se deixar vazio, usa deslocamentos padrão.")]
    public List<Transform> pontosDasPocoes;

    [Tooltip("Ao pegar, dá preferência à poção que um pedido ativo está pedindo (começando pelo pedido mais antigo). Desligado = sempre pega a que foi guardada há mais tempo.")]
    public bool priorizarPedidos = true;

    [Header("Estado (só leitura, pra debug)")]
    public List<IngredientBase> pocoesGuardadas = new List<IngredientBase>();

    public int Quantidade => pocoesGuardadas.Count;
    public bool EstaCheia => Quantidade >= capacidadeMaxima;
    public bool EstaVazia => Quantidade == 0;

    /// <summary>Disparado sempre que entra, sai ou é reorganizada uma poção (a UI se atualiza por aqui).</summary>
    public event Action OnMudou;
    /// <summary>Tentou guardar com a estante cheia (a poção continua na mão).</summary>
    public event Action OnTentouGuardarCheia;
    /// <summary>Tentou pegar com a estante vazia.</summary>
    public event Action OnTentouPegarVazia;
    /// <summary>Tentou guardar algo que não é uma poção engarrafada (ingrediente, garrafa vazia...).</summary>
    public event Action OnItemInvalido;

    void Awake()
    {
        if (capacidadeMaxima < 1) capacidadeMaxima = 1;

        if (pontosDasPocoes == null || pontosDasPocoes.Count < capacidadeMaxima)
        {
            Debug.LogWarning($"{gameObject.name}: 'Pontos Das Pocoes' tem menos pontos que a capacidade ({capacidadeMaxima}). " +
                "As poções vão usar posições padrão, que provavelmente não batem com o modelo da estante.");
        }
    }

    public void Interact(PlayerController player)
    {
        // Garante que nenhuma referência quebrada (poção destruída por fora) conte como vaga ocupada
        LimparReferenciasQuebradas();

        if (player.HeldIngredient != null)
            TentarGuardar(player);
        else
            TentarPegar(player);
    }

    private void TentarGuardar(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;

        if (!EhPocaoEngarrafada(held))
        {
            Debug.Log("A estante só guarda poções engarrafadas");
            OnItemInvalido?.Invoke();
            return;
        }

        if (EstaCheia)
        {
            Debug.Log("Estante cheia — a poção continua na mão");
            OnTentouGuardarCheia?.Invoke();
            return;
        }

        // Só tira da mão depois de ter certeza que cabe (a poção nunca some)
        IngredientBase pocao = player.ReleaseHand();
        pocao.transform.SetParent(transform);
        pocoesGuardadas.Add(pocao);
        Reposicionar();

        Debug.Log($"Estante: guardou {pocao.potionType} ({Quantidade}/{capacidadeMaxima})");
        OnMudou?.Invoke();
    }

    private void TentarPegar(PlayerController player)
    {
        if (EstaVazia)
        {
            Debug.Log("Estante vazia");
            OnTentouPegarVazia?.Invoke();
            return;
        }

        int indice = EscolherIndiceParaPegar();
        IngredientBase pocao = pocoesGuardadas[indice];
        pocoesGuardadas.RemoveAt(indice);

        pocao.transform.SetParent(null);
        player.PickUpIngredient(pocao); // mostra na mão e esconde o objeto da cena
        Reposicionar();

        Debug.Log($"Estante: pegou {pocao.potionType} ({Quantidade}/{capacidadeMaxima})");
        OnMudou?.Invoke();
    }

    /// <summary>
    /// Entrega direto da estante, sem passar pela mão: procura o pedido ativo mais
    /// antigo que tenha uma poção correspondente guardada. Usado pela DeliveryStation.
    /// A poção só sai da estante se o OrderManager realmente aceitar a entrega.
    /// </summary>
    public bool TentarEntregarPedidoDaEstante()
    {
        LimparReferenciasQuebradas();

        if (OrderManager.Instance == null || EstaVazia) return false;

        List<Order> pedidos = OrderManager.Instance.pedidosAtivos;
        for (int p = 0; p < pedidos.Count; p++)
        {
            PotionType tipo = pedidos[p].tipo;
            int indice = IndiceDaPocao(tipo);
            if (indice < 0) continue;

            if (!OrderManager.Instance.TentarEntregar(tipo)) continue;

            IngredientBase pocao = pocoesGuardadas[indice];
            pocoesGuardadas.RemoveAt(indice);
            Destroy(pocao.gameObject);
            Reposicionar();

            Debug.Log($"Estante: entregou {tipo} direto ({Quantidade}/{capacidadeMaxima})");
            OnMudou?.Invoke();
            return true; // a lista de pedidos mudou, então sai do loop
        }

        return false;
    }

    /// <summary>Tipo da poção em cada posição (0 = a mais antiga). None se a posição estiver vazia.</summary>
    public PotionType TipoNoSlot(int indice)
    {
        if (indice < 0 || indice >= pocoesGuardadas.Count) return PotionType.None;
        IngredientBase pocao = pocoesGuardadas[indice];
        return pocao != null ? pocao.potionType : PotionType.None;
    }

    /// <summary>Mesma regra usada pela DeliveryStation pra saber se é uma poção pronta.</summary>
    public static bool EhPocaoEngarrafada(IngredientBase item)
    {
        return item != null && item.estaEngarrafada && item.potionType != PotionType.None;
    }

    private int EscolherIndiceParaPegar()
    {
        if (priorizarPedidos && OrderManager.Instance != null)
        {
            List<Order> pedidos = OrderManager.Instance.pedidosAtivos;
            for (int i = 0; i < pedidos.Count; i++)
            {
                int indice = IndiceDaPocao(pedidos[i].tipo);
                if (indice >= 0) return indice;
            }
        }

        return 0; // a que foi guardada há mais tempo
    }

    private int IndiceDaPocao(PotionType tipo)
    {
        for (int i = 0; i < pocoesGuardadas.Count; i++)
        {
            if (pocoesGuardadas[i] != null && pocoesGuardadas[i].potionType == tipo) return i;
        }
        return -1;
    }

    private void LimparReferenciasQuebradas()
    {
        int removidas = pocoesGuardadas.RemoveAll(p => p == null);
        if (removidas > 0)
        {
            Reposicionar();
            OnMudou?.Invoke();
        }
    }

    /// <summary>Reassenta todas as poções nos pontos, em ordem (sem buracos no meio).</summary>
    private void Reposicionar()
    {
        for (int i = 0; i < pocoesGuardadas.Count; i++)
        {
            PosicionarNoPonto(pocoesGuardadas[i], i);
        }
    }

    private void PosicionarNoPonto(IngredientBase item, int indice)
    {
        if (pontosDasPocoes != null && indice < pontosDasPocoes.Count && pontosDasPocoes[indice] != null)
        {
            item.transform.position = pontosDasPocoes[indice].position;
            item.transform.rotation = pontosDasPocoes[indice].rotation;
        }
        else
        {
            // Fallback: enfileira as poções lado a lado, acima da estante
            float x = (indice - (capacidadeMaxima - 1) / 2f) * 0.4f;
            item.transform.localPosition = new Vector3(x, 1f, 0f);
            item.transform.localRotation = Quaternion.identity;
        }
    }
}
