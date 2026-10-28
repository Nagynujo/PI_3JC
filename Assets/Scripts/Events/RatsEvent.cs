using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Evento Rats. Resolvido apenas com Poison aplicada na nuvem (EventPotionTarget em cloudTarget).</summary>
public class RatsEvent : GameEventBase
{
    [Header("Referências")]
    public RatAgent ratPrefab;
    [Tooltip("Pontos de onde os ratos surgem (ex.: buracos na parede).")]
    public Transform[] spawnPoints;
    [Tooltip("Vazio = encontra o PlayerController da cena uma única vez no Awake.")]
    public Transform player;
    [Tooltip("Centro da sala, usado para os ratos correrem de forma imprevisível.")]
    public Transform roomCenterRef;
    public float roomRadius = 8f;

    [Header("Nuvem de ratos (alvo da poção Poison)")]
    [Tooltip("Objeto com Collider (Is Trigger) + EventPotionTarget (Target Event = este RatsEvent). Começa desativado; segue o centro do bando.")]
    public GameObject cloudTarget;
    public float cloudFollowSpeed = 6f;

    [Header("Onda")]
    [Min(1)] public int ratCount = 4;

    [Header("Velocidades")]
    public float chaseSpeed = 3.5f;
    public float dashSpeed = 7f;
    public float scatterSpeed = 5f;

    [Header("Ataque")]
    public float attackRange = 2.5f;
    public float windupTime = 0.6f;
    public float dashDuration = 0.35f;
    public float hitDistance = 0.9f;
    public float pushForce = 9f;
    public float minSeparation = 1f;

    [Header("Fase de corrida após o empurrão")]
    public float scatterTimeMin = 2f;
    public float scatterTimeMax = 4f;

    [Header("Anti-travamento")]
    public float stuckSpeed = 0.1f;
    public float stuckTime = 1.2f;

    [Header("Feedback visual")]
    public GameObject spawnVfx;
    public GameObject disperseVfx;
    public float disperseDuration = 0.6f;

    private readonly List<RatAgent> rats = new List<RatAgent>();
    private PlayerPushReceiver receiver;

    public Vector3 RoomCenter => roomCenterRef != null ? roomCenterRef.position : transform.position;

    private void Awake()
    {
        if (player == null)
        {
#if UNITY_2023_1_OR_NEWER
            var pc = FindFirstObjectByType<PlayerController>();
#else
            var pc = FindObjectOfType<PlayerController>();
#endif
            if (pc != null) player = pc.transform;
        }
        if (cloudTarget != null) cloudTarget.SetActive(false);
    }

    private PlayerPushReceiver GetReceiver()
    {
        if (receiver == null && player != null)
        {
            receiver = player.GetComponentInParent<PlayerPushReceiver>();
            if (receiver == null) receiver = player.gameObject.AddComponent<PlayerPushReceiver>();
        }
        return receiver;
    }

    private void LateUpdate()
    {
        // A nuvem acompanha o centro do bando para a Bruna poder aplicar a poção nela.
        if (!IsActive || cloudTarget == null || rats.Count == 0) return;
        Vector3 sum = Vector3.zero; int n = 0;
        foreach (var r in rats) if (r != null) { sum += r.transform.position; n++; }
        if (n == 0) return;
        cloudTarget.transform.position = Vector3.Lerp(cloudTarget.transform.position, sum / n, cloudFollowSpeed * Time.deltaTime);
    }

    protected override void OnActivated()
    {
        if (ratPrefab == null || player == null)
        {
            Debug.LogError("[RatsEvent] Configure ratPrefab e player no Inspector.", this);
            return;
        }
        GetReceiver();

        for (int i = 0; i < ratCount; i++) SpawnRat(i);

        if (cloudTarget != null)
        {
            cloudTarget.transform.position = RoomCenter;
            cloudTarget.SetActive(true);
        }
    }

    private void SpawnRat(int i)
    {
        Vector3 basePos = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[i % spawnPoints.Length].position
            : RoomCenter + new Vector3(Mathf.Cos(i), 0f, Mathf.Sin(i)) * roomRadius;
        basePos += new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
        if (NavMesh.SamplePosition(basePos, out var hit, 3f, NavMesh.AllAreas)) basePos = hit.position;

        var rat = Instantiate(ratPrefab, basePos, Quaternion.identity);
        rat.GetComponent<NavMeshAgent>().Warp(basePos);
        if (spawnVfx != null) Destroy(Instantiate(spawnVfx, basePos, Quaternion.identity), 3f);
        rat.Init(this, player, GetReceiver());
        rats.Add(rat);
    }

    /// <summary>Opcional: útil para exigir que a poção seja jogada perto da nuvem de ratos.</summary>
    public bool IsPointNearSwarm(Vector3 point, float radius)
    {
        foreach (var r in rats)
            if (r != null && Vector3.Distance(r.transform.position, point) <= radius) return true;
        return false;
    }

    protected override IEnumerator OnResolving()
    {
        if (cloudTarget != null) cloudTarget.SetActive(false); // não aceita mais poção
        foreach (var r in rats) if (r != null) r.Disperse(disperseDuration, disperseVfx);
        yield return new WaitForSeconds(disperseDuration + 0.05f);
        rats.Clear();
    }

    protected override void OnForceEnded()
    {
        if (cloudTarget != null) cloudTarget.SetActive(false);
        foreach (var r in rats) if (r != null) Destroy(r.gameObject);
        rats.Clear();
    }
}
