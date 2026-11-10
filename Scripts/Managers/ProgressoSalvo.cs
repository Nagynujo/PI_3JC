using UnityEngine;

/// <summary>
/// Guarda o progresso do jogador entre sessões (PlayerPrefs = fica salvo mesmo
/// fechando o jogo). Usado pelo GameManager (salva estrelas ao fim de uma fase)
/// e pelo LevelSelectManager (lê pra decidir o que já foi desbloqueado).
/// </summary>
public static class ProgressoSalvo
{
    private const string PrefixoEstrelas = "Estrelas_";

    /// <summary>
    /// Salva a quantidade de estrelas de uma fase, mas só se for MELHOR do que
    /// a que já estava salva (repetir uma fase não derruba sua melhor marca).
    /// </summary>
    public static void SalvarEstrelas(string idFase, int estrelas)
    {
        if (string.IsNullOrEmpty(idFase)) return;

        int atual = ObterEstrelas(idFase);
        if (estrelas > atual)
        {
            PlayerPrefs.SetInt(PrefixoEstrelas + idFase, estrelas);
            PlayerPrefs.Save();
        }
    }

    public static int ObterEstrelas(string idFase)
    {
        if (string.IsNullOrEmpty(idFase)) return 0;
        return PlayerPrefs.GetInt(PrefixoEstrelas + idFase, 0);
    }

    /// <summary>Apaga todo o progresso salvo. Útil pra um botão de "resetar save" nas Configurações.</summary>
    public static void ApagarTudo()
    {
        PlayerPrefs.DeleteAll();
    }
}
