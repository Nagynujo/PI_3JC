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

    [Header("Se for uma garrafa vazia (preencher isso, ignorar o resto)")]
    public bool ehGarrafaVazia;

    [Header("Poção (preenchido automaticamente ao engarrafar no Caldeirão)")]
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

    public void PreencherComoPotion(PotionType tipo, Mesh mesh, Material material)
    {
        potionType = tipo;
        estaEngarrafada = true;

        if (mesh != null)
            GetComponent<MeshFilter>().sharedMesh = mesh;

        if (material != null)
            GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
