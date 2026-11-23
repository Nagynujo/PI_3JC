using System;
using UnityEngine;

/// <summary>
/// Slider global de tamanho dos ingredientes.
/// Coloque em um objeto da cena (por exemplo o GameManager) e mexa no slider "Global Scale" no Inspector.
/// </summary>
public class IngredientScaleSettings : MonoBehaviour
{
    public static IngredientScaleSettings Instance { get; private set; }
    public static event Action OnScaleChanged;

    [Range(0.1f, 3f)]
    [SerializeField] private float globalScale = 1f;

    public float GlobalScale => globalScale;

    private void Awake()
    {
        Instance = this;
    }

    // Roda sempre que você mexe no slider no Inspector (inclusive durante o Play)
    private void OnValidate()
    {
        OnScaleChanged?.Invoke();
    }
}
