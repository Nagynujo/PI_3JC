using UnityEngine;

public class DeliveryStation : MonoBehaviour, IInteractable
{

    public void Interact(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;

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
