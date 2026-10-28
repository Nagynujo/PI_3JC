using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Uma linha do livro de receitas: ícone do ingrediente 1 + ícone do
/// ingrediente 2 = ícone da poção resultado (com o nome embaixo/do lado).
/// </summary>
public class RecipeRowUI : MonoBehaviour
{
    [Header("Referências (arrastar no prefab da linha)")]
    public Image iconeIngrediente1;
    public Image iconeIngrediente2;
    public Image iconeResultado;

    [Tooltip("Opcional - nome da poção resultado, ex: 'Fogo'")]
    public TextMeshProUGUI textoNomeResultado;

    public void Configurar(Sprite icone1, Sprite icone2, Sprite iconeResultadoSprite, string nomeResultado)
    {
        if (iconeIngrediente1 != null) iconeIngrediente1.sprite = icone1;
        if (iconeIngrediente2 != null) iconeIngrediente2.sprite = icone2;
        if (iconeResultado != null) iconeResultado.sprite = iconeResultadoSprite;
        if (textoNomeResultado != null) textoNomeResultado.text = nomeResultado;
    }
}
