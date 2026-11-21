using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Coloque no objeto do jogador (o mesmo do PlayerController).
/// O PlayerController chama SetWalking(true/false) conforme o jogador anda.
/// Toca o loop de passos do FMOD enquanto estiver andando.
/// </summary>
public class PlayerWalkAudio : MonoBehaviour
{
    [Header("Evento do FMOD")]
    [SerializeField] EventReference walkLoop;   // ex.: event:/andar

    [Header("Ajustes")]
    [Tooltip("Tempo (segundos) sem andar antes de parar o som. Evita picotar ao trocar de direcao.")]
    [SerializeField] float stopDelay = 0.1f;

    EventInstance instance;
    bool playing;
    bool walkingRequested;
    float stillTimer;

    /// <summary>Chamado todo frame pelo PlayerController.</summary>
    public void SetWalking(bool walking)
    {
        walkingRequested = walking;
    }

    void Update()
    {
        // Jogo pausado (Time.timeScale = 0): para o som de andar
        if (Time.timeScale == 0f)
        {
            if (playing) StopWalk(true);
            return;
        }

        if (walkingRequested)
        {
            stillTimer = 0f;
            if (!playing) StartWalk();
        }
        else
        {
            stillTimer += Time.deltaTime;
            if (playing && stillTimer >= stopDelay) StopWalk(true);
        }

        if (playing)
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        }
    }

    void StartWalk()
    {
        if (walkLoop.IsNull) return;

        instance = RuntimeManager.CreateInstance(walkLoop);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.start();
        playing = true;
    }

    void StopWalk(bool fadeOut)
    {
        if (!playing) return;

        instance.stop(fadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
        instance.clearHandle();
        playing = false;
    }

    void OnDisable() => StopWalk(false);
    void OnDestroy() => StopWalk(false);
}