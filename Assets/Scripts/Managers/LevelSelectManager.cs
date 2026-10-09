using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    [System.Serializable]
    public class FaseInfo
    {
        [Tooltip("Identificador único dessa fase — TEM que ser igual ao 'Id Fase' configurado no GameManager daquela cena")]
        public string idFase;
        [Tooltip("Nome exato da cena dessa fase, igual aparece em File > Build Settings")]
        public string nomeCena;
        [Tooltip("Nome mostrado no botão")]
        public string nomeExibicao;
        [Tooltip("Quantas estrelas, SOMANDO todas as fases anteriores, são necessárias pra desbloquear essa")]
        public int estrelasNecessarias;

        [Header("UI dessa fase (arrastar)")]
        public Button botao;
        [Tooltip("Opcional - ícone de cadeado, mostrado só quando a fase está bloqueada")]
        public GameObject cadeado;
        [Tooltip("Opcional - texto tipo '2/3' com as estrelas já conquistadas nessa fase")]
        public TextMeshProUGUI textoEstrelas;
    }

    public List<FaseInfo> fases = new List<FaseInfo>();

    void Start()
    {
        AtualizarBotoes();
    }

    private void AtualizarBotoes()
    {
        int estrelasAcumuladas = 0;

        foreach (FaseInfo fase in fases)
        {
            bool desbloqueada = estrelasAcumuladas >= fase.estrelasNecessarias;

            if (fase.botao != null) fase.botao.interactable = desbloqueada;
            if (fase.cadeado != null) fase.cadeado.SetActive(!desbloqueada);

            int estrelasDessaFase = ProgressoSalvo.ObterEstrelas(fase.idFase);
            if (fase.textoEstrelas != null) fase.textoEstrelas.text = $"{estrelasDessaFase}/3";

            estrelasAcumuladas += estrelasDessaFase;
        }
    }

    /// <summary>
    /// Chame no OnClick de cada botão de fase, passando o índice dela nessa
    /// lista (0 pra primeira fase, 1 pra segunda, etc — no Button, o campo
    /// de parâmetro do OnClick vira um campo de número quando você escolhe
    /// esse método).
    /// </summary>
    public void SelecionarFase(int indice)
    {
        if (indice < 0 || indice >= fases.Count) return;

        FaseInfo fase = fases[indice];

        int estrelasAcumuladas = 0;
        for (int i = 0; i < indice; i++)
            estrelasAcumuladas += ProgressoSalvo.ObterEstrelas(fases[i].idFase);

        if (estrelasAcumuladas < fase.estrelasNecessarias)
        {
            Debug.Log($"Fase '{fase.nomeExibicao}' ainda está trancada (precisa de {fase.estrelasNecessarias} estrelas, tem {estrelasAcumuladas}).");
            return;
        }

        SceneManager.LoadScene(fase.nomeCena);
    }
}
