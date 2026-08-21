using UnityEngine;

public class DeliveryStation : MonoBehaviour, IInteractable
{
    [Tooltip("Tempo em segundos que o player precisa segurar E pra entregar")]
    public float tempoDeEntrega = 1f;
    public bool RequiresHold => true;
    public float HoldDuration => tempoDeEntrega;

    public void Interact(PlayerController player)
    {
        
    }

    public bool CanStartHold(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;
        return held != null && held.estaEngarrafada && held.potionType != PotionType.None;
    }

    public void OnHoldStart(PlayerController player)
    {
        Debug.Log($"Começando a entrega de {player.HeldIngredient.name}...");
    }

    public void OnHoldProgress(PlayerController player, float progress)
    {
        
    }

    public void OnHoldComplete(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;
        if (held == null) return;

        bool entregue = OrderManager.Instance != null && OrderManager.Instance.TentarEntregar(held.potionType);

        if (entregue)
        {
            Debug.Log($"Entrega concluída: {held.potionType}");
            player.ClearHand();
        }
        else
        {
            Debug.Log($"Nenhum pedido pede {held.potionType} agora - guarda a poção");
        }
    }

    public void OnHoldCancelled(PlayerController player)
    {
        Debug.Log("Entrega cancelada");
    }
}
