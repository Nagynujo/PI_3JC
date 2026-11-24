using System.IO;
using UnityEditor;
using UnityEngine;

namespace StylizedFire.EditorTools
{
    public static class FireMenu
    {
        [MenuItem("GameObject/Effects/Stylized Fire", false, 10)]
        [MenuItem("Tools/Stylized Fire/Create Fire Object")]
        public static void CreateFire()
        {
            var mat = GetOrCreateMaterial();
            if (mat == null) return;

            var go = new GameObject("Stylized Fire");
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            go.AddComponent<FireRing>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;

            Undo.RegisterCreatedObjectUndo(go, "Create Stylized Fire");
            Selection.activeGameObject = go;
        }

        [MenuItem("Tools/Stylized Fire/Create Material Only")]
        public static void CreateMaterialOnly()
        {
            var m = GetOrCreateMaterial();
            if (m == null) return;
            Selection.activeObject = m;
            EditorGUIUtility.PingObject(m);
        }

        static Material GetOrCreateMaterial()
        {
            var shader = Shader.Find("Custom/StylizedFire");
            if (shader == null)
            {
                Debug.LogError("Shader Custom/StylizedFire nao encontrado. Verifique erros de compilacao no shader.");
                return null;
            }

            // Descobre a pasta onde o shader realmente esta (qualquer local do projeto)
            string shaderPath = AssetDatabase.GetAssetPath(shader);            // ex: Assets/Shaders/StylizedFire/Shaders/StylizedFire.shader
            string shaderDir = Path.GetDirectoryName(shaderPath).Replace('\\', '/');
            string rootDir = Path.GetDirectoryName(shaderDir).Replace('\\', '/'); // pasta acima de "Shaders"
            if (string.IsNullOrEmpty(rootDir)) rootDir = "Assets";

            string matDir = rootDir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir))
                AssetDatabase.CreateFolder(rootDir, "Materials");

            string matPath = matDir + "/StylizedFire.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) return mat;

            mat = new Material(shader) { name = "StylizedFire" };
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
