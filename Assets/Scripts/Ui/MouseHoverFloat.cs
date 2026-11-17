using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHoverFloat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configurações")]
    [SerializeField] private float alturaElevacao = 15f;
    [SerializeField] private float velocidade = 10f;

    private RectTransform rectTransform;
    private Vector2 posicaoOriginal;
    private Vector2 posicaoAlvo;
    private bool inicializado = false;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            posicaoOriginal = rectTransform.anchoredPosition;
            posicaoAlvo = posicaoOriginal;
            inicializado = true;
        }
    }

    private void Update()
    {
        if (!inicializado) return;

        // Usa unscaledDeltaTime para a animação funcionar MESMO no Menu de Pause (com Time.timeScale = 0)
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            posicaoAlvo,
            Time.unscaledDeltaTime * velocidade
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!inicializado) return;
        posicaoAlvo = posicaoOriginal + new Vector2(0f, alturaElevacao);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!inicializado) return;
        posicaoAlvo = posicaoOriginal;
    }

    private void OnDisable()
    {
        if (rectTransform != null && inicializado)
        {
            rectTransform.anchoredPosition = posicaoOriginal;
            posicaoAlvo = posicaoOriginal;
        }
    }
}