using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecção em cone SEPARADA da apresentação visual.
/// Modo 1 (recomendado): preencha trackedTargets (ex.: a Bruna) — testa só por geometria
/// (distância + ângulo), sem depender de Physics, Layers nem do número de colliders da sala.
/// Modo 2: sem trackedTargets, usa OverlapSphere com targetMask.
/// Começa desligada; assine TargetEntered / TargetExited para criar regras de gameplay.
/// (Não faz teste de linha de visão; adicione Physics.Linecast se precisar.)
/// </summary>
public class ScannerDetector : MonoBehaviour
{
    public Transform origin;
    public LayerMask targetMask = ~0;
    public float checkInterval = 0.05f;
    public bool detectionEnabled = false;

    [Tooltip("Alvos testados por geometria. Se vazio, usa Physics.OverlapSphere.")]
    public Transform[] trackedTargets;
    [Tooltip("Altura somada à posição do alvo (o pivô da Bruna costuma estar no chão).")]
    public float targetHeight = 1f;

    public event System.Action<Transform> TargetEntered;
    public event System.Action<Transform> TargetExited;

    private float range = 10f, angle = 35f, timer;
    private readonly Collider[] buffer = new Collider[256];
    private readonly HashSet<Transform> inside = new HashSet<Transform>();
    private readonly HashSet<Transform> now = new HashSet<Transform>();
    private readonly List<Transform> left = new List<Transform>();

    public void Configure(float newRange, float newAngle) { range = newRange; angle = newAngle; }

    private bool InCone(Vector3 point)
    {
        Vector3 to = point - origin.position;
        return to.magnitude <= range && Vector3.Angle(origin.forward, to) <= angle * 0.5f;
    }

    private void OnDisable()
    {
        foreach (var t in inside) if (t != null) TargetExited?.Invoke(t);
        inside.Clear();
    }

    private void Update()
    {
        if (!detectionEnabled || origin == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        now.Clear();
        if (trackedTargets != null && trackedTargets.Length > 0)
        {
            foreach (var t in trackedTargets)
                if (t != null && InCone(t.position + Vector3.up * targetHeight)) now.Add(t);
        }
        else
        {
            int n = Physics.OverlapSphereNonAlloc(origin.position, range, buffer, targetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (InCone(buffer[i].bounds.center)) now.Add(buffer[i].transform);
        }

        foreach (var t in now) if (inside.Add(t)) TargetEntered?.Invoke(t);

        left.Clear();
        foreach (var t in inside) if (t == null || !now.Contains(t)) left.Add(t);
        foreach (var t in left) { inside.Remove(t); if (t != null) TargetExited?.Invoke(t); }
    }
}
