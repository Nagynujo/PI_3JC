using UnityEngine;


public class IngredientBox : MonoBehaviour, IInteractable
{
    [Tooltip("Prefab do ingrediente que essa caixa cria (ex: EscamaDeDragao)")]
    public GameObject ingredientPrefab;

    // Pegar ingrediente da caixa é instantâneo, não precisa segurar E
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

    // Interação instantânea não usa os métodos de hold abaixo — ficam vazios.
    public bool CanStartHold(PlayerController player) => false;
    public void OnHoldStart(PlayerController player) { }
    public void OnHoldProgress(PlayerController player, float progress) { }
    public void OnHoldComplete(PlayerController player) { }
    public void OnHoldCancelled(PlayerController player) { }
}