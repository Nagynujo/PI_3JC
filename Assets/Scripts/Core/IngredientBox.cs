using UnityEngine;


public class IngredientBox : MonoBehaviour, IInteractable
{
    [Tooltip("Prefab do ingrediente que essa caixa cria (ex: EscamaDeDragao)")]
    public GameObject ingredientPrefab;

    public void Interact(PlayerController player)
    {
        
        if (player.HeldIngredient) return;

        
        GameObject spawned = Instantiate(ingredientPrefab, transform.position, Quaternion.identity);
        IngredientBase ingredient = spawned.GetComponent<IngredientBase>();

        
        player.PickUpIngredient(ingredient);
        Debug.Log("Player pegou ingrediente");
    }
}
