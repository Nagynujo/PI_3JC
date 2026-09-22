using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RecipeBookUI : MonoBehaviour
{
    [Header("Referências (arrastar na cena)")]
    [Tooltip("O Canvas/Painel do livro de receitas. Fica desativado até apertar a tecla.")]
    public GameObject painel;

    [Tooltip("Opcional - um texto único onde a lista de receitas é escrita automaticamente")]
    public TextMeshProUGUI textoReceitas;

    [Tooltip("O mesmo asset de receitas usado pelo Caldeirão")]
    public LivroDeReceitasSO livroDeReceitas;

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

        if (livroDeReceitas == null)
        {
            textoReceitas.text = "(nenhum Livro De Receitas configurado)";
            Debug.LogWarning("RecipeBookUI: nenhum 'Livro De Receitas' configurado. Arraste o asset LivroDeReceitasSO no Inspector.");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (ReceitaPocao r in livroDeReceitas.receitas)
        {
            sb.AppendLine($"{r.ingrediente1} + {r.ingrediente2}  →  {r.resultado}");
        }
        textoReceitas.text = sb.ToString();
    }
}
