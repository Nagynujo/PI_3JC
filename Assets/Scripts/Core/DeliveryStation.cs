using UnityEngine;

public class DeliveryStation : MonoBehaviour, IInteractable
{
    [Tooltip("Opcional: arraste a Estante aqui. Com a mão vazia, apertar E na entrega usa uma poção da estante que combine com algum pedido ativo. Deixe vazio pra manter o comportamento antigo (só entrega o que está na mão).")]
    public Estante estante;

    public void Interact(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;

        // Mão vazia + estante ligada: tenta entregar direto da estante
        if (held == null && estante != null)
        {
            if (estante.TentarEntregarPedidoDaEstante())
                Debug.Log("Entrega concluída direto da estante");
            else
                Debug.Log("Nenhuma poção da estante combina com os pedidos ativos");
            return;
        }

        if (held == null || !held.estaEngarrafada || held.potionType == PotionType.None)
        {
            Debug.Log("Precisa estar segurando uma poção engarrafada pra entregar");
            return;
        }

        bool entregue = OrderManager.Instance != null && OrderManager.Instance.TentarEntregar(held.potionType);

        if (entregue)
        {
            Debug.Log($"Entrega concluída: {held.potionType}");
            player.ClearHand();
        }
        else
        {
            Debug.Log($"Nenhum pedido pede {held.potionType} agora — guarda a poção");
        }
    }
}
