using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multiplicadores temporários de gameplay, sem tocar em Time.timeScale nem nos valores originais.
/// Cada fonte (evento, buff, etc.) registra o seu; o resultado é o produto de todos.
/// Ao remover uma fonte, o valor volta exatamente ao que os outros modificadores indicam.
/// </summary>
public static class EventModifiers
{
    private static readonly Dictionary<object, float> move = new Dictionary<object, float>();
    private static readonly Dictionary<object, float> action = new Dictionary<object, float>();
    private static readonly HashSet<object> paralyzers = new HashSet<object>();

    /// <summary>Multiplique a velocidade de movimento da personagem por isto.</summary>
    public static float MoveSpeedMultiplier => paralyzers.Count > 0 ? 0f : Product(move);
    /// <summary>Multiplique o avanço de progresso das ações (cortar, misturar...) por isto.</summary>
    public static float ActionSpeedMultiplier => paralyzers.Count > 0 ? 0f : Product(action);

    /// <summary>True enquanto qualquer fonte (ex.: Olhão) mantém a Bruna paralisada.</summary>
    public static bool IsParalyzed => paralyzers.Count > 0;
    public static void SetParalyzed(object source, bool on)
    {
        if (on) paralyzers.Add(source); else paralyzers.Remove(source);
    }

    public static void SetMove(object source, float m) { move[source] = Mathf.Max(0.01f, m); }
    public static void SetAction(object source, float m) { action[source] = Mathf.Max(0.01f, m); }
    public static void RemoveAll(object source) { move.Remove(source); action.Remove(source); paralyzers.Remove(source); }

    private static float Product(Dictionary<object, float> d)
    {
        float r = 1f;
        foreach (var kv in d) r *= kv.Value;
        return r;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { move.Clear(); action.Clear(); paralyzers.Clear(); }
}
