using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using ClusterVR.CreatorKit.World.Implements.MainScreenViews;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.World.Implements.DespawnHeights;
using ClusterVR.CreatorKit.Gimmick;
using UnityEditor.VersionControl;
using ClusterVR.CreatorKit.World;
using ClusterVR.CreatorKit.Editor.Preview.World;
using System;
using UnityEditor.Experimental.GraphView;
using Google.Protobuf.WellKnownTypes;

namespace ClusterWorldTools
{
    class ValidatorWindow : EditorWindow
    {
        ValidatorManager validatorManager = new ValidatorManager();
        List<IAutoFix> autoFixes = new List<IAutoFix>();

        bool foldOutIsOpen = false;

        [MenuItem("WorldTools/改善チェック")]
        static public void CreateWindow()
        {
            EditorWindow window = GetWindow<ValidatorWindow>();
            window.titleContent = new GUIContent("改善チェック");
        }

        public void OnEnable()
        {
            validatorManager.Load();
        }

        public void OnDisable()
        {
            validatorManager.Save();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("改善点をConsoleに表示します");
            foldOutIsOpen = EditorGUILayout.Foldout(foldOutIsOpen, "チェック項目を選択");
            if (foldOutIsOpen)
            {
                EditorGUI.indentLevel++;

                foreach (var validatorType in validatorManager.GetTypes())
                {
                    validatorManager.Enable(validatorType, EditorGUILayout.ToggleLeft(validatorManager.GetValidator(validatorType).GetDescription(), validatorManager.IsEnabled(validatorType)));
                }

                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("確認する"))
            {
                ValidateVenue();
                GetWindow(Assembly.Load("UnityEditor").GetType("UnityEditor.ConsoleWindow"), false, "Console", false).Focus();
            }

            foreach (var autoFix in autoFixes)
            {
                EditorGUILayout.LabelField(autoFix.GetLabel());
            }

            if (autoFixes.Count() > 0 && GUILayout.Button("すべて修正"))
            {
                foreach (var autoFix in autoFixes)
                {
                    autoFix.Fix();
                }
                autoFixes.Clear();
            }
        }

        private void ValidateVenue()
        {
            autoFixes.Clear();

            foreach (var validator in validatorManager.CreateEnabledValidatorInstances())
            {
                if (validator.Validate() == false)
                {
                    if (validator is IAutoFix) autoFixes.Add(validator as IAutoFix);
                }
            }
        }
    }

    class ValidatorManager
    {
        class ValidatorEnable
        {
            public IValidator validator { get; private set; }
            public bool isEnabled { get; private set; }

            public ValidatorEnable(System.Type validatorType, bool enable)
            {
                if(typeof(IValidator).IsAssignableFrom(validatorType))validator = (IValidator)Activator.CreateInstance(validatorType);
                else validator = null;

                isEnabled = enable;
            }

            public void Enable(bool enable)
            {
                isEnabled = enable;
            }
        }

        Dictionary<System.Type, ValidatorEnable> validators = new Dictionary<System.Type, ValidatorEnable>();

        const string SETTINGS_KEY_PREFIX = "ValidatorSettings/";

        public bool IsEnabled(System.Type type)
        {
            try
            {
                return validators[type].isEnabled;
            }
            catch
            {
                return false;
            }
        }

        public void Enable(System.Type type, bool enable)
        {
            if (validators.ContainsKey(type) == false) return;
            validators[type].Enable(enable);
        }

        public IValidator GetValidator(System.Type type)
        {
            return validators[type].validator;
        }

        public void Load()
        {
            validators.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass == false) continue;
                    if (type.IsGenericType) continue;
                    if (type.IsAbstract) continue;
                    if (typeof(IValidator).IsAssignableFrom(type) == false) continue;
                    validators.Add(type, new ValidatorEnable(type, Common.LoadSetting($"{SETTINGS_KEY_PREFIX}{type.Name}", true, System.Convert.ToBoolean)));
                }
            }
        }

        public void Save()
        {
            foreach(var validator in validators)
            {
                Common.SaveSetting($"{SETTINGS_KEY_PREFIX}{validator.Key.Name}", validator.Value.isEnabled);
            }
        }

        public IEnumerable<IValidator> CreateEnabledValidatorInstances()
        {
            return validators.Where(validator => validator.Value.isEnabled).Select(validator => validator.Value.validator);
        }

        public IEnumerable<System.Type> GetTypes()
        {
            return validators.Keys.ToArray();
        }
    }

    interface IValidator
    {
        public bool Validate();

        public string GetDescription();

        public static void PublishMessage(MessageType type, in string message, in string url = null, UnityEngine.Object target = null)
        {
            switch (type)
            {
                case MessageType.Info:
                    Debug.Log(message, target);
                    break;
                case MessageType.Warning:
                    Debug.LogWarning(message, target);
                    break;
                case MessageType.Error:
                    Debug.LogError(message, target);
                    break;
            }
        }

        delegate bool FindQueryFunc<T>(T x);

        public static IEnumerable<T> FindGameObjectsAndPrefabs<T>(FindQueryFunc<T> func) where T : UnityEngine.Object
        {
            var gameObjects = UnityEngine.Object.FindObjectsOfType<T>(true).Where(x => func(x));
            var prefabs = AssetDatabase.FindAssets("t:GameObject", null).Select(path => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(path))).Where(prefab => prefab != null && func(prefab));

            var ret = new List<T>();
            ret.AddRange(gameObjects);
            ret.AddRange(prefabs);

            return ret;
        }
    }

    interface IAutoFix
    {
        public void Fix();
        public string GetLabel();
    }

    class UnityVersionValidator : IValidator
    {
        // Unityバージョン
        const string CLUSTER_UNITY_VERSION = "2021.3.4f1";

        bool IValidator.Validate()
        {
            if (Application.unityVersion != CLUSTER_UNITY_VERSION)
            {
                IValidator.PublishMessage(MessageType.Error, $"Unityバージョンは{CLUSTER_UNITY_VERSION}を使用してください。", "https://docs.cluster.mu/creatorkit/installation/install-unity/");
                return false;
            }
            else return true;
        }

        string IValidator.GetDescription()
        {
            return "適切なUnityバージョンが使用されているか確認します。";
        }
    }

    class BuildTargetValidator : IValidator, IAutoFix
    {
        bool IValidator.Validate()
        {
#if UNITY_EDITOR_WIN
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows
                && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                IValidator.PublishMessage(MessageType.Error, "ビルドターゲットをWindowsにしてください。");
#else
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                IValidator.PublishMessage(MessageType.Error, "ビルドターゲットをMacOSにしてください。");
#endif
                return false;
            }
            return true;
        }

        string IValidator.GetDescription()
        {
            return "適切なビルドターゲットが指定されているか確認します。";
        }

        void IAutoFix.Fix()
        {
#if UNITY_EDITOR_WIN
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows64);
#else
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneOSX);
#endif
            Debug.Log("ビルドターゲットを修正しました。");
        }

        string IAutoFix.GetLabel()
        {
            return "ビルドターゲットを修正します。";
        }
    }

    class RealtimeLightValidator : IValidator
    {
        // リアルタイムライトの最大数
        const int MAX_NUMBER_OF_REALTIME_LIGHTS = 2;

        bool IValidator.Validate()
        {
            var lights = UnityEngine.Object.FindObjectsOfType<Light>(true).Where(light => light.lightmapBakeType != LightmapBakeType.Baked);

            if (lights.Count() > MAX_NUMBER_OF_REALTIME_LIGHTS)
            {
                foreach (var light in lights)
                {
                    IValidator.PublishMessage(MessageType.Warning, $"RealtimeまたはMixedライトは同時に{MAX_NUMBER_OF_REALTIME_LIGHTS}つまでしか正確に反映されません。ライトベイクを推奨します。"
                        , "https://docs.cluster.mu/creatorkit/world/lighting-world/", light);
                }
                return false;
            }
            else return true;
        }

        string IValidator.GetDescription()
        {
            return "リアルタイムライトの個数が推奨範囲内かどうか確認します。";
        }
    }

    class TextureAssetValidator : IValidator
    {
        // テクスチャの最大解像度
        const int MAX_TEXTURE_SIZE = 1024;

        bool IValidator.Validate()
        {
            bool result = true;

            var assetPaths = AssetDatabase.GetAllAssetPaths();

            foreach (var path in assetPaths)
            {
                if (path.Contains("Assets")) continue;
                var texture = AssetImporter.GetAtPath(path) as TextureImporter;
                if (texture == null) continue;

                int textureSize = texture.maxTextureSize;
                int width_mobile;
                TextureImporterFormat format;
                texture.GetPlatformTextureSettings("Android", out width_mobile, out format);
                textureSize = Mathf.Max(textureSize, width_mobile);
                texture.GetPlatformTextureSettings("iOS", out width_mobile, out format);
                textureSize = Mathf.Max(textureSize, width_mobile);
                if (textureSize > MAX_TEXTURE_SIZE)
                {
                    result = false;
                    IValidator.PublishMessage(MessageType.Warning, "テクスチャ解像度が大きすぎます。", "", texture);
                }
            }

            return result;
        }

        string IValidator.GetDescription()
        {
            return "テクスチャ解像度が大きすぎる場合に警告します。";
        }
    }

#if UNITY_EDITOR_WIN
    class FilePathLengthValidator : IValidator
    {
        // Windowsのディレクトリ名の長さの最大
        const int MAX_DIRECTORY_NAME_LENGTH_WIN = 260;

        bool IValidator.Validate()
        {
            bool result = true;

            var assetPaths = AssetDatabase.GetAllAssetPaths();

            foreach (var path in assetPaths)
            {
                if (Path.GetFullPath(path).Length > MAX_DIRECTORY_NAME_LENGTH_WIN)
                {
                    result = false;
                    IValidator.PublishMessage(MessageType.Error, "ファイルパスが長すぎます。ファイル、もしくはプロジェクト全体を浅い階層に移動してください。", "", AssetDatabase.LoadMainAssetAtPath(path));
                }
            }
            return result;
        }

        string IValidator.GetDescription()
        {
            return "ファイルパスの長さが制限を超えていないか確認します。";
        }
    }
#endif


    class CollideGimmicksValidator : IValidator
    {
        bool IValidator.Validate()
        {
            // 他のコライダとの接触検知には自身にコライダor自身にRigidbodyが付いたうえで子にコライダが必要
            var invalidOnCollideItemTriggers = IValidator.FindGameObjectsAndPrefabs<ClusterVR.CreatorKit.Trigger.Implements.OnCollideItemTrigger>(trigger => trigger.GetComponent<Collider>() == null && (trigger.GetComponent<Rigidbody>() == null || trigger.GetComponentInChildren<Collider>(true) == null));
            foreach (var trigger in invalidOnCollideItemTriggers)
            {
                IValidator.PublishMessage(MessageType.Warning, "On Collide Item Triggerにはコライダーが必要です。子オブジェクトのコライダーを適用するにはRigidbodyを追加してください。", "", trigger);
            }

            // クリック場所の判定には単純に自身or子にコライダがあればいい
            var invalidContactableItems = IValidator.FindGameObjectsAndPrefabs<ClusterVR.CreatorKit.Item.Implements.ContactableItem>(item => item.GetComponentInChildren<Collider>(true) == null);
            foreach (var item in invalidContactableItems)
            {
                IValidator.PublishMessage(MessageType.Warning, "アイテムをクリックするためにはコライダーが必要です。", "", item);
            }

            return invalidOnCollideItemTriggers.Count() == 0 && invalidContactableItems.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "ギミックやトリガーに適切なコライダーが設定されているか確認します。";
        }
    }

    class ScreenShaderValidator : IValidator
    {
        const string SCREEN_SHADER_NAME = "ClusterVR/InternalSDK/MainScreen";

        bool IValidator.Validate()
        {
            var screens = IValidator.FindGameObjectsAndPrefabs<StandardMainScreenView>(screen =>
            {
                var renderer = screen.GetComponent<MeshRenderer>();
                return renderer.sharedMaterial.shader.name != SCREEN_SHADER_NAME;
            });

            foreach (var screen in screens)
            {
                IValidator.PublishMessage(MessageType.Info, $"スクリーンのシェーダーは{SCREEN_SHADER_NAME}を推奨します。", "", screen);
            }

            return screens.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "スクリーンに推奨シェーダーが使われているか確認します。";
        }
    }

    class DespawnHeightExistValidator : IValidator, IAutoFix
    {
        bool IValidator.Validate()
        {
            if (UnityEngine.Object.FindObjectOfType<DespawnHeight>(true) == null)
            {
                IValidator.PublishMessage(MessageType.Error, "ワールドにはDespawnHeightがひとつ必要です。");
                return false;
            }

            return true;
        }

        string IValidator.GetDescription()
        {
            return "Despawn Heightが配置されているか確認します。";
        }

        // 既存コライダのBoundsの一番低い位置の1m下に作成（コライダが一つもない場合は-5mの位置）
        void IAutoFix.Fix()
        {
            var colliders = UnityEngine.Object.FindObjectsOfType<Collider>(true);
            float height = float.MaxValue;
            if (colliders.Length > 0)
            {
                foreach (var collider in colliders) height = Mathf.Min(height, collider.bounds.min.y);
                height -= 1.0f;
            }
            else height = -5.0f;

            GameObject despawn = new GameObject();
            despawn.name = "Despawn";
            despawn.AddComponent<ClusterVR.CreatorKit.World.Implements.DespawnHeights.DespawnHeight>();
            despawn.transform.position = new Vector3(0, height, 0);
            Selection.activeObject = despawn;

            Debug.Log($"Despawn Heightを高さ {height} の位置に作成しました。");
        }

        string IAutoFix.GetLabel()
        {
            return "Despawn Heightを作成します。";
        }
    }

    class DespawnHeightPositionValidator : IValidator
    {
        bool IValidator.Validate()
        {
            var despawnHeight = UnityEngine.Object.FindObjectOfType<DespawnHeight>(true);
            if (despawnHeight == null) return true;

            var colliders = UnityEngine.Object.FindObjectsOfType<Collider>(true);
            bool result = true;
            foreach (var collider in colliders)
            {
                if (collider.bounds.max.y < despawnHeight.transform.position.y)
                {
                    IValidator.PublishMessage(MessageType.Warning, "DespawnHeightより低い位置にコライダーがあります。", "", collider);
                    result = false;
                }
            }

            return result;
        }

        string IValidator.GetDescription()
        {
            return "Despawn Heightの位置が適切でない可能性がある場合に警告します。";
        }
    }

    class DespawnHeightLimitValidator : IValidator
    {
        bool IValidator.Validate()
        {
            var despawnHeights = UnityEngine.Object.FindObjectsOfType<DespawnHeight>();
            if (despawnHeights.Length > 1)
            {
                foreach (var despawnHeight in despawnHeights)
                {
                    IValidator.PublishMessage(MessageType.Error, "DespawnHeightはひとつだけ設置する必要があります。", "", despawnHeight);
                }
                return false;
            }

            return true;
        }

        string IValidator.GetDescription()
        {
            return "Despawn Heightが2つ以上ある場合に警告します。";
        }
    }

    class SpawnPointExistValidator : IValidator
    {
        bool IValidator.Validate()
        {
            if (UnityEngine.Object.FindObjectOfType<ClusterVR.CreatorKit.World.Implements.SpawnPoints.SpawnPoint>(true) == null)
            {
                IValidator.PublishMessage(MessageType.Error, "ワールドにはSpawnPointがひとつ以上必要です。");
                return false;
            }

            return true;
        }

        string IValidator.GetDescription()
        {
            return "Spawn Pointが設置されているかどうか確認します。";
        }
    }

    class ColorSpaceValidator : IValidator, IAutoFix
    {
        bool IValidator.Validate()
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                IValidator.PublishMessage(MessageType.Warning, "Color SpaceをLinearに変更してください。");
                return false;
            }

            return true;
        }

        string IValidator.GetDescription()
        {
            return "Color Spaceの設定を確認します。";
        }

        void IAutoFix.Fix()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("Color Spaceを変更しました。");
        }

        string IAutoFix.GetLabel()
        {
            return "Color SpaceをLinearに変更します。";
        }
    }

    class AvatarLightValidator : IValidator, IAutoFix
    {
        static string[] avatarLayers =
        {
                LayerMask.LayerToName(6),  //Accessory
                LayerMask.LayerToName(7),  //AccessoryPreview
                LayerMask.LayerToName(16), //OwnAvatar
                LayerMask.LayerToName(23), //Performer
                LayerMask.LayerToName(24)  //Audience
        };
        static int allAvatarLayerMask = LayerMask.GetMask(avatarLayers);

        IEnumerable<Light> lights = null;

        bool IValidator.Validate()
        {
            lights = UnityEngine.Object.FindObjectsOfType<Light>(true).Where(light =>
            {
                var avatarLayerMask = light.cullingMask & allAvatarLayerMask;
                return !((avatarLayerMask == allAvatarLayerMask) || (avatarLayerMask == 0));
            });

            foreach (var light in lights)
            {
                IValidator.PublishMessage(MessageType.Warning, "アバター関連レイヤーの一部にライトが当たらない設定になっています。", "", light);
            }

            return lights.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "アバターのライティング設定を確認します。";
        }

        void IAutoFix.Fix()
        {
            if (lights == null) return;
            var fixLights = lights.Where(light => ((light.cullingMask & allAvatarLayerMask) != 0));
            foreach (var light in fixLights)
            {
                light.cullingMask = light.cullingMask | allAvatarLayerMask;
                Debug.Log("ライトがアバター全体を照らすようにレイヤーマスクを修正しました。", light);
            }
        }

        string IAutoFix.GetLabel()
        {
            return "アバターを照らしているライトのレイヤーマスクを修正します。";
        }
    }

    class ParticleStopActionValidator : IValidator
    {
        bool IValidator.Validate()
        {
            var particles = IValidator.FindGameObjectsAndPrefabs<ParticleSystem>(particle => particle.GetComponentInChildren<ClusterVR.CreatorKit.Item.Implements.Item>() != null && particle.main.stopAction == ParticleSystemStopAction.Destroy);
            foreach (var particle in particles)
            {
                IValidator.PublishMessage(MessageType.Warning, "ItemをParticle SystemのStop ActionでDestroyすることは非推奨です。Destroy Item Gimmickを利用してください。", "", particle);
            }

            return particles.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "Particle Systemの機能でItemが破棄される可能性がある場合に警告します。";
        }
    }

    class WorldGateIdValidator : IValidator
    {
        // World IDの正規表現
        const string WORLD_ID_REGEX = "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
        Regex worldIdRegex = new Regex(WORLD_ID_REGEX, RegexOptions.Compiled);

        bool IValidator.Validate()
        {
            var worldGates = IValidator.FindGameObjectsAndPrefabs<ClusterVR.CreatorKit.World.Implements.WorldGate.WorldGate>(worldGate =>
            {
                var s = new SerializedObject(worldGate);
                return worldIdRegex.IsMatch(s.FindProperty("worldOrEventId").stringValue) == false;
            });

            foreach (var worldGate in worldGates)
            {
                IValidator.PublishMessage(MessageType.Error, "World Gateの行先IDが正しくありません。IDはワールド詳細ページURLのhttps://cluster.mu/w/以降、またはイベント詳細ページURLのhttps://cluster.mu/e/以降の文字列です。"
                    , "https://docs.cluster.mu/creatorkit/world-components/world-gate/", worldGate);
            }

            return worldGates.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "World Gateの行先IDに適切なIDが指定されているか確認します。";
        }
    }

    class CreateItemGimmickDestroyValidator : IValidator
    {
        bool IValidator.Validate()
        {
            var gimmicks = IValidator.FindGameObjectsAndPrefabs<ClusterVR.CreatorKit.Gimmick.Implements.CreateItemGimmick>(gimmick => gimmick.ItemTemplate != null && gimmick.ItemTemplate.gameObject.GetComponent<ClusterVR.CreatorKit.Gimmick.Implements.DestroyItemGimmick>() == null);
            foreach (var gimmick in gimmicks)
            {
                IValidator.PublishMessage(MessageType.Info, "Create Item Gimmickで生成されるItemはDestroy Item Gimmickで破棄することを推奨します。", "", gimmick);
            }

            return gimmicks.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "Create Item Gimmickの対象にDestroy Item Gimmickが設定されているか確認します。";
        }
    }
    class RigidbodyMeshColliderValidator : IValidator, IAutoFix
    {
        IEnumerable<MeshCollider> colliders = null;
        bool IValidator.Validate()
        {
            colliders = IValidator.FindGameObjectsAndPrefabs<MeshCollider>(collider => collider.gameObject.GetComponent<Rigidbody>() != null && collider.convex == false);
            foreach (var collider in colliders)
            {
                IValidator.PublishMessage(MessageType.Warning, "RigidbodyにMesh Colliderを正しく適用するにはConvexを指定する必要があります。", "", collider);
            }
            return colliders.Count() == 0;
        }

        string IValidator.GetDescription()
        {
            return "RigidbodyのMesh ColliderのConvex設定を確認します。";
        }

        void IAutoFix.Fix()
        {
            if (colliders == null) return;

            foreach (var collider in colliders)
            {
                collider.convex = true;
            }
        }

        string IAutoFix.GetLabel()
        {
            return "Rigidbodyが設定されたMesh ColliderをConvexにします。";
        }
    }
}