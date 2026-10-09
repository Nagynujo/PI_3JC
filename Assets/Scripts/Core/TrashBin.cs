using UnityEngine;

public class TrashBin : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController player)
    {
        if (player.HeldIngredient == null)
        {
            Debug.Log("Nada na mão pra descartar");
            return;
        }

        Debug.Log($"Descartou {player.HeldIngredient.name} na lixeira");
        player.ClearHand(); 
    }
}
