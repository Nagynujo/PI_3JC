using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// =====================================================================================
// [LUCCA - SOM] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - SOM]")
//   1. Novo campo privado 'ratAudio' (referência ao componente RatAudio, ARQUIVO NOVO).
//   2. Init(): pega o RatAudio do próprio rato e toca RatAudio.Surgir() quando o rato
//      entra em jogo (é aqui que o RatsEvent "libera" cada rato depois do Instantiate).
//   3. Disperse(): toca RatAudio.Sumir() quando o rato começa a ser removido. Só toca se o
//      rato ainda estava ativo, pra não repetir o som se Disperse for chamado duas vezes.
//
// COMO FUNCIONA:
//   O RatAudio fica no MESMO objeto (prefab) do RatAgent. Ele espalha os sons de "surgir"
//   entre os 4 ratos (atraso aleatório + intervalo mínimo) e usa o evento de multi
//   instrument do FMOD. Se o prefab NÃO tiver RatAudio, o rato funciona normalmente, só
//   sem som (nenhum erro).
//
// ONDE CONFIGURAR:
//   No prefab do rato: Add Component > RatAudio e escolher os eventos no Inspector
//   (somSurgir e, opcionalmente, somSumir). Nada mais precisa mudar (RatsEvent continua igual).
//
// NÃO chamar RatAudio.Surgir() também em outro lugar (ex.: no RatsEvent), senão o som toca duas vezes.
//
// Todo o resto do arquivo está igual à versão anterior.
// =====================================================================================

/// <summary>
/// Um rato: perseguir -> avisar (windup) -> investir -> empurrar -> correr pela sala -> perseguir...
/// Usa NavMeshAgent (respeita obstáculos). Parâmetros vêm do RatsEvent.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RatAgent : MonoBehaviour
{
    private enum S { Chase, Windup, Dash, Scatter }

    [Tooltip("Objeto filho (ex.: ícone '!' ou brilho vermelho) ligado durante o aviso de investida.")]
    [SerializeField] private GameObject warningIndicator;

    private NavMeshAgent agent;
    private RatsEvent owner;
    private Transform target;
    private PlayerPushReceiver receiver;
    private S state;
    private float stateTimer, repathTimer, wanderTimer, stuckTimer;
    private bool active;

    // [LUCCA - SOM] NOVO: sons do rato (FMOD). Opcional: fica null se o prefab não tiver RatAudio.
    private RatAudio ratAudio;

    public void Init(RatsEvent ownerEvent, Transform player, PlayerPushReceiver pushReceiver)
    {
        agent = GetComponent<NavMeshAgent>();

        // [LUCCA - SOM] NOVO: procura o RatAudio no mesmo objeto (pode não existir).
        ratAudio = GetComponent<RatAudio>();

        owner = ownerEvent; target = player; receiver = pushReceiver;
        agent.stoppingDistance = 0.4f;
        if (warningIndicator != null) warningIndicator.SetActive(false);
        active = true;
        EnterChase();

        // [LUCCA - SOM] NOVO: som de o rato surgir. O RatAudio espalha o som entre os 4 ratos.
        if (ratAudio != null) ratAudio.Surgir();
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    private void Update()
    {
        if (!active || target == null || !agent.isOnNavMesh) return;
        float dist = Vector3.Distance(Flat(transform.position), Flat(target.position));
        float dt = Time.deltaTime;

        switch (state)
        {
            case S.Chase:
                repathTimer -= dt;
                if (repathTimer <= 0f) { agent.SetDestination(target.position); repathTimer = 0.2f; }
                if (dist <= owner.attackRange) EnterWindup();
                break;

            case S.Windup:
                FaceTarget(dt);
                stateTimer -= dt;
                if (stateTimer <= 0f) EnterDash();
                break;

            case S.Dash:
                stateTimer -= dt;
                agent.SetDestination(target.position);
                if (dist <= owner.hitDistance) Hit();
                else if (stateTimer <= 0f) EnterScatter();
                break;

            case S.Scatter:
                stateTimer -= dt; wanderTimer -= dt;
                if (wanderTimer <= 0f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f))
                    PickScatterPoint();
                if (stateTimer <= 0f) EnterChase();
                break;
        }
        UpdateStuck(dt);
    }

    private void EnterChase()
    {
        state = S.Chase; repathTimer = 0f; stuckTimer = 0f;
        agent.isStopped = false; agent.speed = owner.chaseSpeed;
    }

    private void EnterWindup()
    {
        state = S.Windup; stateTimer = owner.windupTime;
        agent.isStopped = true;
        if (warningIndicator != null) warningIndicator.SetActive(true); // jogador percebe a investida
    }

    private void EnterDash()
    {
        state = S.Dash; stateTimer = owner.dashDuration;
        if (warningIndicator != null) warningIndicator.SetActive(false);
        agent.isStopped = false; agent.speed = owner.dashSpeed;
    }

    private void Hit()
    {
        Vector2 r = Random.insideUnitCircle;
        if (r.sqrMagnitude < 0.01f) r = Vector2.up;
        Vector3 dir = new Vector3(r.x, 0f, r.y).normalized; // direção aleatória
        if (receiver != null) receiver.ApplyPush(dir, owner.pushForce);
        EnterScatter();
    }

    private void EnterScatter()
    {
        state = S.Scatter; stuckTimer = 0f;
        stateTimer = Random.Range(owner.scatterTimeMin, owner.scatterTimeMax);
        agent.isStopped = false; agent.speed = owner.scatterSpeed;
        PickScatterPoint();
    }

    private void PickScatterPoint()
    {
        wanderTimer = Random.Range(0.6f, 1.4f);
        Vector3 center = owner.RoomCenter;
        for (int i = 0; i < 6; i++)
        {
            Vector2 c = Random.insideUnitCircle * owner.roomRadius;
            Vector3 p = center + new Vector3(c.x, 0f, c.y);
            if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas) &&
                Vector3.Distance(Flat(hit.position), Flat(target.position)) > owner.minSeparation * 2f)
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    // Evita rato preso em obstáculo ou colado na personagem.
    private void UpdateStuck(float dt)
    {
        if (state == S.Windup || agent.isStopped) { stuckTimer = 0f; return; }
        if (agent.velocity.sqrMagnitude < owner.stuckSpeed * owner.stuckSpeed)
        {
            stuckTimer += dt;
            if (stuckTimer > owner.stuckTime)
            {
                stuckTimer = 0f;
                if (state == S.Scatter) PickScatterPoint(); else EnterScatter();
            }
        }
        else stuckTimer = 0f;
    }

    private void FaceTarget(float dt)
    {
        Vector3 d = Flat(target.position - transform.position);
        if (d.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 12f * dt);
    }

    /// <summary>Para o comportamento e encolhe o rato antes de removê-lo.</summary>
    public void Disperse(float duration, GameObject vfxPrefab)
    {
        // [LUCCA - SOM] NOVO: guarda se o rato ainda estava ativo, pra tocar o som de sumir
        // UMA vez só (Disperse chamado de novo não repete o som).
        bool estavaAtivo = active;

        active = false;
        if (warningIndicator != null) warningIndicator.SetActive(false);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (vfxPrefab != null) Destroy(Instantiate(vfxPrefab, transform.position, Quaternion.identity), 3f);

        // [LUCCA - SOM] NOVO: som de o rato sumir (opcional, só toca se houver evento no RatAudio).
        if (estavaAtivo && ratAudio != null) ratAudio.Sumir();

        StartCoroutine(Shrink(duration));
    }

    private IEnumerator Shrink(float duration)
    {
        Vector3 start = transform.localScale;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / duration);
            yield return null;
        }
        Destroy(gameObject);
    }
}