using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuração da fase")]
    public float duracaoDaFase = 180f;

    [Header("Metas de estrelas (pontuação mínima)")]
    [Tooltip("Pontuação mínima pra ganhar 2 estrelas. Abaixo disso, 1 estrela.")]
    public int pontuacaoPara2Estrelas = 300;
    [Tooltip("Pontuação mínima pra ganhar 3 estrelas.")]
    public int pontuacaoPara3Estrelas = 600;

    [Header("Estado (só leitura, pra debug)")]
    public float tempoRestante;
    public bool faseAcabou;

    public event Action<float> OnTempoAtualizado;
    public event Action<int> OnFaseAcabou;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        tempoRestante = duracaoDaFase;
    }

    void Update()
    {
        if (faseAcabou) return;

        tempoRestante -= Time.deltaTime;
        OnTempoAtualizado?.Invoke(tempoRestante);

        if (tempoRestante <= 0f)
        {
            tempoRestante = 0f;
            TerminarFase();
        }
    }

    
    public void TerminarFase()
    {
        if (faseAcabou) return;
        faseAcabou = true;

        if (OrderManager.Instance != null)
            OrderManager.Instance.PausarPedidos();

        int estrelas = CalcularEstrelas();
        int pontuacao = ScoreManager.Instance != null ? ScoreManager.Instance.pontuacao : 0;
        Debug.Log($"Fase acabou! Pontuação: {pontuacao} -> {estrelas} estrela(s)");

        OnFaseAcabou?.Invoke(estrelas);
    }

    private int CalcularEstrelas()
    {
        int pontuacao = ScoreManager.Instance != null ? ScoreManager.Instance.pontuacao : 0;

        if (pontuacao >= pontuacaoPara3Estrelas) return 3;
        if (pontuacao >= pontuacaoPara2Estrelas) return 2;
        return 1;
    }
}
