using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderSlotUI : MonoBehaviour
{
    [Header("Referências (arrastar no prefab do slot)")]
    public Image iconePotion;

    [Tooltip("Texto no topo do card com o nome da poção pedida (ex: 'Fogo')")]
    public TextMeshProUGUI textoNomePocao;

    [Tooltip("Image com Image Type = Filled, Fill Method = Horizontal (ou Radial 360)")]
    public Image barraTempo;

    [Tooltip("Opcional - número de segundos restantes")]
    public TextMeshProUGUI textoTempo;

    [Tooltip("Opcional - um contorno/destaque mostrado só no pedido mais urgente (o mais antigo)")]
    public GameObject destaqueBorda;

    public void Configurar(Sprite icone, string nomePocao)
    {
        if (iconePotion != null)
            iconePotion.sprite = icone;

        if (textoNomePocao != null)
            textoNomePocao.text = nomePocao;
    }

    public void AtualizarProgresso(float progresso01, float tempoRestante)
    {
        if (barraTempo != null)
            barraTempo.fillAmount = progresso01;

        if (textoTempo != null)
            textoTempo.text = Mathf.CeilToInt(Mathf.Max(0f, tempoRestante)).ToString();
    }

    /// <summary>
    /// Todos os pedidos na fileira mostram ícone/nome/tempo normalmente — isso só
    /// liga um destaque visual (se configurado) no pedido mais urgente (o mais antigo).
    /// </summary>
    public void DefinirComoUrgente(bool urgente)
    {
        if (destaqueBorda != null) destaqueBorda.SetActive(urgente);
    }
}
