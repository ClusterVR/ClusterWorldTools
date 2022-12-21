using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using ClusterVR.CreatorKit.World.Implements.MainScreenViews;
using System.Linq;
using System.Reflection;

namespace ClusterWorldTools
{
    public class Validator : EditorWindow
    {
        // 定数
        // リアルタイムライトの最大数
        const int MAX_NUMBER_OF_REALTIME_LIGHTS = 2;

        // テクスチャの最大解像度
        const int MAX_TEXTURE_SIZE = 1024;

        // Unityバージョン
        const string CLUSTER_UNITY_VERSION = "2021.3.4f1";

        // Windowsのディレクトリ名の長さの最大
        const int MAX_DIRECTORY_NAME_LENGTH_WIN = 260;

        struct Message
        {
            public enum Type { tips, warning, error };
            public Type type;
            public string msg;
            public string url;
            public Object target;
            public delegate void Function();
            public Function function;

            public Message(Type _type, string _msg, string _url = "", Object _target = null) { type = _type; msg = _msg; url = _url; target = _target; function = null; }
            public Message(Type _type, string _msg, Function _function, Object _target = null) { type = _type; msg = _msg; function = _function; target = _target; url = ""; }
        }

        static List<Message> messages = new List<Message>();

        [MenuItem("WorldTools/改善チェック")]
        static public void CreateWindow()
        {
            EditorWindow window = GetWindow<Validator>();
            window.titleContent = new GUIContent("改善チェック");
        }
        private void OnGUI()
        {
            bool result = true;
            EditorGUILayout.LabelField("改善点をConsoleに表示します");
            if(GUILayout.Button("確認する"))result = ValidateVenue();
            if (messages.Count > 0)
            {
                EditorGUILayout.LabelField("以下を自動修正できます");
                foreach (var msg in messages) EditorGUILayout.LabelField(msg.msg);
                if (GUILayout.Button("修正する"))
                {
                    List<Message> messages_remove = new List<Message>();
                    foreach (var msg in messages)
                    {
                        if (msg.function != null) msg.function();
                        messages_remove.Add(msg);
                    }
                    foreach (var msg in messages_remove) messages.Remove(msg);
                }
            }
        }

        private bool ValidateVenue()
        {
            bool result = true;

            var assetPaths = AssetDatabase.GetAllAssetPaths();

            messages.Clear();

            result &= ValidateRealtimeLights();
            result &= ValidateDespawnHeight();
            result &= ValidateTextureAssets(assetPaths);
            result &= ValidateFilePathLength(assetPaths);
            result &= ValidateCollideGimmicks();
            result &= ValidateScreenShader();
            result &= ValidateUnityVersion();
            result &= ValidateBuildTarget();
            result &= ValidateRequiredComponents();
            result &= ValidateColorSpace();
            result &= ValidateAvatarLights();
            result &= ValidateParticleStopAction();

            List<Message> messages_remove = new List<Message>();
            foreach (var msg in messages)
            {
                switch(msg.type)
                {
                    case Message.Type.tips:
                        Debug.Log(msg.msg, msg.target);
                        break;
                    case Message.Type.warning:
                        Debug.LogWarning(msg.msg, msg.target);
                        break;
                    case Message.Type.error:
                        Debug.LogError(msg.msg, msg.target);
                        break;
                }
                if (msg.function == null) messages_remove.Add(msg);
            }
            foreach (var msg in messages_remove) messages.Remove(msg);

            if(result == false)GetWindow(Assembly.Load("UnityEditor").GetType("UnityEditor.ConsoleWindow"), false, "Console", false).Focus();

            return result;
        }

        delegate bool queryFunc<T>(T x);

        private List<T> FindGameObjectsAndPrefabs<T>(queryFunc<T> func) where T : Object
        {
            var gameObjects = FindObjectsOfType<T>(true).Where(x => func(x));
            var prefabs = AssetDatabase.FindAssets("t:GameObject", null).Select(path => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(path))).Where(prefab => prefab != null && func(prefab));

            var ret = new List<T>();
            ret.AddRange(gameObjects);
            ret.AddRange(prefabs);

            return ret;
        }

        private bool ValidateUnityVersion()
        {
            if (Application.unityVersion != CLUSTER_UNITY_VERSION)
            {
                messages.Add(new Message(Message.Type.error, $"Unityバージョンは{CLUSTER_UNITY_VERSION}を使用してください。", "https://docs.cluster.mu/creatorkit/installation/install-unity/"));
                return false;
            }
            else return true;
        }

        private bool ValidateBuildTarget()
        {
#if UNITY_EDITOR_WIN
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows
                && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                messages.Add(new Message(Message.Type.error, "ビルドターゲットをWindowsにしてください。", FixBuildTarget));
#else
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                messages.Add(new Message(Message.Type.error, "ビルドターゲットをMacOSにしてください。", FixBuildTarget));
#endif
                return false;
            }
            return true;
        }

        private void FixBuildTarget()
        {
#if UNITY_EDITOR_WIN
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows64);
#else
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneOSX);
#endif
            Debug.Log("Fix Build Target");
        }

        private bool ValidateRealtimeLights()
        {
            var lights = FindObjectsOfType<Light>(true);

            var realtimeLights = lights.Where(light => light.lightmapBakeType != LightmapBakeType.Baked);

            if (realtimeLights.Count() > MAX_NUMBER_OF_REALTIME_LIGHTS)
            {
                foreach (var light in realtimeLights)
                {
                    messages.Add(new Message(Message.Type.warning, "RealtimeまたはMixedライトは同時に2つまでしか正確に反映されません。ライトベイクを推奨します。"
                        , "https://docs.cluster.mu/creatorkit/world/lighting-world/", light));
                }
                return false;
            }
            else return true;
        }

        private bool ValidateTextureAssets(in string[] assetPaths)
        {
            bool result = true;

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
                    messages.Add(new Message(Message.Type.warning, "テクスチャ解像度が大きすぎます。", "", texture));
                }
            }

            return result;
        }

        private bool ValidateFilePathLength(in string[] assetPaths)
        {
            bool result = true;

#if UNITY_EDITOR_WIN
            foreach (var path in assetPaths)
            {
                if(Path.GetFullPath(path).Length > MAX_DIRECTORY_NAME_LENGTH_WIN)
                {
                    result = false;
                    messages.Add(new Message(Message.Type.error, "ファイルパスが長すぎます。ファイル、もしくはプロジェクト全体を浅い階層に移動してください。", "", AssetDatabase.LoadMainAssetAtPath(path)));

                }
            }
#endif
            return result;
        }

        private bool ValidateCollideGimmicks()
        {
            // 他のコライダとの接触検知には自身にコライダor自身にRigidbodyが付いたうえで子にコライダが必要
            var invalidOnCollideItemTriggers = FindGameObjectsAndPrefabs<ClusterVR.CreatorKit.Trigger.Implements.OnCollideItemTrigger>(trigger => trigger.GetComponent<Collider>() == null && (trigger.GetComponent<Rigidbody>() == null || trigger.GetComponentInChildren<Collider>(true) == null));
            foreach (var trigger in invalidOnCollideItemTriggers)
            {
                messages.Add(new Message(Message.Type.warning, "On Collide Item Triggerにはコライダーが必要です。子オブジェクトのコライダーを適用するにはRigidbodyを追加してください。", "", trigger));
            }

            // クリック場所の判定には単純に自身or子にコライダがあればいい
            var invalidContactableItems = FindGameObjectsAndPrefabs<ClusterVR.CreatorKit.Item.Implements.ContactableItem>(item => item.GetComponentInChildren<Collider>(true) == null);
            foreach (var item in invalidContactableItems)
            {
                messages.Add(new Message(Message.Type.warning, "アイテムをクリックするためにはコライダーが必要です。", "", item));
            }

            return invalidOnCollideItemTriggers.Count == 0 && invalidContactableItems.Count == 0;
        }

        private bool ValidateScreenShader()
        {
            bool result = true;
            var gameObjects = FindObjectsOfType<StandardMainScreenView>(true);

            foreach (var obj in gameObjects)
            {
                var renderer = obj.GetComponent<MeshRenderer>();
                if (renderer == null) continue;

                if(renderer.sharedMaterial.shader.name != "ClusterVR/InternalSDK/MainScreen")
                {
                    result = false;
                    messages.Add(new Message(Message.Type.warning, "スクリーンのシェーダーはClusterVR/InternalSDK/MainScreenを推奨します。", "", obj));
                }
            }

            return result;
        }

        private bool ValidateDespawnHeight()
        {
            bool result = true;
            var despawnHeight = FindObjectOfType<ClusterVR.CreatorKit.World.Implements.DespawnHeights.DespawnHeight>(true);
            var colliders = FindObjectsOfType<Collider>(true);

            if (despawnHeight == null) return false;

            foreach (var collider in colliders)
            {
                if(collider.bounds.max.y < despawnHeight.transform.position.y)
                {
                    messages.Add(new Message(Message.Type.warning, "DespawnHeightより低い位置にコライダーがあります。", "", collider));
                    result = false;
                }
            }

            return result;
        }

        private bool ValidateRequiredComponents()
        {
            bool result = true;

            if(FindObjectOfType<ClusterVR.CreatorKit.World.Implements.SpawnPoints.SpawnPoint>(true) == null)
            {
                messages.Add(new Message(Message.Type.error, "ワールドにはSpawnPointがひとつ以上必要です。"));
                result = false;
            }

            var despawnHeights = FindObjectsOfType<ClusterVR.CreatorKit.World.Implements.DespawnHeights.DespawnHeight>(true);
            if (despawnHeights == null)
            {
                messages.Add(new Message(Message.Type.error, "ワールドにはDespawnHeightがひとつ必要です。"));
                result = false;
            }
            else if (despawnHeights.Length > 1)
            {
                foreach (var despawnHeight in despawnHeights)
                {
                    messages.Add(new Message(Message.Type.warning, "DespawnHeightはひとつだけ設置する必要があります。", "", despawnHeight));
                }
                result = false;
            }

            return result;
        }

        private bool ValidateColorSpace()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear) return true;
            else
            {
                messages.Add(new Message(Message.Type.error, "Color SpaceをLinearに変更してください。", FixColorSpace));
                return false;
            }
        }

        private void FixColorSpace()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("Color Spaceを修正しました");
        }

        private bool ValidateAvatarLights()
        {
            string[] avatarLayers =
            {
                LayerMask.LayerToName(6),  //Accessory
                LayerMask.LayerToName(7),  //AccessoryPreview
                LayerMask.LayerToName(16), //OwnAvatar
                LayerMask.LayerToName(23), //Performer
                LayerMask.LayerToName(24)  //Audience
            };
            var allAvatarLayerMask = LayerMask.GetMask(avatarLayers);

            var invalidAvatarLights = FindObjectsOfType<Light>(true).Where(light =>
            {
                var avatarLayerMask = light.cullingMask & allAvatarLayerMask;
                return !((avatarLayerMask == allAvatarLayerMask) || (avatarLayerMask == 0));
            }).ToArray();

            foreach (var invalidAvatarLight in invalidAvatarLights)
            {
                messages.Add(new Message(Message.Type.warning, "アバター関連レイヤーの一部にライトが当たらない設定になっています。", FixAvatarLights, invalidAvatarLight));
            }

            return invalidAvatarLights.Length == 0;
        }

        void FixAvatarLights()
        {
            string[] avatarLayers =
            {
                LayerMask.LayerToName(6),  //Accessory
                LayerMask.LayerToName(7),  //AccessoryPreview
                LayerMask.LayerToName(16), //OwnAvatar
                LayerMask.LayerToName(23), //Performer
                LayerMask.LayerToName(24)  //Audience
            };
            var allAvatarLayerMask = LayerMask.GetMask(avatarLayers);

            var avatarLights = FindObjectsOfType<Light>(true).Where(light =>
            {
                return (light.cullingMask & allAvatarLayerMask) != 0;
            });

            foreach (var light in avatarLights)
            {
                light.cullingMask = light.cullingMask | allAvatarLayerMask;
            }
        }

        private bool ValidateParticleStopAction()
        {
            var invalidParticles = FindGameObjectsAndPrefabs<ParticleSystem>(particle => particle.GetComponentInChildren<ClusterVR.CreatorKit.Item.Implements.Item>() != null && particle.main.stopAction == ParticleSystemStopAction.Destroy);
            foreach (var particle in invalidParticles)
            {
                messages.Add(new Message(Message.Type.warning, "ItemをParticle SystemのStop ActionでDestroyすることは非推奨です。Destroy Item Gimmickを利用してください。", "", particle));
            }

            return invalidParticles.Count == 0;
        }
    }
}