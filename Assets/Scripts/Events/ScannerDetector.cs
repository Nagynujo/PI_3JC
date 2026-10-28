using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecção em cone SEPARADA da apresentação visual. Começa desligada e sem consequências:
/// assine TargetEntered / TargetExited no futuro para criar regras de gameplay.
/// (Não faz teste de linha de visão; adicione Physics.Linecast se precisar.)
/// </summary>
public class ScannerDetector : MonoBehaviour
{
    public Transform origin;
    public LayerMask targetMask = ~0;
    public float checkInterval = 0.1f;
    public bool detectionEnabled = false;

    public event System.Action<Collider> TargetEntered;
    public event System.Action<Collider> TargetExited;

    private float range = 10f, angle = 35f, timer;
    private readonly Collider[] buffer = new Collider[32];
    private readonly HashSet<Collider> inside = new HashSet<Collider>();
    private readonly HashSet<Collider> now = new HashSet<Collider>();
    private readonly List<Collider> left = new List<Collider>();

    public void Configure(float newRange, float newAngle) { range = newRange; angle = newAngle; }

    private void OnDisable()
    {
        foreach (var c in inside) if (c != null) TargetExited?.Invoke(c);
        inside.Clear();
    }

    private void Update()
    {
        if (!detectionEnabled || origin == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        now.Clear();
        int n = Physics.OverlapSphereNonAlloc(origin.position, range, buffer, targetMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Vector3 to = buffer[i].bounds.center - origin.position;
            if (Vector3.Angle(origin.forward, to) <= angle * 0.5f) now.Add(buffer[i]);
        }
        foreach (var c in now) if (inside.Add(c)) TargetEntered?.Invoke(c);

        left.Clear();
        foreach (var c in inside) if (c == null || !now.Contains(c)) left.Add(c);
        foreach (var c in left) { inside.Remove(c); if (c != null) TargetExited?.Invoke(c); }
    }
}
