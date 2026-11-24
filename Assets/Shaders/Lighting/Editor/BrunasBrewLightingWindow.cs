using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using BrunasBrew.Lighting;

namespace BrunasBrew.Lighting.Editor
{
    /// <summary>
    /// Ferramenta de Editor para instalar e desfazer uma iluminacao estilizada na cena.
    /// Compativel com URP e Built-in, sem dependencias de pacotes extras.
    /// Nenhum Update/animacao em runtime.
    /// </summary>
    public sealed class BrunasBrewLightingWindow : EditorWindow
    {
        private const string RigName = "BRUNAS_BREW_LIGHTING";

        private enum VisualStyle { Aconchegante, Magico, Equilibrado }

        private VisualStyle style = VisualStyle.Aconchegante;
        private Transform roomCenter;
        private Transform cauldron1;
        private Transform cauldron2;
        private float cauldronGlow = 1f;
        private bool disablePreviousLights = true;
        private bool mainLightShadows = true;
        private float brightness = 1f;

        [MenuItem("Brunas Brew/Iluminacao/Configurar iluminacao")]
        public static void Open()
        {
            GetWindow<BrunasBrewLightingWindow>("Iluminacao - Brunas Brew");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            GUILayout.Label("BRUNA'S BREW | ILUMINACAO", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cria 3 luzes Spot e 2 luzes Point (uma para cada caldeirao). A iluminacao ambiente " +
                "e configurada automaticamente. Nao modifica shaders ou materiais.", MessageType.Info);

            style = (VisualStyle)EditorGUILayout.EnumPopup("Estilo", style);
            brightness = EditorGUILayout.Slider("Brilho das luzes", brightness, 0.35f, 2.0f);
            roomCenter = (Transform)EditorGUILayout.ObjectField("Centro da loja (opcional)", roomCenter, typeof(Transform), true);
            cauldron1 = (Transform)EditorGUILayout.ObjectField("Caldeirao 1", cauldron1, typeof(Transform), true);
            cauldron2 = (Transform)EditorGUILayout.ObjectField("Caldeirao 2", cauldron2, typeof(Transform), true);
            cauldronGlow = EditorGUILayout.Slider("Brilho dos caldeiroes", cauldronGlow, 0f, 2f);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Criar / selecionar marcador do centro da loja"))
                    CreateOrSelectRoomCenter();
            }
            disablePreviousLights = EditorGUILayout.ToggleLeft("Desativar luzes antigas (reversivel)", disablePreviousLights);
            mainLightShadows = EditorGUILayout.ToggleLeft("Sombra suave apenas na luz principal", mainLightShadows);

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Centro da loja: GameObject vazio, fixo, no meio do piso. Voce pode criar um pelo botao acima " +
                "e reposiciona-lo na Hierarchy. Se deixar os caldeiroes em branco, a ferramenta tenta " +
                "encontrar ate dois objetos com 'caldeir' ou 'cauldron' no nome. " +
                "Se a busca falhar, arraste cada caldeirao manualmente.", MessageType.None);

            if (GraphicsSettings.currentRenderPipeline != null)
            {
                EditorGUILayout.HelpBox(
                    "Pipeline SRP detectado. Se as luzes Spot/Point nao aparecerem em URP, " +
                    "verifique o URP Asset > Lighting > Additional Lights (Per Pixel).", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("APLICAR NA CENA ABERTA", GUILayout.Height(34))) Apply();
                if (GUILayout.Button("Restaurar iluminacao anterior", GUILayout.Height(25))) Restore();
            }
            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Pare o Play Mode antes de configurar.", MessageType.Warning);
        }

        private struct Colors
        {
            public Color sky, equator, ground;
            public Color key, fill, rim, potion;
            public float keyPower, fillPower, rimPower, potionPower;
        }

        private static Color Hex(string value)
        {
            Color result;
            return ColorUtility.TryParseHtmlString(value, out result) ? result : Color.white;
        }

        private Colors GetColors()
        {
            switch (style)
            {
                case VisualStyle.Magico:
                    return new Colors
                    {
                        sky = Hex("#9A91C3"), equator = Hex("#756C9C"), ground = Hex("#48425C"),
                        key = Hex("#FFD6AD"), fill = Hex("#9CA7FF"),
                        rim = Hex("#E3A0FF"), potion = Hex("#FF74C8"),
                        keyPower = 3.4f, fillPower = 2.5f, rimPower = 3.0f, potionPower = 2.5f
                    };
                case VisualStyle.Equilibrado:
                    return new Colors
                    {
                        sky = Hex("#BCB6BD"), equator = Hex("#97919B"), ground = Hex("#605863"),
                        key = Hex("#FFE7C9"), fill = Hex("#B7C4F7"),
                        rim = Hex("#D8B5F0"), potion = Hex("#F58CB9"),
                        keyPower = 3.0f, fillPower = 1.7f, rimPower = 1.9f, potionPower = 1.6f
                    };
                default:
                    return new Colors
                    {
                        sky = Hex("#A49AAF"), equator = Hex("#817786"), ground = Hex("#504755"),
                        key = Hex("#FFE0B1"), fill = Hex("#ACB7F4"),
                        rim = Hex("#DDB0EF"), potion = Hex("#FF81BD"),
                        keyPower = 3.8f, fillPower = 2.0f, rimPower = 2.3f, potionPower = 1.8f
                    };
            }
        }

        private static GameObject FindRig(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == RigName) return root;
            return null;
        }

        private static Bounds CalculateBounds(Scene scene)
        {
            Bounds result = new Bounds(Vector3.zero, new Vector3(10f, 5f, 10f));
            bool found = false;
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.scene != scene) continue;
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
                if (renderer.transform.root.name == RigName) continue;
                Bounds b = renderer.bounds;
                if (b.size.sqrMagnitude < 0.001f || b.size.magnitude > 100f) continue;
                if (!found) { result = b; found = true; }
                else result.Encapsulate(b);
            }
            return result;
        }

        /// <summary>
        /// Localiza os caldeiroes sem confundir as partes de um mesmo prefab com
        /// caldeiroes diferentes. O preenchimento manual dos campos tem prioridade.
        /// </summary>
        private static List<Transform> FindCauldrons(Scene scene)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            HashSet<Transform> named = new HashSet<Transform>();
            foreach (Transform t in transforms)
            {
                if (t.gameObject.scene != scene || t.root.name == RigName) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("caldeir") || n.Contains("cauldron") || n.Contains("caldron"))
                    named.Add(t);
            }

            // Prefab com subobjetos chamados CauldronWater/CauldronBase:
            // normalmente a raiz representa um unico caldeirao.
            List<Transform> top = new List<Transform>();
            foreach (Transform t in named)
            {
                bool hasNamedParent = false;
                for (Transform p = t.parent; p != null; p = p.parent)
                {
                    if (named.Contains(p)) { hasNamedParent = true; break; }
                }
                if (!hasNamedParent) top.Add(t);
            }

            List<Transform> result = new List<Transform>();
            foreach (Transform root in top)
            {
                // Excecao: uma pasta de Hierarchy "Cauldrons" pode guardar
                // dois prefabs; nesse caso usamos os dois filhos, nao a pasta.
                List<Transform> branches = new List<Transform>();
                foreach (Transform candidate in named)
                {
                    if (candidate == root || !candidate.IsChildOf(root)) continue;
                    bool otherNamedBetween = false;
                    for (Transform p = candidate.parent; p != root && p != null; p = p.parent)
                    {
                        if (named.Contains(p)) { otherNamedBetween = true; break; }
                    }
                    if (!otherNamedBetween) branches.Add(candidate);
                }
                if (branches.Count >= 2) result.AddRange(branches);
                else result.Add(root);
            }

            result.Sort((a, b) => string.CompareOrdinal(GetTransformPath(a), GetTransformPath(b)));
            return result;
        }

        private static string GetTransformPath(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }

        private static bool IsValidSceneTarget(Transform target, Scene scene)
        {
            return target != null && target.gameObject.scene == scene && target.root.name != RigName;
        }

        private void CreateOrSelectRoomCenter()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            const string markerName = "BRUNAS_BREW_CENTRO_LOJA";
            GameObject marker = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == markerName) { marker = root; break; }

            if (marker == null)
            {
                Bounds bounds = CalculateBounds(scene);
                marker = new GameObject(markerName);
                SceneManager.MoveGameObjectToScene(marker, scene);
                Undo.RegisterCreatedObjectUndo(marker, "Criar centro da loja");
                marker.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.6f, bounds.center.z);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            roomCenter = marker.transform;
            Selection.activeGameObject = marker;
            EditorGUIUtility.PingObject(marker);
        }

        private static Vector3 VisualCenter(Transform t)
        {
            // Aproximacao da superficie superior do caldeirao a partir de seus bounds.
            Renderer[] parts = t.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(t.position, Vector3.one);
            foreach (Renderer part in parts)
            {
                if (part is ParticleSystemRenderer || part is TrailRenderer) continue;
                if (!hasBounds) { bounds = part.bounds; hasBounds = true; }
                else bounds.Encapsulate(part.bounds);
            }
            if (hasBounds)
                return new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.55f, bounds.center.z);
            return t.position + Vector3.up * 0.75f;
        }

        private static Light CreateOrReuse(GameObject rig, string name)
        {
            Transform child = rig.transform.Find(name);
            GameObject go;
            if (child != null) go = child.gameObject;
            else
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Criar luz Brunas Brew");
                go.transform.SetParent(rig.transform, false);
            }
            Light light = go.GetComponent<Light>();
            if (light == null) light = Undo.AddComponent<Light>(go);
            Undo.RecordObject(light, "Configurar luz Brunas Brew");
            Undo.RecordObject(go.transform, "Posicionar luz Brunas Brew");
            go.SetActive(true);
            light.enabled = true;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            light.bounceIntensity = 1f;
            return light;
        }

        private static void SetSpot(Light light, Vector3 position, Vector3 target, float range,
                                    float angle, Color tint, float power, bool shadows)
        {
            light.type = LightType.Spot;
            light.transform.position = position;
            Vector3 direction = target - position;
            if (direction.sqrMagnitude > 0.001f)
                light.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            light.range = range;
            light.spotAngle = angle;
            light.color = tint;
            light.intensity = power;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.55f;
        }


        private static void ConfigurePotLight(Light light, Transform pot, Vector3 center,
                                              float width, float depth, Color tint, float intensity)
        {
            light.type = LightType.Point;
            // Um alcance pequeno evita que as duas luzes superiluminem a loja inteira.
            light.range = Mathf.Clamp(Mathf.Min(width, depth) * 0.30f, 1.8f, 3.5f);
            light.color = tint;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.identity;
            light.transform.position = pot != null ? VisualCenter(pot) : center + Vector3.up * 0.5f;
            light.enabled = pot != null && intensity > 0f;
        }

        private static void CaptureBeforeChanges(BrunasBrewLightingRigData data, Scene scene)
        {
            if (data.hasBackup) return;
            data.originalAmbientMode = RenderSettings.ambientMode;
            data.originalSky = RenderSettings.ambientSkyColor;
            data.originalEquator = RenderSettings.ambientEquatorColor;
            data.originalGround = RenderSettings.ambientGroundColor;
            data.originalAmbientIntensity = RenderSettings.ambientIntensity;
            data.originalReflectionIntensity = RenderSettings.reflectionIntensity;
            data.originalLights.Clear();
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.gameObject.scene != scene || light.transform.root == data.transform) continue;
                data.originalLights.Add(new BrunasBrewLightingRigData.OriginalLight
                {
                    source = light,
                    wasEnabled = light.enabled
                });
            }
            data.hasBackup = true;
        }

        private void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (IsValidSceneTarget(cauldron1, scene) && cauldron1 == cauldron2)
            {
                EditorUtility.DisplayDialog("Bruna's Brew", "Selecione dois caldeiroes diferentes ou deixe um campo em branco.", "OK");
                return;
            }

            GameObject rig = FindRig(scene);
            if (rig == null)
            {
                rig = new GameObject(RigName);
                SceneManager.MoveGameObjectToScene(rig, scene);
                Undo.RegisterCreatedObjectUndo(rig, "Criar iluminacao Brunas Brew");
            }

            BrunasBrewLightingRigData data = rig.GetComponent<BrunasBrewLightingRigData>();
            if (data == null) data = Undo.AddComponent<BrunasBrewLightingRigData>(rig);
            Undo.RecordObject(data, "Salvar backup de iluminacao");
            CaptureBeforeChanges(data, scene);

            foreach (var entry in data.originalLights)
            {
                if (entry.source == null) continue;
                Undo.RecordObject(entry.source, "Ajustar luzes anteriores");
                entry.source.enabled = disablePreviousLights ? false : entry.wasEnabled;
            }

            Colors colors = GetColors();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = colors.sky;
            RenderSettings.ambientEquatorColor = colors.equator;
            RenderSettings.ambientGroundColor = colors.ground;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.65f;

            Bounds bounds = CalculateBounds(scene);
            float width = Mathf.Clamp(bounds.size.x, 5f, 25f);
            float depth = Mathf.Clamp(bounds.size.z, 5f, 25f);
            float height = Mathf.Clamp(bounds.size.y * 0.75f, 3f, 7f);
            Vector3 center = roomCenter != null && roomCenter.gameObject.scene == scene
                ? roomCenter.position : new Vector3(bounds.center.x, bounds.min.y + 0.6f, bounds.center.z);
            Vector3 above = center + Vector3.up * height;
            float range = Mathf.Clamp(Mathf.Sqrt(width * width + depth * depth) * 1.05f, 8f, 34f);

            SetSpot(CreateOrReuse(rig, "01 - Principal quente"),
                above + new Vector3(-width * 0.22f, 0f, -depth * 0.15f), center,
                range, 95f, colors.key, colors.keyPower * brightness, mainLightShadows);

            SetSpot(CreateOrReuse(rig, "02 - Preenchimento azul"),
                above + new Vector3(width * 0.27f, -height * 0.10f, -depth * 0.07f), center,
                range * 0.9f, 105f, colors.fill, colors.fillPower * brightness, false);

            SetSpot(CreateOrReuse(rig, "03 - Recorte lilas"),
                above + new Vector3(width * 0.10f, -height * 0.20f, depth * 0.27f), center,
                range * 0.8f, 105f, colors.rim, colors.rimPower * brightness, false);

            List<Transform> detectedPots = FindCauldrons(scene);
            Transform explicitPot1 = IsValidSceneTarget(cauldron1, scene) ? cauldron1 : null;
            Transform explicitPot2 = IsValidSceneTarget(cauldron2, scene) ? cauldron2 : null;
            Transform targetPot1 = explicitPot1;
            Transform targetPot2 = explicitPot2;

            if (targetPot1 == null)
            {
                foreach (Transform candidate in detectedPots)
                    if (candidate != targetPot2) { targetPot1 = candidate; break; }
            }
            if (targetPot2 == null)
            {
                foreach (Transform candidate in detectedPots)
                    if (candidate != targetPot1) { targetPot2 = candidate; break; }
            }

            // Mantem o nome da primeira luz para reutilizar rigs ja instalados.
            Light potLight1 = CreateOrReuse(rig, "04 - Magia do caldeirao");
            Light potLight2 = CreateOrReuse(rig, "05 - Magia do caldeirao 2");
            ConfigurePotLight(potLight1, targetPot1, center, width, depth, colors.potion,
                colors.potionPower * brightness * cauldronGlow);
            ConfigurePotLight(potLight2, targetPot2, center, width, depth, colors.potion,
                colors.potionPower * brightness * cauldronGlow);
            data.cauldronLight = potLight1; // compatibilidade com a versao anterior
            data.cauldronLight2 = potLight2;

            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            int activePots = (targetPot1 != null ? 1 : 0) + (targetPot2 != null ? 1 : 0);
            Debug.Log("[Brunas Brew] Iluminacao aplicada. Caldeiroes iluminados: " + activePots + "/2.");
            EditorUtility.DisplayDialog("Bruna's Brew", "Iluminacao aplicada na cena!\n\n" +
                "Caldeiroes iluminados: " + activePots + "/2." +
                (activePots < 2 ? "\nArraste os caldeiroes nos campos da janela se a deteccao nao encontrar os dois." : "") +
                "\n\nAjuste as luzes em BRUNAS_BREW_LIGHTING e salve a cena (Ctrl+S).", "OK");
        }

        private static void Restore()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject rig = FindRig(scene);
            if (rig == null)
            {
                EditorUtility.DisplayDialog("Bruna's Brew", "Nenhuma iluminacao instalada nesta cena.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Restaurar iluminacao",
                "Restaurar as luzes originais e remover o conjunto criado pelo Bruna's Brew?", "Restaurar", "Cancelar"))
                return;

            BrunasBrewLightingRigData data = rig.GetComponent<BrunasBrewLightingRigData>();
            if (data != null && data.hasBackup)
            {
                RenderSettings.ambientMode = data.originalAmbientMode;
                RenderSettings.ambientSkyColor = data.originalSky;
                RenderSettings.ambientEquatorColor = data.originalEquator;
                RenderSettings.ambientGroundColor = data.originalGround;
                RenderSettings.ambientIntensity = data.originalAmbientIntensity;
                RenderSettings.reflectionIntensity = data.originalReflectionIntensity;
                foreach (var entry in data.originalLights)
                {
                    if (entry.source == null) continue;
                    Undo.RecordObject(entry.source, "Restaurar luz anterior");
                    entry.source.enabled = entry.wasEnabled;
                }
            }
            Undo.DestroyObjectImmediate(rig);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("[Brunas Brew] Iluminacao anterior restaurada.");
        }
    }
}
