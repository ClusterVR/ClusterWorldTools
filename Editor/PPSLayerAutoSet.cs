using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;

namespace ClusterWorldTools
{
    class PPSLayerAutoSet : Editor
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

            var postProcesses = FindObjectsOfType<PostProcessVolume>(true);
            foreach (var pps in postProcesses)
            {
                if(changedList.CheckAndAdd(pps.gameObject) == false)pps.gameObject.layer = POST_PROCESSING_LAYER;
            }
            changedList.RemoveDeletedMembers();
        }
    }
}