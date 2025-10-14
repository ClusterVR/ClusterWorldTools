using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using ClusterWorldTools.Editor.Common;

namespace ClusterWorldTools.Editor.Utility
{
    class PPSLayerAutoSet
    {
        const int POST_PROCESSING_LAYER = 21;

        static GameObjectIdList changedList = null;

        [InitializeOnLoadMethod]
        public static void SetCallBack()
        {
            EditorApplication.hierarchyChanged += SetLayer;
            EditorSceneManager.sceneOpened += ClearList;
        }

        public static void ClearList(Scene scene, OpenSceneMode openSceneMode)
        {
            changedList = null;
        }

        static void SetLayer()
        {
            if(changedList == null)
            {
                changedList = new GameObjectIdList();
                changedList.Initialize<PostProcessVolume>();
            }

            var postProcesses = UnityEngine.Object.FindObjectsByType<PostProcessVolume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pps in postProcesses)
            {
                if (changedList.CheckAndAdd(pps.gameObject) == false)
                {
                    Undo.RecordObject(pps.gameObject, "Set Post-Processing Layer");
                    pps.gameObject.layer = POST_PROCESSING_LAYER;
                }
            }
            changedList.RemoveDeletedMembers();
        }
    }
}