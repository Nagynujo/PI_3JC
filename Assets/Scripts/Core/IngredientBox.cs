using UnityEngine;


public class IngredientBox : MonoBehaviour, IInteractable
{
    [Tooltip("Prefab do ingrediente que essa caixa cria (ex: EscamaDeDragao)")]
    public GameObject ingredientPrefab;

    public bool RequiresHold => false;
    public float HoldDuration => 0f;

    public void Interact(PlayerController player)
    {
        
        if (player.HeldIngredient) return;

        
        GameObject spawned = Instantiate(ingredientPrefab, transform.position, Quaternion.identity);
        IngredientBase ingredient = spawned.GetComponent<IngredientBase>();

        
        player.PickUpIngredient(ingredient);
        Debug.Log("Player pegou ingrediente");
    }

    public bool CanStartHold(PlayerController player) => false;
    public void OnHoldStart(PlayerController player) { }
    public void OnHoldProgress(PlayerController player, float progress) { }
    public void OnHoldComplete(PlayerController player) { }
    public void OnHoldCancelled(PlayerController player) { }
}