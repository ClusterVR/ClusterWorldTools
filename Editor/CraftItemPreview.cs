using ClusterVR.CreatorKit.Editor.ItemExporter;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace ClusterWorldTools
{
    public class CraftItemPreviewScene : IDisposable
    {
        const int RENDER_SIZE_X = 1024;
        const int RENDER_SIZE_Y = RENDER_SIZE_X;

        const string PPS_ASSET_DIRECTORY = "Assets/PPSConfiguration";

        GameObject item = null;
        GameObject root = null;

        Scene scene;
        Camera camera;
        GameObject cameraGameObject = null;
        PostProcessVolume ppsComponent;

        public Color backgroundColor = Color.black;

        public bool applyCurrentEnvironment = false;

        public struct RenderSituation
        {
            public string name { get; }
            public string assetName { get; }
            public RenderTexture texture { get; set; }
            public RenderSituation(string _name, string _assetName)
            {
                name = _name;
                assetName = _assetName;
                texture = new RenderTexture(RENDER_SIZE_X, RENDER_SIZE_Y, 32, GraphicsFormat.R16G16B16A16_SFloat);
            }
        }

        struct EnvironmentSettings
        {
            AmbientMode ambientMode;
            public Color ambientSkyColor, ambientEquatorColor, ambientGroundColor;
            DefaultReflectionMode reflectionMode;
            Cubemap reflection;

            public EnvironmentSettings(AmbientMode _ambientMode, Color _ambientSkyColor, Color _ambientEquatorColor, Color _ambientGroundColor, DefaultReflectionMode _reflectionMode, Cubemap _reflection)
            {
                ambientMode = _ambientMode;
                ambientSkyColor = _ambientSkyColor;
                ambientEquatorColor = _ambientEquatorColor;
                ambientGroundColor = _ambientGroundColor;
                reflectionMode = _reflectionMode;
                reflection = _reflection;
            }

            public void SetRenderSettings()
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientSkyColor = ambientSkyColor;
                RenderSettings.ambientEquatorColor = ambientEquatorColor;
                RenderSettings.ambientGroundColor = ambientGroundColor;
                RenderSettings.defaultReflectionMode = reflectionMode;
                RenderSettings.customReflection = reflection;
            }

            public static EnvironmentSettings GetCurrentSettings()
            {
                return new EnvironmentSettings(RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor, RenderSettings.defaultReflectionMode, RenderSettings.customReflection as Cubemap);
            }
        }

        // 青空の背景の環境光
        EnvironmentSettings previewEnvironment = new EnvironmentSettings(AmbientMode.Trilight, new Color(0.736f, 0.768f, 0.8f), new Color(0.5992157f, 0.6612035f, 0.7490196f), new Color(0.04588235f, 0.04806723f, 0.05098039f), DefaultReflectionMode.Custom, CreateSingleColorCubeMap(4, new Color(0.454902f, 0.7254902f, 0.9607844f)));
        
        private static Cubemap CreateSingleColorCubeMap(int size, Color color)
        {
            var texture = new Cubemap(size, DefaultFormat.LDR, TextureCreationFlags.None);
            var colors = Enumerable.Repeat(color, size * size).ToArray();

            foreach(CubemapFace face in Enum.GetValues(typeof(CubemapFace)))
            {
                if(face == CubemapFace.Unknown) continue;
                texture.SetPixels(colors, face);
            }
            return texture;
        }

        public RenderSituation[] renderSituations =
        {
            new RenderSituation("エフェクトなし","none.asset"),
            new RenderSituation("ブルーム（弱）", "bloom_min.asset"),
            new RenderSituation("ブルーム（中）", "bloom_middle.asset"),
            new RenderSituation("ブルーム（強）", "bloom_max.asset"),
            new RenderSituation("カラー（黒）", "color_grading_black.asset"),
            new RenderSituation("カラー（青）", "color_grading_blue.asset"),
            new RenderSituation("カラー（緑）", "color_grading_green.asset"),
            new RenderSituation("カラー（オレンジ）", "color_grading_orange.asset"),
            new RenderSituation("カラー（ピンク）", "color_grading_pink.asset"),
            new RenderSituation("カラー（紫）", "color_grading_purple.asset"),
            new RenderSituation("カラー（赤）", "color_grading_red.asset"),
            new RenderSituation("カラー（黄）", "color_grading_yellow.asset")
        };

        public CraftItemPreviewScene()
        {
        }

        public void InitializeScene()
        {
            scene = EditorSceneManager.NewPreviewScene();
            root = new GameObject();
            EditorSceneManager.MoveGameObjectToScene(root, scene);
            InitializeCamera();
            InitializePPS();
        }

        public void Update()
        {
            var currentEnvironment = EnvironmentSettings.GetCurrentSettings();
            if (applyCurrentEnvironment == false)previewEnvironment.SetRenderSettings();
            for(int i = 0; i < renderSituations.Length; ++i)
            {
                ppsComponent.profile = LoadPPSProfile(renderSituations[i].assetName);
                Render(ref renderSituations[i]);
            }
            currentEnvironment.SetRenderSettings();
        }

        private void InitializeCamera()
        {
            if (cameraGameObject != null) UnityEngine.Object.DestroyImmediate(cameraGameObject);

            cameraGameObject = new GameObject();
            EditorSceneManager.MoveGameObjectToScene(cameraGameObject, scene);

            camera = cameraGameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.scene = scene;

            var ppsLayer = cameraGameObject.AddComponent<PostProcessLayer>();
            ppsLayer.volumeLayer = 1;
        }

        private void InitializePPS()
        {
            var ppsObject = new GameObject();
            ppsObject.transform.parent = root.transform;
            ppsObject.layer = 0;

            ppsComponent = ppsObject.AddComponent<PostProcessVolume>();
            ppsComponent.isGlobal = true;

            ppsComponent.profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        }

        private void Render(ref RenderSituation situation)
        {
            if(situation.texture ==  null) situation.texture = new RenderTexture(RENDER_SIZE_X, RENDER_SIZE_Y, 32, GraphicsFormat.R16G16B16A16_SFloat);
            camera.targetTexture = situation.texture;
            camera.backgroundColor = backgroundColor;

            camera.Render();
        }

        public void LoadItem(GameObject targetItem)
        {
            if (item != null) GameObject.DestroyImmediate(item);
            if (targetItem == null)
            {
                item = null;
                return;
            }
            item = (GameObject)PrefabUtility.InstantiatePrefab(targetItem, root.transform);
            if(item == null) item = GameObject.Instantiate(targetItem, root.transform);

            item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var bounds = EncapsulationBounds(item);
            var rot = Quaternion.Euler(30f, 135f, 0f);
            var pos = bounds.center + Mathf.Max(10f, bounds.size.magnitude) * (rot * Vector3.back);
            cameraGameObject.transform.SetPositionAndRotation(pos, rot);
            camera.cameraType = CameraType.Preview;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.size.magnitude, ClusterVR.CreatorKit.Constants.Constants.ItemPreviewMagnificationLimitDiagonalSize) * 0.6f;
        }

        private PostProcessProfile LoadPPSProfile(in string assetPath)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(Common.AssetPath($"{PPS_ASSET_DIRECTORY}/{assetPath}"));
            if(profile == null)
            {
                Debug.LogError("必要なアセットが見つかりません。拡張機能を導入しなおしてください。");
                return ScriptableObject.CreateInstance<PostProcessProfile>();
            }
            return profile;
        }

        public void Dispose()
        {
            if (cameraGameObject != null) UnityEngine.Object.DestroyImmediate(cameraGameObject);
            foreach (var situation in renderSituations)
            {
                if(situation.texture != null) UnityEngine.Object.DestroyImmediate(situation.texture);
            }
            if(item != null) UnityEngine.Object.DestroyImmediate(item);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);

            EditorSceneManager.ClosePreviewScene(scene);
        }

        static Bounds EncapsulationBounds(GameObject go)
        {
            var activeRenderers = go.GetComponentsInChildren<Renderer>();
            if (activeRenderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            return activeRenderers.Select(r => r.bounds)
                .Aggregate(((result, current) =>
                {
                    result.Encapsulate(current);
                    return result;
                }));
        }
    }

    public class CraftItemPreview : EditorWindow
    {
        CraftItemPreviewScene scene;

        [SerializeField]
        GameObject item;
        GameObject item_before;

        Color color = Color.black;

        Vector2 scrollPosition = Vector2.zero;

        [MenuItem("WorldTools/クラフトアイテムプレビュー")]
        static public void CreateWindow()
        {
            EditorWindow window = GetWindow<CraftItemPreview>();
            window.titleContent = new GUIContent("クラフトアイテムプレビュー");
        }

        public void OnDisable()
        {
            if(scene != null)scene.Dispose();
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            SerializedObject serialized = new SerializedObject(this);
            serialized.Update();

            SerializedProperty itemProperty = serialized.FindProperty("item");
            EditorGUILayout.PropertyField(itemProperty, true);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                bool load = GUILayout.Button(EditorGUIUtility.IconContent("Refresh"));
                if (item != item_before) load = true;
                item_before = item;

                if (load && item != null)
                {
                    if (scene != null) scene.Dispose();
                    scene = new CraftItemPreviewScene();
                    scene.InitializeScene();

                    scene.LoadItem(item);
                }
            }

            if(EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("プレイモード中は使えません。");
            }


            if (scene != null)
            {
                color = EditorGUILayout.ColorField("背景色", color);
                scene.backgroundColor = color;
                scene.applyCurrentEnvironment = !EditorGUILayout.Toggle("標準のライティングを適用", !scene.applyCurrentEnvironment);
                scene.applyCurrentEnvironment = EditorGUILayout.Toggle("シーンのライティングを適用", scene.applyCurrentEnvironment);

                using (var scope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    scene.Update();

                    var size = this.position.width / 4;
                    var rect = new Rect(scope.scrollPosition.x, scope.scrollPosition.y, size, size);

                    var labelStyle = new GUIStyle(EditorStyles.boldLabel);
                    labelStyle.normal.textColor = color.grayscale > 0.5f ? Color.black : Color.white;

                    foreach (var situation in scene.renderSituations)
                    {
                        EditorGUI.LabelField(rect, new GUIContent(situation.texture));

                        var labelRect = new Rect(rect.position, new Vector2(rect.width, EditorStyles.boldLabel.fontSize));
                        EditorGUI.LabelField(labelRect, situation.name, labelStyle);
                        rect.Set(rect.position.x + size, rect.position.y, rect.width, rect.height);
                        if (rect.position.x + rect.width > this.position.width)
                        {
                            rect.Set(scope.scrollPosition.x, rect.position.y + size, rect.width, rect.height);
                        }
                    }

                    scrollPosition = scope.scrollPosition;
                }
            }

            serialized.ApplyModifiedProperties();
        }
    }
}