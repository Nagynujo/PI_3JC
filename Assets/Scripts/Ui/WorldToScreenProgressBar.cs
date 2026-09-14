using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de progresso que vive dentro do seu Canvas normal (Screen Space - Overlay),
/// em vez de precisar de um Canvas World Space por estação. Ela se posiciona
/// sozinha na tela, acompanhando a posição do objeto 3D (Caldeirao, StationBase, etc).
///
/// Uso: arraste esse prefab pra dentro do seu Canvas existente, uma vez por
/// estação que precisa de barra. Não precisa de nenhum Canvas extra.
///
/// Só funciona direto se o Canvas for "Screen Space - Overlay". Se for
/// "Screen Space - Camera", troque a linha marcada abaixo (comentário no código).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class WorldToScreenProgressBar : MonoBehaviour
{
    [Header("Alvo no mundo")]
    [Tooltip("O objeto da estação (o que tem o Caldeirao, StationBase, etc)")]
    public Transform alvoNoMundo;
    [Tooltip("Deslocamento acima do objeto onde a barra deve aparecer")]
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);

    [Header("Fonte do progresso")]
    [Tooltip("Arraste o mesmo script do alvo (precisa implementar IProgressAction)")]
    public MonoBehaviour fonteDeProgresso;

    [Header("Câmera (opcional)")]
    [Tooltip("Deixe vazio pra usar a Camera.main (a câmera com a tag 'MainCamera'). Só preencha se sua câmera não tiver essa tag.")]
    public Camera cameraManual;

    [Header("Visual")]
    [Tooltip("Objeto que contém a barra (é ativado/desativado)")]
    public GameObject raiz;
    [Tooltip("Image com Image Type = Filled, Fill Method = Horizontal")]
    public Image barraPreenchimento;

    private IProgressAction progresso;
    private RectTransform rectTransform;
    private Camera cameraPrincipal;
    private bool avisoJaMostrado;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        progresso = fonteDeProgresso as IProgressAction;
        cameraPrincipal = cameraManual != null ? cameraManual : Camera.main;

        if (raiz == gameObject)
        {
            Debug.LogError($"{gameObject.name}: o campo 'Raiz' não pode ser o mesmo objeto que tem esse script! " +
                "Se Raiz for desativado, esse script (que fica no mesmo objeto) para de rodar e nunca mais se reativa. " +
                "Crie um objeto pai separado só pra guardar o script, e deixe 'Raiz' apontar pro objeto filho com o visual.");
        }

        if (raiz != null) raiz.SetActive(false);
    }

    void LateUpdate()
    {
        // Tenta achar a câmera de novo caso ela não estivesse pronta no Awake
        if (cameraPrincipal == null)
            cameraPrincipal = cameraManual != null ? cameraManual : Camera.main;

        if (progresso == null || alvoNoMundo == null || raiz == null || cameraPrincipal == null)
        {
            if (!avisoJaMostrado)
            {
                avisoJaMostrado = true;
                if (progresso == null) Debug.LogWarning($"{gameObject.name}: 'Fonte De Progresso' está vazio ou não implementa IProgressAction.");
                if (alvoNoMundo == null) Debug.LogWarning($"{gameObject.name}: 'Alvo No Mundo' não foi preenchido.");
                if (raiz == null) Debug.LogWarning($"{gameObject.name}: 'Raiz' não foi preenchido.");
                if (cameraPrincipal == null) Debug.LogWarning($"{gameObject.name}: não achei nenhuma câmera com a tag 'MainCamera'. Preenche o campo 'Camera Manual' com sua câmera, ou marca a tag dela como 'MainCamera'.");
            }
            return;
        }

        bool ativo = progresso.EstaEmAndamento;

        if (raiz.activeSelf != ativo)
            raiz.SetActive(ativo);

        if (!ativo) return;

        if (barraPreenchimento != null)
            barraPreenchimento.fillAmount = progresso.Progresso01;

        // Se o Canvas for "Screen Space - Overlay", isso já basta:
        Vector3 posicaoNaTela = cameraPrincipal.WorldToScreenPoint(alvoNoMundo.position + offset);
        rectTransform.position = posicaoNaTela;

        // Se o Canvas for "Screen Space - Camera" em vez de "Overlay", troque a linha
        // acima por isto (e arraste o RectTransform do Canvas no campo canvasRect):
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, posicaoNaTela, cameraPrincipal, out Vector2 local);
        // rectTransform.localPosition = local;
    }
}
