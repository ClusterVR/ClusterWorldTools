using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools
{
    public class CreateCCKRequired : MonoBehaviour
    {
        const string SCREEN_PREFAB_PATH = "Prefabs/StandardMainScreen.prefab";
        const string SPEED_JUMP_PREFAB_PATH = "Prefabs/SpeedAndJump.prefab";

        const string GAMEOBJECT_MENU_ITEM = "GameObject/ClusterCreatorKit/";

        [MenuItem(GAMEOBJECT_MENU_ITEM + "SpawnPoint")]
        public static void CreateSpawnPoint()
        {
            CreateObjectHasSingleComponent<ClusterVR.CreatorKit.World.Implements.SpawnPoints.SpawnPoint>("SpawnPoint");
        }

        [MenuItem(GAMEOBJECT_MENU_ITEM + "DespawnHeight")]
        public static void CreateDespawnHeight()
        {
            CreateObjectHasSingleComponent<ClusterVR.CreatorKit.World.Implements.DespawnHeights.DespawnHeight>("DespawnHeight");
        }

        [MenuItem(GAMEOBJECT_MENU_ITEM + "MainScreen")]
        public static void CreateMainScreen()
        {
            InstantiatePrefab("StandardMainScreen", Common.AssetPath(SCREEN_PREFAB_PATH));
        }

        [MenuItem(GAMEOBJECT_MENU_ITEM + "Speed and Jump")]
        public static void CreateMoveSpeed()
        {
            if(FindObjectOfType<ClusterVR.CreatorKit.Gimmick.Implements.SetMoveSpeedRatePlayerGimmick>() != null
                || FindObjectOfType<ClusterVR.CreatorKit.Gimmick.Implements.SetJumpHeightRatePlayerGimmick>() != null)
            {
                if (EditorUtility.DisplayDialog("確認", "Set Move Speed Rate Player GimmickまたはSet Jump Height Rate Player Gimmickがすでに存在します。新規作成しますか？", "Yes", "No") == false) return;
            }
            InstantiatePrefab("Speed and Jump", Common.AssetPath(SPEED_JUMP_PREFAB_PATH));
        }
        
        private static GameObject CreateObjectHasSingleComponent<T>(in string name) where T : Component
        {
            GameObject gameObject = new GameObject();
            gameObject.name = name;
            if (Selection.activeGameObject != null) gameObject.transform.SetParent(Selection.activeGameObject.transform, false);
            gameObject.AddComponent<T>();
            Selection.activeGameObject = gameObject;

            return gameObject;
        }

        private static GameObject InstantiatePrefab(in string name, in string prefabPath)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Prefabが見つかりません。拡張機能を導入しなおしてください。");
                return null;
            }

            GameObject prefab_instance = Selection.activeGameObject ? Instantiate(prefab, Selection.activeGameObject.transform) : Instantiate(prefab);
            prefab_instance.name = name;
            Selection.activeGameObject = prefab_instance;

            return prefab_instance;
        }
    }
}
