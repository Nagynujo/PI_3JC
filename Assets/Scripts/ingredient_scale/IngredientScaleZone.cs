using UnityEngine;

// =====================================================================================
// [NOVO ARQUIVO] IngredientScaleZone
// -------------------------------------------------------------------------------------
// POR QUE EXISTE:
//   Antes só existia um slider GLOBAL (IngredientScaleSettings), então não dava pra ter
//   o ingrediente num tamanho na mesa de trabalho e em outro no caldeirão.
//
// COMO USAR:
//   Adicione este componente na MESA DE TRABALHO (cada estação) e no CALDEIRÃO.
//   Qualquer IngredientScale que seja filho desse objeto passa a usar o "multiplicador"
//   daqui. Quando o jogador pega o ingrediente de volta, o pai muda e o ingrediente
//   deixa de ter zona (multiplicador 1) automaticamente.
//
// FÓRMULA FINAL DO TAMANHO NO MUNDO:
//   baseWorldScale (no ingrediente) x GlobalScale (settings) x multiplicador (esta zona)
// =====================================================================================
public class IngredientScaleZone : MonoBehaviour
{
    [Tooltip("1 = tamanho normal. Ajuste por estação (ex.: 0.5 na mesa, 0.3 no caldeirão).")]
    [Range(0.05f, 5f)]
    [SerializeField] private float multiplicador = 1f;

    /// <summary>Lido pelo IngredientScale.Apply() pra calcular o tamanho final.</summary>
    public float Multiplicador => multiplicador;

    /// <summary>
    /// Roda quando você mexe no slider no Inspector (inclusive durante o Play).
    /// Reaplica a escala em todos os ingredientes que estão dentro desta zona,
    /// pra você ver o resultado ao vivo.
    /// </summary>
    private void OnValidate()
    {
        // Só em Play: evita mexer em prefabs/assets fora de execução.
        if (!Application.isPlaying) return;

        foreach (IngredientScale s in GetComponentsInChildren<IngredientScale>())
        {
            s.Apply();
        }
    }
}