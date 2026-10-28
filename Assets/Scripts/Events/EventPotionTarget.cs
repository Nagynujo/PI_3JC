using UnityEngine;

/// <summary>
/// Alvo onde a Bruna aplica uma poção para resolver um evento (nuvem de ratos, fonte de calor...).
/// Segue o mesmo padrão de Estante/Caldeirao: o PlayerController acha este script pelo OnTriggerStay,
/// então o Collider (Is Trigger) precisa estar NO MESMO objeto. Aperte E segurando a poção.
/// Só a poção certa resolve; outra poção é ignorada e NÃO é consumida.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EventPotionTarget : MonoBehaviour, IInteractable
{
    [Tooltip("Evento que este alvo resolve (RatsEvent, IcedEvent...).")]
    public GameEventBase targetEvent;
    [Tooltip("Consome a poção da mão quando ela resolve o evento.")]
    public bool consumePotion = true;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    public void Interact(PlayerController player)
    {
        if (!isActiveAndEnabled || targetEvent == null || !targetEvent.IsActive) return;

        IngredientBase held = player.HeldIngredient;
        if (!Estante.EhPocaoEngarrafada(held))
        {
            Debug.Log("Precisa estar segurando uma poção engarrafada");
            return;
        }

        if (targetEvent.TryResolveWithPotion(held.potionType))
        {
            Debug.Log($"{targetEvent.EventName} resolvido com {held.potionType}");
            if (consumePotion) player.ClearHand();
        }
        else
        {
            Debug.Log($"{held.potionType} não resolve {targetEvent.EventName}");
        }
    }
}
