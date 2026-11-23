using UnityEngine;

/// <summary>
/// Mantém o tamanho do ingrediente no MUNDO sempre igual, independente do scale do pai
/// (mesa de trabalho, caldeirão etc.). Coloque em cada ingrediente ou no prefab dele.
/// </summary>
public class IngredientScale : MonoBehaviour
{
    [Tooltip("Tamanho base deste ingrediente no mundo (1 = tamanho normal).")]
    [SerializeField] private float baseWorldScale = 1f;

    private void OnEnable()
    {
        IngredientScaleSettings.OnScaleChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        IngredientScaleSettings.OnScaleChanged -= Apply;
    }

    private void Start()
    {
        Apply();   // garante que o slider global já existe
    }

    // Chamado automaticamente quando o ingrediente troca de pai (mesa -> caldeirão etc.)
    private void OnTransformParentChanged()
    {
        Apply();
    }

    public void Apply()
    {
        float global = IngredientScaleSettings.Instance != null
            ? IngredientScaleSettings.Instance.GlobalScale
            : 1f;

        float target = baseWorldScale * global;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

        transform.localScale = new Vector3(
            target / Mathf.Max(parentScale.x, 0.0001f),
            target / Mathf.Max(parentScale.y, 0.0001f),
            target / Mathf.Max(parentScale.z, 0.0001f));
    }
}
