using UnityEngine;


public class StationBase : MonoBehaviour, IInteractable
{
    [Header("Configuração da Estação")]
    [Tooltip("Qual tipo de ingrediente essa estação aceita")]
    public ProcessType tipoAceito;

    [Tooltip("Tempo em segundos que o player precisa segurar E pra cortar/moer o ingrediente")]
    public float tempoDeProcesso = 2f;

    public IngredientBase itemAtual;
    public bool estaOcupada;

    // --- IInteractable ---

    public bool RequiresHold => true;
    public float HoldDuration => tempoDeProcesso;

    public void Interact(PlayerController player)
    {
        // Estação usa interação segurada — ver CanStartHold/OnHoldStart/OnHoldComplete abaixo.
        // Esse método fica vazio porque RequiresHold é true (PlayerController nunca o chama).
    }

    public bool CanStartHold(PlayerController player)
    {
        if (estaOcupada) return false;

        IngredientBase held = player.HeldIngredient;
        if (held == null) return false;

        return held.tipoDeProcesso == tipoAceito;
    }

    public void OnHoldStart(PlayerController player)
    {
        itemAtual = player.ReleaseHeldIngredient();
        estaOcupada = true;
        Debug.Log($"{gameObject.name} começou a processar {itemAtual.name}");
    }

    public void OnHoldProgress(PlayerController player, float progress)
    {
        // Ponto de extensão: atualizar barra de progresso / animação de corte-moagem
        // Ex: progressBar.fillAmount = progress;
    }

    public void OnHoldComplete(PlayerController player)
    {
        Debug.Log($"{gameObject.name} terminou de processar {itemAtual.name}");
        // TODO: marcar itemAtual como processado (ex: itemAtual.MarcarComoProcessado())
        estaOcupada = false;
    }

    public void OnHoldCancelled(PlayerController player)
    {
        // Player soltou E ou saiu do alcance antes de terminar: devolve o ingrediente pra mão dele
        if (itemAtual != null)
        {
            itemAtual.gameObject.SetActive(true);
            player.PickUpIngredient(itemAtual);
        }

        Debug.Log($"{gameObject.name} teve o processamento cancelado");
        itemAtual = null;
        estaOcupada = false;
    }
}