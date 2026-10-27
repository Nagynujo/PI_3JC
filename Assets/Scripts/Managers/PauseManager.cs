using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Referências (arrastar na cena)")]
    public GameObject painelPause;
    public GameObject painelSettings;

    [Header("Efeitos Sonoros")]
    public AudioSource somClique; // Arraste o AudioSource com o som de clique/bolhas

    [Tooltip("Nome exato da cena do menu inicial/seleção de fase, igual aparece em File > Build Settings")]
    public string nomeCenaMenu = "MainMenu";

    private InputAction pauseAction;
    private bool pausado;

    void Awake()
    {
        pauseAction = new InputAction("Pausar", InputActionType.Button, binding: "<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");

        if (painelPause != null) painelPause.SetActive(false);
        if (painelSettings != null) painelSettings.SetActive(false);
    }

    void OnEnable() => pauseAction.Enable();
    void OnDisable() => pauseAction.Disable();

    void OnDestroy()
    {
        // Por segurança: se esse objeto for destruído enquanto pausado
        // (trocar de cena, etc), não deixa o jogo travado em timeScale 0.
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (pauseAction.WasPressedThisFrame())
        {
            if (pausado) Despausar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        pausado = true;
        if (painelPause != null) painelPause.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Despausar()
    {
        TocarSom();
        pausado = false;
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSettings != null) painelSettings.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>Chame no OnClick do botão "Continuar".</summary>
    public void BotaoContinuar() => Despausar();

    /// <summary>Chame no OnClick do botão "Configurações".</summary>
    public void AbrirSettings()
    {
        TocarSom();
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSettings != null) painelSettings.SetActive(true);
    }

    /// <summary>Chame no OnClick do botão "Voltar" dentro das Configurações.</summary>
    public void FecharSettings()
    {
        TocarSom();
        if (painelSettings != null) painelSettings.SetActive(false);
        if (painelPause != null) painelPause.SetActive(true);
    }

    /// <summary>Chame no OnClick do botão "Reiniciar Fase".</summary>
    public void ReiniciarFase()
    {
        TocarSom();
        Time.timeScale = 1f;
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }

    /// <summary>Chame no OnClick do botão "Menu Principal" / "Seleção de Fase".</summary>
    public void VoltarAoMenu()
    {
        TocarSom();
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCenaMenu);
    }

    private void TocarSom()
    {
        if (somClique != null)
        {
            somClique.Play();
        }
    }
}