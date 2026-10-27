using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class MenuVideoChanger : MonoBehaviour
{
    [Header("Componentes de Vídeo e UI")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject menuPanel;

    [Header("Áudios")]
    [SerializeField] private AudioSource musicaMenuAudioSource; // Música de fundo em loop
    [SerializeField] private AudioSource efeitoBolhasAudioSource; // Efeito sonoro de bolhas

    [Header("Vídeos")]
    [SerializeField] private VideoClip startVideo;

    [Header("Cena do Jogo")]
    [SerializeField] private string nomeDaProximaCena = "LevelSelect";

    private bool jaIniciou = false;

    public void OnStartButtonClick()
    {
        if (jaIniciou) return;
        jaIniciou = true;

        // 1. Esconde os botões da UI
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 2. Para a música do menu
        if (musicaMenuAudioSource != null && musicaMenuAudioSource.isPlaying)
        {
            musicaMenuAudioSource.Stop();
        }

        // 3. Toca o efeito de som das bolhas
        if (efeitoBolhasAudioSource != null)
        {
            efeitoBolhasAudioSource.Play();
        }

        // 4. Troca e inicia o vídeo de transição
        if (videoPlayer != null && startVideo != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = startVideo;
            videoPlayer.isLooping = false;

            videoPlayer.loopPointReached += CarregarCena;
            videoPlayer.Play();
        }
        else
        {
            CarregarJogoDireto();
        }
    }

    private void CarregarCena(VideoPlayer vp)
    {
        vp.loopPointReached -= CarregarCena;
        CarregarJogoDireto();
    }

    private void CarregarJogoDireto()
    {
        SceneManager.LoadScene(nomeDaProximaCena);
    }
}