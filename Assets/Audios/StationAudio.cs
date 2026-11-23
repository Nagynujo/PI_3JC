using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Adicione na estacao (caldeirao, mesa de corte...) e escolha os eventos no Inspector.
/// Nao depende de mais nenhum script: usa o FMOD direto.
///
///   Begin()               -> quando a estacao COMECA a trabalhar (loop liga)
///   SetProgress(0..1)     -> opcional, se o evento tiver o parametro "Progress"
///   End(true)             -> terminou (toca o som de conclusao, se houver)
///   End(false)            -> cancelou (toca o som de cancelar, se houver)
/// </summary>
public class StationAudio : MonoBehaviour
{
    [Header("Eventos desta estacao")]
    [SerializeField] EventReference loopEvent;      // ex.: event:/borbulho
    [SerializeField] EventReference completeEvent;  // opcional
    [SerializeField] EventReference cancelEvent;    // opcional

    [Header("Parametro do FMOD (opcional)")]
    [SerializeField] string progressParameter = "Progress";

    EventInstance loopInstance;
    bool running;

    public void Begin()
    {
        if (running || loopEvent.IsNull) return;

        loopInstance = RuntimeManager.CreateInstance(loopEvent);
        loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        loopInstance.start();
        running = true;
    }

    public void SetProgress(float normalized)
    {
        if (!running) return;
        loopInstance.setParameterByName(progressParameter, Mathf.Clamp01(normalized));
    }

    public void End(bool completed)
    {
        StopLoop(true);

        EventReference finishing = completed ? completeEvent : cancelEvent;
        if (!finishing.IsNull)
            RuntimeManager.PlayOneShot(finishing, transform.position);
    }

    void StopLoop(bool fadeOut)
    {
        if (!running) return;

        loopInstance.stop(fadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        loopInstance.release();
        loopInstance.clearHandle();
        running = false;
    }

    void OnDisable() => StopLoop(false);
    void OnDestroy() => StopLoop(false);
}