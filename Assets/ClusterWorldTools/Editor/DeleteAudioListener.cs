using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools
{
    class DeleteAudioListener : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        public static void SetCallBack()
        {
            EditorApplication.hierarchyChanged += Delete;
        }

        public static void Delete()
        {
            if (EditorApplication.isPlaying) return;

            var cameras = FindObjectsOfType<Camera>(true);
            foreach (var camera in cameras)
            {
                var audioListenerComponent = camera.GetComponent<AudioListener>();
                if (audioListenerComponent == null) continue;
                DestroyImmediate(audioListenerComponent);
            }
        }
    }
}