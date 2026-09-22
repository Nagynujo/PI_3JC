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

    [Tooltip("Opcional - controla a opacidade da carta inteira quando ela está empilhada atrás")]
    public CanvasGroup canvasGroup;

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

    /// <summary>
    /// Só a carta da frente (o pedido mais antigo) mostra ícone/barra/tempo.
    /// As de trás na pilha ficam só com o fundo do card (o "verso"), sem detalhe.
    /// </summary>
    public void DefinirComoFrente(bool frente)
    {
        if (iconePotion != null) iconePotion.gameObject.SetActive(frente);
        if (barraTempo != null) barraTempo.gameObject.SetActive(frente);
        if (textoTempo != null) textoTempo.gameObject.SetActive(frente);
    }

    /// <summary>Opacidade da carta inteira (0 a 1) — cartas mais atrás na pilha ficam mais apagadas.</summary>
    public void DefinirOpacidade(float alfa)
    {
        if (canvasGroup != null) canvasGroup.alpha = alfa;
    }
}
