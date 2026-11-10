using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class PassoDoTutorial
    {
        [TextArea(2, 4)]
        public string texto;
        [Tooltip("Opcional - uma imagem/print ilustrando o passo")]
        public Sprite imagem;
    }

    [Header("Passos (na ordem que aparecem)")]
    public List<PassoDoTutorial> passos = new List<PassoDoTutorial>
    {
        new PassoDoTutorial { texto = "Use WASD para se mover pela cozinha." },
        new PassoDoTutorial { texto = "Pressione E pra interagir: pegar ingredientes, usar estações, etc." },
        new PassoDoTutorial { texto = "Pegue um ingrediente cru na caixa correspondente e leve até a tábua de corte ou o moedor." },
        new PassoDoTutorial { texto = "Segure E na estação até a barra de progresso encher, processando o ingrediente." },
        new PassoDoTutorial { texto = "Leve os 2 ingredientes processados até o Caldeirão e interaja pra colocar cada um dentro." },
        new PassoDoTutorial { texto = "Com os 2 dentro, interaja de novo pra começar a fervura." },
        new PassoDoTutorial { texto = "Pegue uma garrafa vazia e, quando a poção estiver pronta, interaja com o Caldeirão pra engarrafar." },
        new PassoDoTutorial { texto = "Leve a poção até a Estação de Entrega e segure E pra entregar, cumprindo o pedido correspondente." },
        new PassoDoTutorial { texto = "Fique de olho nos pedidos no canto da tela — cada um tem um prazo! Aperte Tab a qualquer momento pra abrir o livro de receitas." },
    };

    [Header("Referências (arrastar na cena)")]
    public GameObject painel;
    public TextMeshProUGUI textoPasso;
    public Image imagemPasso;
    [Tooltip("Opcional - texto tipo '2 / 9' indicando o progresso")]
    public TextMeshProUGUI textoProgresso;

    [Header("Configuração")]
    [Tooltip("Pausa o jogo (Time.timeScale = 0) enquanto o tutorial estiver aberto — assim os pedidos/temporizadores não correm enquanto o jogador lê")]
    public bool pausarJogo = true;
    [Tooltip("Mostra o tutorial automaticamente assim que a cena carrega")]
    public bool mostrarAoIniciar = true;

    private int indiceAtual;
    private InputAction avancarAction;
    private bool tutorialAtivo;

    void Awake()
    {
        avancarAction = new InputAction("AvancarTutorial", InputActionType.Button);
        avancarAction.AddBinding("<Keyboard>/space");
        avancarAction.AddBinding("<Mouse>/leftButton");
        avancarAction.AddBinding("<Gamepad>/buttonSouth");

        if (painel != null) painel.SetActive(false);
    }

    void OnEnable()
    {
        avancarAction.Enable();
    }

    void OnDisable()
    {
        avancarAction.Disable();
    }

    void Start()
    {
        if (mostrarAoIniciar) IniciarTutorial();
    }

    void Update()
    {
        if (!tutorialAtivo) return;

        if (avancarAction.WasPressedThisFrame())
        {
            Proximo();
        }
    }

    public void IniciarTutorial()
    {
        if (passos.Count == 0 || painel == null) return;

        indiceAtual = 0;
        tutorialAtivo = true;
        painel.SetActive(true);

        if (pausarJogo) Time.timeScale = 0f;

        MostrarPasso();
    }

    /// <summary>Chame isso no OnClick de um botão "Próximo", se preferir usar botão em vez de Espaço/clique.</summary>
    public void Proximo()
    {
        indiceAtual++;

        if (indiceAtual >= passos.Count)
        {
            FecharTutorial();
            return;
        }

        MostrarPasso();
    }

    /// <summary>Chame isso no OnClick de um botão "Pular", se quiser permitir pular o tutorial.</summary>
    public void FecharTutorial()
    {
        tutorialAtivo = false;
        if (painel != null) painel.SetActive(false);
        if (pausarJogo) Time.timeScale = 1f;
    }

    private void MostrarPasso()
    {
        PassoDoTutorial passo = passos[indiceAtual];

        if (textoPasso != null) textoPasso.text = passo.texto;
        if (imagemPasso != null) imagemPasso.sprite = passo.imagem;
        if (textoProgresso != null) textoProgresso.text = $"{indiceAtual + 1} / {passos.Count}";
    }
}
