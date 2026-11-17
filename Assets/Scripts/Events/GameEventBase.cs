using System.Collections;
using UnityEngine;

public enum GameEventState { Inactive, Active, Resolving }

/// <summary>
/// Base de todos os contratempos (Rats, Iced, Olhão).
/// O gerenciador decide QUANDO e QUAL evento começa; cada evento cuida do próprio
/// comportamento, feedback visual e condição de resolução.
/// </summary>
public abstract class GameEventBase : MonoBehaviour
{
    [Header("Evento")]
    [SerializeField] private string eventName;
    [Tooltip("Poção que resolve o evento (Rats = Poison, Iced = Fire). None = não resolve por poção.")]
    [SerializeField] private PotionType requiredPotion = PotionType.None;

    public string EventName => string.IsNullOrEmpty(eventName) ? GetType().Name : eventName;
    public PotionType RequiredPotion => requiredPotion;
    public GameEventState State { get; private set; } = GameEventState.Inactive;
    public bool IsActive => State == GameEventState.Active;
    /// <summary>Ativo OU ainda encerrando efeitos (ex.: ratos se dispersando).</summary>
    public bool IsBusy => State != GameEventState.Inactive;

    public event System.Action<GameEventBase> Started;
    /// <summary>Disparado só depois que TODOS os efeitos foram revertidos.</summary>
    public event System.Action<GameEventBase> Ended;

    public bool TryActivate()
    {
        if (IsBusy) return false;
        State = GameEventState.Active;
        OnActivated();
        Started?.Invoke(this);
        return true;
    }

    /// <summary>Chamado pelo sistema de poções/entrega quando uma poção é aplicada.</summary>
    public bool TryResolveWithPotion(PotionType potion)
    {
        if (!IsActive || requiredPotion == PotionType.None) return false;
        if (potion != requiredPotion) return false; // outras poções nunca resolvem
        Resolve();
        return true;
    }

    /// <summary>Inicia o encerramento normal (com animação de saída, se houver).</summary>
    protected void Resolve()
    {
        if (!IsActive) return;
        State = GameEventState.Resolving;
        StartCoroutine(ResolveRoutine());
    }

    private IEnumerator ResolveRoutine()
    {
        yield return StartCoroutine(OnResolving());
        Finish();
    }

    /// <summary>Encerramento imediato (fim da fase, reinício, desativação do objeto).</summary>
    public void ForceEnd()
    {
        if (!IsBusy) return;
        StopAllCoroutines();
        OnForceEnded();
        Finish();
    }

    private void Finish()
    {
        State = GameEventState.Inactive;
        Ended?.Invoke(this);
    }

    private void OnDisable()
    {
        if (IsBusy) ForceEnd();
    }

    protected abstract void OnActivated();
    /// <summary>Reverte efeitos de forma animada. O evento só termina quando esta rotina acabar.</summary>
    protected virtual IEnumerator OnResolving() { yield break; }
    /// <summary>Reverte TUDO imediatamente, sem animações.</summary>
    protected abstract void OnForceEnded();
}
