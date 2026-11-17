
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;

public class PauseSettingsTransition : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField] private VideoPlayer videoPlayerPause;

    [Header("Videos")]
    [SerializeField] private VideoClip videoPause;
    [SerializeField] private VideoClip videoSettingsEntrada;
    [SerializeField] private VideoClip videoSettingsSaida;

    [Header("Velocidade das Transicoes")]
    [Range(0.1f, 2f)]
    [SerializeField] private float velocidade = 1f;

    [Header("Botoes do Menu Pause")]
    [SerializeField] private GameObject botaoResume;
    [SerializeField] private GameObject botaoSettings;
    [SerializeField] private GameObject botaoRetry;
    [SerializeField] private GameObject botaoMainMenu;

    [Header("Botao Back do Settings")]
    [SerializeField] private GameObject botaoBackSettings;

    [Header("Ao terminar de voltar")]
    [SerializeField] private UnityEvent aoFinalizarVolta;

    private enum Estado
    {
        Pause,
        AbrindoSettings,
        SettingsAberto,
        FechandoSettings
    }

    private Estado estado = Estado.Pause;

    private float velocidadePause = 1f;

    private void Awake()
    {
        MostrarBotaoBack(false);

        if (videoPlayerPause == null)
        {
            Debug.LogError(
                "[PauseSettings] VideoPlayer nao configurado!"
            );
            return;
        }

        velocidadePause = videoPlayerPause.playbackSpeed;

        if (videoPause == null)
        {
            Debug.LogWarning(
                "[PauseSettings] Configure o video original do Pause!"
            );
        }

        videoPlayerPause.loopPointReached += VideoTerminou;
    }

    private void OnDestroy()
    {
        if (videoPlayerPause != null)
            videoPlayerPause.loopPointReached -= VideoTerminou;
    }

    private bool PrepararVideoPlayer()
    {
        if (videoPlayerPause == null)
        {
            Debug.LogError(
                "[PauseSettings] VideoPlayer nao configurado!"
            );
            return false;
        }

        if (!videoPlayerPause.gameObject.activeSelf)
            videoPlayerPause.gameObject.SetActive(true);

        if (!videoPlayerPause.enabled)
            videoPlayerPause.enabled = true;

        if (!videoPlayerPause.isActiveAndEnabled)
        {
            Debug.LogError(
                "[PauseSettings] VideoPlayer inativo. " +
                "Verifique os objetos pais na Hierarchy."
            );
            return false;
        }

        return true;
    }

    public void AbrirSettings()
    {
        if (estado != Estado.Pause)
            return;

        if (videoSettingsEntrada == null)
        {
            Debug.LogWarning(
                "[PauseSettings] Video de entrada nao configurado!"
            );
            return;
        }

        if (!TocarVideo(videoSettingsEntrada, false, velocidade))
            return;

        estado = Estado.AbrindoSettings;

        MostrarBotoesPause(false);
        MostrarBotaoBack(false);
    }

    private void Update()
    {
        if (videoPlayerPause == null)
            return;

        if (estado != Estado.AbrindoSettings &&
            estado != Estado.FechandoSettings)
            return;

        VideoClip clipEsperado =
            estado == Estado.AbrindoSettings
            ? videoSettingsEntrada
            : videoSettingsSaida;

        if (clipEsperado == null ||
            videoPlayerPause.clip != clipEsperado)
            return;

        bool chegouAoFim = false;

        // Verifica o ultimo frame.
        if (videoPlayerPause.frameCount > 0 &&
            videoPlayerPause.frame >=
                (long)videoPlayerPause.frameCount - 1)
        {
            chegouAoFim = true;
        }

        // Verificacao adicional pelo tempo do video.
        if (clipEsperado.length > 0 &&
            videoPlayerPause.time >=
                clipEsperado.length - 0.03)
        {
            chegouAoFim = true;
        }

        if (!chegouAoFim)
            return;

        if (estado == Estado.AbrindoSettings)
        {
            CongelarSettings();
        }
        else if (estado == Estado.FechandoSettings)
        {
            RestaurarPause();
        }
    }

    private void CongelarSettings()
    {
        if (estado != Estado.AbrindoSettings)
            return;

        estado = Estado.SettingsAberto;

        if (videoPlayerPause != null &&
            videoPlayerPause.isActiveAndEnabled)
        {
            videoPlayerPause.Pause();
        }

        MostrarBotaoBack(true);

        Debug.Log(
            "[PauseSettings] Settings aberto. Back ativado."
        );
    }

    public void VoltarDoSettings()
    {
        if (estado != Estado.SettingsAberto)
            return;

        if (videoSettingsSaida == null)
        {
            Debug.LogWarning(
                "[PauseSettings] Video reverso nao configurado!"
            );
            return;
        }

        if (!TocarVideo(videoSettingsSaida, false, velocidade))
            return;

        estado = Estado.FechandoSettings;

        MostrarBotaoBack(false);
        MostrarBotoesPause(false);

        Debug.Log(
            "[PauseSettings] Animacao reversa iniciada."
        );
    }

    private bool TocarVideo(
        VideoClip clip,
        bool repetir,
        float velocidadeReproducao)
    {
        if (clip == null || !PrepararVideoPlayer())
            return false;

        videoPlayerPause.Stop();

        videoPlayerPause.source = VideoSource.VideoClip;
        videoPlayerPause.clip = clip;
        videoPlayerPause.isLooping = repetir;
        videoPlayerPause.playbackSpeed = velocidadeReproducao;

        videoPlayerPause.Play();

        Debug.Log(
            "[PauseSettings] Reproduzindo: " + clip.name +
            " | Loop: " + repetir
        );

        return true;
    }

    private void VideoTerminou(VideoPlayer vp)
    {
        if (estado == Estado.AbrindoSettings)
        {
            CongelarSettings();
        }
        else if (estado == Estado.FechandoSettings)
        {
            RestaurarPause();
        }
    }

    private void RestaurarPause()
    {
        if (estado != Estado.FechandoSettings)
            return;

        if (videoPause == null)
        {
            Debug.LogError(
                "[PauseSettings] O campo Video Pause esta vazio! " +
                "Arraste o video original do menu Pause no Inspector."
            );
            return;
        }

        estado = Estado.Pause;

        MostrarBotaoBack(false);

        // Fecha o painel de Settings e executa
        // as funcoes configuradas no Inspector.
        aoFinalizarVolta?.Invoke();

        // Restaura os botoes originais.
        MostrarBotoesPause(true);

        // Restaura explicitamente o video do Pause
        // com a reproducao em loop ativada.
        if (TocarVideo(videoPause, true, velocidadePause))
        {
            Debug.Log(
                "[PauseSettings] Pause restaurado com sucesso: " +
                videoPause.name
            );
        }
    }

    private void MostrarBotoesPause(bool mostrar)
    {
        if (botaoResume != null)
            botaoResume.SetActive(mostrar);

        if (botaoSettings != null)
            botaoSettings.SetActive(mostrar);

        if (botaoRetry != null)
            botaoRetry.SetActive(mostrar);

        if (botaoMainMenu != null)
            botaoMainMenu.SetActive(mostrar);
    }

    private void MostrarBotaoBack(bool mostrar)
    {
        if (botaoBackSettings != null)
            botaoBackSettings.SetActive(mostrar);
    }
}
