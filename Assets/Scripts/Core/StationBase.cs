using UnityEngine;


public class StationBase : MonoBehaviour, IInteractable
{
    [Tooltip("Qual tipo de ingrediente essa estação aceita")]
    public ProcessType tipoAceito;

    public IngredientBase itemAtual;
    public bool estaOcupada;

    public virtual void Interact(PlayerController player)
    {
        if (estaOcupada) return;

        IngredientBase held = player.HeldIngredient;
        if (held == null) return;

        if (held.tipoDeProcesso == tipoAceito)
        {
            itemAtual = held;
            estaOcupada = true;
            player.ClearHand();
            Debug.Log($"{gameObject.name} recebeu {held.name}");
            // TODO: próximo passo - mini-interação de processar (timer/QTE)
        }
        else
        {
            Debug.Log("Ingrediente errado pra essa estação");
        }
    }
}
