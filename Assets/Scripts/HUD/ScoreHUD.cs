using UnityEngine;
using TMPro;

public class ScoreHUD : MonoBehaviour
{
    [Header("Referência (arrastar na cena)")]
    public TextMeshProUGUI textoPontuacao;

    [Tooltip("Texto antes do número, ex: 'Pontos: '")]
    public string prefixo = "Pontos: ";

    void Start()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("ScoreHUD: não achei o ScoreManager na cena.");
            return;
        }

        AtualizarTexto(ScoreManager.Instance.pontuacao);
        ScoreManager.Instance.OnPontuacaoAlterada += AtualizarTexto;
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.OnPontuacaoAlterada -= AtualizarTexto;
    }

    private void AtualizarTexto(int novaPontuacao)
    {
        if (textoPontuacao != null)
            textoPontuacao.text = prefixo + novaPontuacao;
    }
}
