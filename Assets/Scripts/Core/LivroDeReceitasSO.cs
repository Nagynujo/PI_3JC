using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "LivroDeReceitas", menuName = "Poções/Livro de Receitas")]
public class LivroDeReceitasSO : ScriptableObject
{
    public List<ReceitaPocao> receitas = new List<ReceitaPocao>();

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
