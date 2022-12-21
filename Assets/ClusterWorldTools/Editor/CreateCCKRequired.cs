using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools
{
    public class CreateCCKRequired : MonoBehaviour
    {
        const string SCREEN_PREFAB_PATH = "Assets/ClusterVR/StaticResources/Prefabs/StandardMainScreen.prefab";

        [MenuItem("GameObject/ClusterCreatorKit/SpawnPoint")]
        public static void CreateSpawnPoint()
        {
            CreateObjectHasSingleComponent<ClusterVR.CreatorKit.World.Implements.SpawnPoints.SpawnPoint>("SpawnPoint");
        }

        [MenuItem("GameObject/ClusterCreatorKit/DespawnHeight")]
        public static void CreateDespawnHeight()
        {
            CreateObjectHasSingleComponent<ClusterVR.CreatorKit.World.Implements.DespawnHeights.DespawnHeight>("DespawnHeight");
        }

        [MenuItem("GameObject/ClusterCreatorKit/MainScreen")]
        public static void CreateMainScreen()
        {
            GameObject screen = PrefabUtility.LoadPrefabContents(SCREEN_PREFAB_PATH);
            if(screen == null)
            {
                Debug.LogError("Prefabが見つかりません。拡張機能を導入しなおしてください。");
                return;
            }
            screen.name = "StandardMainScreen";

            GameObject screen_instance = Selection.activeGameObject ? Instantiate(screen, Selection.activeGameObject.transform) : Instantiate(screen);
            screen_instance.name = "StandardMainScreen";
        }

        private static void CreateObjectHasSingleComponent<T>(in string name) where T : Component
        {
            GameObject gameObject = new GameObject();
            gameObject.name = name;
            if (Selection.activeGameObject != null) gameObject.transform.SetParent(Selection.activeGameObject.transform, false);
            gameObject.AddComponent<T>();
            Selection.activeGameObject = gameObject;
        }
    }
}
