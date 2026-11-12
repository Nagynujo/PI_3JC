using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador central: sorteia e ativa UM evento por vez, em intervalos configuráveis.
/// Usa Time.deltaTime (tempo normal da fase) — nunca altera Time.timeScale.
/// </summary>
public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    [Header("Eventos disponíveis (Rats, Iced, Olhão)")]
    [SerializeField] private List<GameEventBase> events = new List<GameEventBase>();

    [Header("Intervalo entre eventos (segundos de jogo)")]
    [SerializeField, Min(0f)] private float minInterval = 15f;
    [SerializeField, Min(0f)] private float maxInterval = 30f;
    [SerializeField] private bool autoStart = true;

    private GameManager gm;
    private GameEventBase currentEvent;
    private bool running;
    private float timer;
    private readonly List<GameEventBase> candidates = new List<GameEventBase>();

    public GameEventBase CurrentEvent => currentEvent;
    public bool HasActiveEvent => currentEvent != null;
    public event System.Action<GameEventBase> EventStarted;
    public event System.Action<GameEventBase> EventEnded;

    private void Reset() { events.AddRange(GetComponentsInChildren<GameEventBase>(true)); }
    private void OnValidate() { if (maxInterval < minInterval) maxInterval = minInterval; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        foreach (var e in events)
        {
            if (e == null) continue;
            e.Ended += HandleEnded;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gm != null) gm.OnFaseAcabou -= HandleFaseAcabou;
        foreach (var e in events) if (e != null) e.Ended -= HandleEnded;
    }

    private void Start()
    {
        // Integra com o GameManager existente: o cronômetro da fase continua dele;
        // quando a fase acaba, todos os eventos são encerrados e revertidos.
        if (GameManager.Instance != null)
        {
            gm = GameManager.Instance;
            gm.OnFaseAcabou += HandleFaseAcabou;
        }
        if (autoStart) StartSystem();
    }

    private void HandleFaseAcabou(int estrelas) { StopSystem(); }

    private void Update()
    {
        // Enquanto há evento ativo (ou encerrando), o sistema só espera.
        if (!running || currentEvent != null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) TryStartRandomEvent();
    }

    /// <summary>Fase começa SEM evento ativo e aguarda um intervalo aleatório.</summary>
    public void StartSystem()
    {
        if (gm != null && gm.faseAcabou) return;
        running = true;
        ScheduleNext();
    }

    /// <summary>Chamar no fim da fase / game over / pausa definitiva. Reverte tudo.</summary>
    public void StopSystem()
    {
        running = false;
        var e = currentEvent;
        if (e != null) e.ForceEnd();
        currentEvent = null;
    }

    public bool TryStartRandomEvent()
    {
        if (currentEvent != null) return false; // exclusividade

        candidates.Clear();
        foreach (var e in events)
            if (e != null && e.isActiveAndEnabled && !e.IsBusy) candidates.Add(e);

        if (candidates.Count == 0) { ScheduleNext(); return false; }

        var chosen = candidates[Random.Range(0, candidates.Count)]; // pode repetir o anterior
        currentEvent = chosen; // marca ANTES de ativar: bloqueia chamadas reentrantes
        if (!chosen.TryActivate())
        {
            currentEvent = null;
            ScheduleNext();
            return false;
        }
        EventStarted?.Invoke(chosen);
        return true;
    }

    /// <summary>
    /// Resolve o evento atual se a poção for a correta. Normalmente chamado pelo
    /// EventPotionTarget (alvo onde a Bruna aplica a poção).
    /// </summary>
    public bool TryResolveCurrent(PotionType potion)
    {
        return currentEvent != null && currentEvent.TryResolveWithPotion(potion);
    }

    private void HandleEnded(GameEventBase e)
    {
        if (e != currentEvent) return; // um evento nunca libera outro
        currentEvent = null;
        EventEnded?.Invoke(e);
        ScheduleNext();
    }

    private void ScheduleNext() { timer = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval)); }

    // ---- Testes no Editor (clique direito no componente, em Play Mode) ----
    [ContextMenu("DEBUG/Sortear evento agora")] private void DebugStart() { TryStartRandomEvent(); }
    [ContextMenu("DEBUG/Aplicar poção Poison")] private void DebugPoison() { Debug.Log("Poison resolveu? " + TryResolveCurrent(PotionType.Poison)); }
    [ContextMenu("DEBUG/Aplicar poção Fire")] private void DebugFire() { Debug.Log("Fire resolveu? " + TryResolveCurrent(PotionType.Fire)); }
    [ContextMenu("DEBUG/Parar sistema")] private void DebugStop() { StopSystem(); }
}
