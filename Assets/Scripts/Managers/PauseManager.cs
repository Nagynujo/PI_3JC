using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pause. Usa Time.timeScale = 0 enquanto pausado — como o tempo de
/// fase (GameManager) e os pedidos (OrderManager) usam Time.deltaTime, eles
/// já congelam sozinhos sem precisar de nenhum código extra neles.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Referências (arrastar na cena)")]
    public GameObject painelPause;
    public GameObject painelSettings;

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
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSettings != null) painelSettings.SetActive(true);
    }

    /// <summary>Chame no OnClick do botão "Voltar" dentro das Configurações.</summary>
    public void FecharSettings()
    {
        if (painelSettings != null) painelSettings.SetActive(false);
        if (painelPause != null) painelPause.SetActive(true);
    }

    /// <summary>Chame no OnClick do botão "Reiniciar Fase".</summary>
    public void ReiniciarFase()
    {
        Time.timeScale = 1f;
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }

    /// <summary>Chame no OnClick do botão "Menu Principal" / "Seleção de Fase".</summary>
    public void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCenaMenu);
    }
}
