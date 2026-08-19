using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class IngredientBase : MonoBehaviour
{
    [Header("Se for um ingrediente cru (preencher isso)")]
    public IngredientKind kind;
    public ProcessType tipoDeProcesso;

    [Header("Visual quando processado (opcional)")]
    public Mesh meshProcessado;
    public Material materialProcessado;

    [Header("Se for uma poção (preencher isso, ignorar o resto)")]
    public PotionType potionType = PotionType.None;
    public bool estaEngarrafada;

    public bool EstaProcessado { get; private set; }

    public Mesh GetIngredientMesh()
    {
        return GetComponent<MeshFilter>().sharedMesh;
    }

    public Material GetIngredientMaterial()
    {
        return GetComponent<MeshRenderer>().sharedMaterial;
    }

    public void MarcarComoProcessado()
    {
        EstaProcessado = true;

        if (meshProcessado != null)
            GetComponent<MeshFilter>().sharedMesh = meshProcessado;

        if (materialProcessado != null)
            GetComponent<MeshRenderer>().sharedMaterial = materialProcessado;
    }

    public void MarcarComoEngarrafada()
    {
        estaEngarrafada = true;
        // se quiser trocar o visual pra "com rolha", dá pra usar os mesmos
        // campos meshProcessado/materialProcessado, ou adicionar novos campos
        // específicos depois.
    }
}