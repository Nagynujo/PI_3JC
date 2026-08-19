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
        return player.HeldIngredient != null;
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
        Debug.Log($"Entrega concluída: {player.HeldIngredient.name}");
        player.ClearHand(); 
    }

    public void OnHoldCancelled(PlayerController player)
    {
        Debug.Log("Entrega cancelada");
    }
}