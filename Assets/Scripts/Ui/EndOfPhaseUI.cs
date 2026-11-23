using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndOfPhaseUI : MonoBehaviour
{
    [Header("Referências (arrastar na cena)")]
    public GameObject painel;

    [Tooltip("Arraste as 3 imagens de estrela, na ordem 1ª/2ª/3ª")]
    public Image[] estrelas;
    public Sprite estrelaPreenchida;
    public Sprite estrelaVazia;

    [Tooltip("Opcional")]
    public TextMeshProUGUI textoPontuacao;

    [Header("Navegação (botões do painel de fim de fase)")]
    [Tooltip("Nome exato da cena do menu inicial, igual aparece em File > Build Settings")]
    public string nomeCenaMenu = "MainMenu";

    void Awake()
    {
        if (painel != null) painel.SetActive(false);
    }

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("EndOfPhaseUI: não achei o GameManager na cena.");
            return;
        }
        GameManager.Instance.OnFaseAcabou += MostrarResultado;
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnFaseAcabou -= MostrarResultado;
    }

    private void MostrarResultado(int quantidadeEstrelas)
    {
        if (painel != null) painel.SetActive(true);

        for (int i = 0; i < estrelas.Length; i++)
        {
            estrelas[i].sprite = i < quantidadeEstrelas ? estrelaPreenchida : estrelaVazia;
        }

        if (textoPontuacao != null && ScoreManager.Instance != null)
            textoPontuacao.text = $"{ScoreManager.Instance.pontuacao} pontos";
    }

    
    public void JogarNovamente()
    {
        Scene cenaAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(cenaAtual.name);
    }

    
    public void VoltarAoMenu()
    {
        SceneManager.LoadScene(nomeCenaMenu);
    }
}
