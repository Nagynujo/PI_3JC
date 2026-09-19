using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RecipeBookUI : MonoBehaviour
{
    [System.Serializable]
    public class Receita
    {
        public IngredientKind ingrediente1;
        public IngredientKind ingrediente2;
        public PotionType resultado;
    }

    [Header("Referências (arrastar na cena)")]
    [Tooltip("O Canvas/Painel do livro de receitas. Fica desativado até apertar a tecla.")]
    public GameObject painel;

    [Tooltip("Opcional - um texto único onde a lista de receitas é escrita automaticamente")]
    public TextMeshProUGUI textoReceitas;

    [Header("Receitas conhecidas (mesma lógica do Caldeirão)")]
    public List<Receita> receitas = new List<Receita>
    {
        new Receita { ingrediente1 = IngredientKind.Camomila, ingrediente2 = IngredientKind.EscamaDeDragao, resultado = PotionType.Fire },
        new Receita { ingrediente1 = IngredientKind.Camomila, ingrediente2 = IngredientKind.OlhoDeSalamandra, resultado = PotionType.Acid },
        new Receita { ingrediente1 = IngredientKind.OlhoDeSalamandra, ingrediente2 = IngredientKind.EscamaDeDragao, resultado = PotionType.Poison },
        new Receita { ingrediente1 = IngredientKind.MilhoDePipoca, ingrediente2 = IngredientKind.Camomila, resultado = PotionType.Invisibility },
        new Receita { ingrediente1 = IngredientKind.MilhoDePipoca, ingrediente2 = IngredientKind.EscamaDeDragao, resultado = PotionType.Cure },
        new Receita { ingrediente1 = IngredientKind.MilhoDePipoca, ingrediente2 = IngredientKind.OlhoDeSalamandra, resultado = PotionType.Fast },
    };

    private InputAction toggleAction;
    private bool aberto;

    void Awake()
    {
        toggleAction = new InputAction("AbrirLivroDeReceitas", InputActionType.Button, binding: "<Keyboard>/tab");
        toggleAction.AddBinding("<Gamepad>/select");

        if (painel != null) painel.SetActive(false);
    }

    void OnEnable()
    {
        toggleAction.Enable();
        MontarTexto();
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

    private void MontarTexto()
    {
        if (textoReceitas == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (Receita r in receitas)
        {
            sb.AppendLine($"{r.ingrediente1} + {r.ingrediente2}  →  {r.resultado}");
        }
        textoReceitas.text = sb.ToString();
    }
}
