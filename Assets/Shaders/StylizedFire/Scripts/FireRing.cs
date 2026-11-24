using UnityEngine;

namespace StylizedFire
{
    /// <summary>
    /// Gera uma malha em anel (cilindro aberto) com UVs prontas para o shader Custom/StylizedFire
    /// (uv.x = volta ao redor 0..1, uv.y = altura 0..1) e envia ao material a proporcao
    /// largura/altura de cada lingua de fogo, para os ganchos nao ficarem esticados.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FireRing : MonoBehaviour
    {
        [Min(0.01f)] public float radius = 0.6f;
        [Min(0.01f)] public float height = 1.0f;
        [Range(16, 256)] public int segments = 96;
        [Range(2, 16)] public int heightSegments = 2;

        static readonly int CellAspectId = Shader.PropertyToID("_CellAspect");
        static readonly int TonguesId = Shader.PropertyToID("_Tongues");

        Mesh _mesh;
        MaterialPropertyBlock _mpb;

        void OnEnable() { Build(); UpdateAspect(); }
        void OnValidate() { Build(); UpdateAspect(); }
        void Update() { UpdateAspect(); }

        void UpdateAspect()
        {
            var mr = GetComponent<MeshRenderer>();
            var mat = mr != null ? mr.sharedMaterial : null;
            if (mat == null || !mat.HasProperty(TonguesId)) return;

            float tongues = Mathf.Max(1f, Mathf.Round(mat.GetFloat(TonguesId)));
            Vector3 s = transform.lossyScale;
            float worldRadius = radius * 0.5f * (Mathf.Abs(s.x) + Mathf.Abs(s.z));
            float worldHeight = Mathf.Max(0.0001f, height * Mathf.Abs(s.y));
            float aspect = (2f * Mathf.PI * worldRadius / tongues) / worldHeight;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(CellAspectId, aspect);
            mr.SetPropertyBlock(_mpb);
        }

        void Build()
        {
            var mf = GetComponent<MeshFilter>();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "FireRingMesh", hideFlags = HideFlags.HideAndDontSave };
            }
            _mesh.Clear();

            int cols = segments + 1;
            int rows = heightSegments + 1;
            var verts = new Vector3[cols * rows];
            var uvs = new Vector2[cols * rows];
            var tris = new int[segments * heightSegments * 6];

            for (int y = 0; y < rows; y++)
            {
                float v = y / (float)heightSegments;
                for (int x = 0; x < cols; x++)
                {
                    float u = x / (float)segments;
                    float a = u * Mathf.PI * 2f;
                    int i = y * cols + x;
                    verts[i] = new Vector3(Mathf.Cos(a) * radius, v * height, Mathf.Sin(a) * radius);
                    uvs[i] = new Vector2(u, v);
                }
            }

            int t = 0;
            for (int y = 0; y < heightSegments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int i = y * cols + x;
                    tris[t++] = i; tris[t++] = i + cols; tris[t++] = i + 1;
                    tris[t++] = i + 1; tris[t++] = i + cols; tris[t++] = i + cols + 1;
                }
            }

            _mesh.vertices = verts;
            _mesh.uv = uvs;
            _mesh.triangles = tris;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            mf.sharedMesh = _mesh;
        }

        void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh);
            }
        }
    }
}
