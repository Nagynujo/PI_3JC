using UnityEngine;

/// <summary>
/// Gera um cone translúcido (malha procedural, dupla face) que sai do olho em +Z.
/// Só apresentação: não detecta nada. Coloque como filho do olho.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ScannerConeVisual : MonoBehaviour
{
    [Tooltip("Material translúcido/unlit. Se vazio, usa Sprites/Default (aceita cor de vértice).")]
    [SerializeField] private Material material;
    [SerializeField, Range(8, 64)] private int segments = 32;
    [SerializeField] private Color color = new Color(1f, 0.9f, 0.5f, 0.22f);

    private Mesh mesh;

    public void Rebuild(float range, float angle)
    {
        if (mesh == null) { mesh = new Mesh { name = "ScannerCone" }; GetComponent<MeshFilter>().sharedMesh = mesh; }
        var rend = GetComponent<MeshRenderer>();
        if (rend.sharedMaterial == null)
        {
            if (material == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh == null) { Debug.LogWarning("[ScannerConeVisual] Atribua um material.", this); return; }
                material = new Material(sh);
            }
            rend.sharedMaterial = material;
        }
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        float radius = range * Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad);
        var verts = new Vector3[segments + 1];
        var cols = new Color[segments + 1];
        verts[0] = Vector3.zero;
        cols[0] = color;
        Color edge = new Color(color.r, color.g, color.b, color.a * 0.2f); // some perto da borda
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, range);
            cols[i + 1] = edge;
        }
        var tris = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int a = i + 1, b = (i + 1) % segments + 1;
            tris[i * 6 + 0] = 0; tris[i * 6 + 1] = a; tris[i * 6 + 2] = b;
            tris[i * 6 + 3] = 0; tris[i * 6 + 4] = b; tris[i * 6 + 5] = a; // lado de trás
        }
        mesh.Clear();
        mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
        mesh.RecalculateBounds();
    }
}
