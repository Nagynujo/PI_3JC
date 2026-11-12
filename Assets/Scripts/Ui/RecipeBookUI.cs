using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RecipeBookUI : MonoBehaviour
{
    [System.Serializable]
    public class IngredienteIconeEntry
    {
        public IngredientKind tipo;
        public Sprite icone;
    }

    [System.Serializable]
    public class PotionIconeEntry
    {
        public PotionType tipo;
        public Sprite icone;
    }

    [Header("Referências (arrastar na cena)")]
    [Tooltip("O Canvas/Painel do livro de receitas. Fica desativado até apertar a tecla.")]
    public GameObject painel;

    [Tooltip("O mesmo asset de receitas usado pelo Caldeirão")]
    public LivroDeReceitasSO livroDeReceitas;

    [Header("Modo com ícones (preenche isso pra usar imagens)")]
    [Tooltip("Onde as linhas (RecipeRowUI) são instanciadas, uma por receita")]
    public Transform containerReceitas;
    [Tooltip("Prefab de uma linha de receita (com RecipeRowUI)")]
    public RecipeRowUI linhaPrefab;
    public List<IngredienteIconeEntry> iconesIngredientes;
    public List<PotionIconeEntry> iconesPocoes;

    [Header("Modo texto (fallback - usado só se não preencher o modo com ícones acima)")]
    [Tooltip("Opcional - um texto único onde a lista de receitas é escrita automaticamente")]
    public TextMeshProUGUI textoReceitas;

    private InputAction toggleAction;
    private bool aberto;
    private bool linhasMontadas;

    void Awake()
    {
        toggleAction = new InputAction("AbrirLivroDeReceitas", InputActionType.Button, binding: "<Keyboard>/tab");
        toggleAction.AddBinding("<Gamepad>/select");

        if (painel != null) painel.SetActive(false);
    }

    void OnEnable()
    {
        toggleAction.Enable();
        MontarLivro();
    }

    void OnDisable()
    {
        toggleAction.Disable();
    }

    void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            aberto = !aberto;
            if (painel != null) painel.SetActive(aberto);
        }
    }

    private void MontarLivro()
    {
        if (linhasMontadas) return; // só monta uma vez, não precisa refazer toda hora que abre
        linhasMontadas = true;

        if (livroDeReceitas == null)
        {
            if (textoReceitas != null) textoReceitas.text = "(nenhum Livro De Receitas configurado)";
            Debug.LogWarning("RecipeBookUI: nenhum 'Livro De Receitas' configurado. Arraste o asset LivroDeReceitasSO no Inspector.");
            return;
        }

        bool modoIconesConfigurado = containerReceitas != null && linhaPrefab != null;

        if (modoIconesConfigurado)
        {
            MontarComIcones();
        }
        else
        {
            MontarComoTexto();
        }
    }

    private void MontarComIcones()
    {
        foreach (ReceitaPocao r in livroDeReceitas.receitas)
        {
            RecipeRowUI linha = Instantiate(linhaPrefab, containerReceitas);
            linha.Configurar(
                GetIconeIngrediente(r.ingrediente1),
                GetIconeIngrediente(r.ingrediente2),
                GetIconePocao(r.resultado),
                PotionNomes.EmPortugues(r.resultado)
            );
        }
    }

    private void MontarComoTexto()
    {
        if (textoReceitas == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (ReceitaPocao r in livroDeReceitas.receitas)
        {
            sb.AppendLine($"{r.ingrediente1} + {r.ingrediente2}  →  {PotionNomes.EmPortugues(r.resultado)}");
        }
        textoReceitas.text = sb.ToString();
    }

    private Sprite GetIconeIngrediente(IngredientKind tipo)
    {
        foreach (var entry in iconesIngredientes)
        {
            if (entry.tipo == tipo) return entry.icone;
        }
        return null;
    }

    private Sprite GetIconePocao(PotionType tipo)
    {
        foreach (var entry in iconesPocoes)
        {
            if (entry.tipo == tipo) return entry.icone;
        }
        return null;
    }
}
