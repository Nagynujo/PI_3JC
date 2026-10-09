using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fonte única das receitas de poção. Crie UM asset disso (Assets > Create >
/// Poções > Livro de Receitas) e arraste o MESMO asset no Caldeirão e no
/// RecipeBookUI — assim as duas telas sempre concordam, e mudar uma receita
/// é editar um lugar só.
/// </summary>
[CreateAssetMenu(fileName = "LivroDeReceitas", menuName = "Poções/Livro de Receitas")]
public class LivroDeReceitasSO : ScriptableObject
{
    public List<ReceitaPocao> receitas = new List<ReceitaPocao>();

    /// <summary>
    /// Procura uma receita pra essa dupla de ingredientes (em qualquer ordem).
    /// Retorna PotionType.None se não achar combinação.
    /// </summary>
    public PotionType Combinar(IngredientKind a, IngredientKind b)
    {
        foreach (ReceitaPocao r in receitas)
        {
            bool bate = (r.ingrediente1 == a && r.ingrediente2 == b) ||
                        (r.ingrediente1 == b && r.ingrediente2 == a);
            if (bate) return r.resultado;
        }
        return PotionType.None;
    }
}
