using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using ClusterVR.CreatorKit.Editor.EditorEvents;

namespace ClusterWorldTools.Editor.Utility
{
    public class DeactivateOnUpload
    {
        public const string deactivateOnUploadTag = "DeactivateOnUpload";

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            // タグがない場合は追加
            EnsureTagExists(deactivateOnUploadTag);

            WorldUploadEvents.RegisterOnWorldUploadStart((data =>
            {
                DeactivateSceneObjectsWithTag(data.Scene, deactivateOnUploadTag);
                return true;
            }));
        }

        public static void DeactivateSceneObjectsWithTag(Scene scene, string tag)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in rootGameObjects)
            {
                DeactivateGameObjectWithTagRecursive(gameObject, tag);
            }
        }

        static void EnsureTagExists(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            for (var i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            var index = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(index);
            tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }

        static void DeactivateGameObjectWithTagRecursive(GameObject gameObject, string tag)
        {
            if (gameObject.CompareTag(tag))
            {
                gameObject.SetActive(false);
            }
            foreach (Transform child in gameObject.transform)
            {
                DeactivateGameObjectWithTagRecursive(child.gameObject, tag);
            }
        }
    }
}
