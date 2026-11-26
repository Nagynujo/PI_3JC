using System.Collections;
using FMODUnity;
using UnityEngine;

// [LUCCA - SOM] ARQUIVO NOVO: sons dos ratos (hazard). Mesmo padrao do OrderAudio / StationAudio.

/// <summary>
/// Sons do rato. Coloque no PREFAB do rato (ou no spawner) e escolha os eventos no Inspector.
/// Nao depende de mais nenhum script: usa o FMOD direto.
///
///   Surgir()  -> quando o rato aparece
///   Sumir()   -> quando o rato some / e eliminado / foge (opcional)
///
/// Como aparecem varios ratos juntos (4 por vez), os sons de SURGIR nao tocam no mesmo
/// frame: cada chamada recebe um atraso aleatorio e existe um intervalo minimo entre sons
/// de ratos diferentes, compartilhado por todos. Campos sem evento escolhido sao ignorados.
/// </summary>
public class RatAudio : MonoBehaviour
{
    [Header("Eventos do FMOD")]
    [SerializeField] EventReference somSurgir;   // ex.: event:/rato_surgir (o multi instrument)
    [SerializeField] EventReference somSumir;    // opcional

    [Header("Espalhar os sons (varios ratos juntos)")]
    [Tooltip("Atraso aleatorio maximo (segundos) antes de tocar o som de SURGIR")]
    [SerializeField, Range(0f, 1f)] float atrasoMaximo = 0.35f;

    [Tooltip("Intervalo minimo (segundos) entre sons de SURGIR de ratos diferentes")]
    [SerializeField, Range(0f, 0.5f)] float intervaloMinimo = 0.12f;

    // Compartilhado entre TODOS os ratos: instante a partir do qual o proximo som de surgir pode comecar.
    static float proximoSomLivre;

    // Zera ao entrar no Play (evita valor velho se o "Domain Reload" estiver desligado no editor).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetarEstatico() => proximoSomLivre = 0f;

    /// <summary>Chame quando o rato aparece.</summary>
    public void Surgir()
    {
        if (somSurgir.IsNull) return;
        StartCoroutine(TocarEspalhado());
    }

    /// <summary>Chame quando o rato some, e eliminado ou foge (opcional).</summary>
    public void Sumir()
    {
        if (somSumir.IsNull) return;
        RuntimeManager.PlayOneShot(somSumir, transform.position);
    }

    IEnumerator TocarEspalhado()
    {
        // Sorteia um atraso, mas nunca antes do proximo horario livre (fila entre os ratos).
        float inicio = Mathf.Max(Time.time + Random.Range(0f, atrasoMaximo), proximoSomLivre);
        proximoSomLivre = inicio + intervaloMinimo;

        float espera = inicio - Time.time;
        if (espera > 0f) yield return new WaitForSeconds(espera);

        RuntimeManager.PlayOneShot(somSurgir, transform.position);
    }
}
