using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class IngredientBase : MonoBehaviour
{
    
    public ProcessType tipoDeProcesso;

    public Mesh GetIngredientMesh()
    {
        return GetComponent<MeshFilter>().sharedMesh;
    }

    public Material GetIngredientMaterial()
    {
        return GetComponent<MeshRenderer>().sharedMaterial;
    }
}
