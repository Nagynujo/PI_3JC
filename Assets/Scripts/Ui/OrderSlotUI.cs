using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderSlotUI : MonoBehaviour
{
    [Header("Referências (arrastar no prefab do slot)")]
    public Image iconePotion;

    [Tooltip("Image com Image Type = Filled, Fill Method = Horizontal (ou Radial 360)")]
    public Image barraTempo;

    [Tooltip("Opcional - número de segundos restantes")]
    public TextMeshProUGUI textoTempo;

    public void Configurar(Sprite icone)
    {
        if (iconePotion != null)
            iconePotion.sprite = icone;
    }

    public void AtualizarProgresso(float progresso01, float tempoRestante)
    {
        if (barraTempo != null)
            barraTempo.fillAmount = progresso01;

        if (textoTempo != null)
            textoTempo.text = Mathf.CeilToInt(Mathf.Max(0f, tempoRestante)).ToString();
    }
}
