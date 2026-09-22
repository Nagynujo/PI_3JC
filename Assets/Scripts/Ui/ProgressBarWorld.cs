using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de progresso posicionada no mundo (world-space Canvas), acima de uma
/// estação. Funciona com qualquer script que implemente IProgressAction —
/// Caldeirao, StationBase (cortar/moer), ou qualquer estação nova que você
/// criar depois com duração/progresso.
/// </summary>
public class ProgressBarWorld : MonoBehaviour
{
    [Header("Fonte do progresso")]
    [Tooltip("Arraste aqui o script da estação (Caldeirao, StationBase, etc) — precisa implementar IProgressAction")]
    public MonoBehaviour fonteDeProgresso;

    [Header("Visual")]
    [Tooltip("Objeto pai que contém a barra (geralmente o Canvas World Space). É ativado/desativado inteiro.")]
    public GameObject raiz;
    [Tooltip("Image com Image Type = Filled, Fill Method = Horizontal")]
    public Image barraPreenchimento;

    [Header("Comportamento")]
    [Tooltip("Gira a barra pra sempre ficar de frente pra câmera")]
    public bool sempreDeFrenteParaCamera = true;

    private IProgressAction progresso;
    private Camera cameraPrincipal;

    void Awake()
    {
        progresso = fonteDeProgresso as IProgressAction;

        if (progresso == null)
        {
            Debug.LogWarning($"{gameObject.name}: o objeto arrastado em 'Fonte De Progresso' não implementa IProgressAction. A barra não vai funcionar.");
        }

        cameraPrincipal = Camera.main;

        if (raiz != null) raiz.SetActive(false);
    }

    void LateUpdate()
    {
        if (progresso == null || raiz == null) return;

        bool ativo = progresso.EstaEmAndamento;

        if (raiz.activeSelf != ativo)
            raiz.SetActive(ativo);

        if (!ativo) return;

        if (barraPreenchimento != null)
            barraPreenchimento.fillAmount = progresso.Progresso01;

        if (sempreDeFrenteParaCamera && cameraPrincipal != null)
        {
            transform.forward = transform.position - cameraPrincipal.transform.position;
        }
    }
}
