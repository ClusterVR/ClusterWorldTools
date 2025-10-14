using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools.Editor.Utility
{
    class DeleteAudioListener
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.hierarchyChanged += Delete;
        }

        public static void Delete()
        {
            if (EditorApplication.isPlaying) return;

            var activeGameObject = Selection.activeGameObject;
            if (activeGameObject == null) return;
            if (CheckIsEditorOnly(activeGameObject)) return;

            var audioListener = activeGameObject.GetComponent<AudioListener>();
            if (audioListener == null) return;

            Object.DestroyImmediate(audioListener);
            Debug.Log("Audio Listenerを自動削除しました。確認用など一時的に必要な場合はEditorOnlyにしてからコンポーネントを設定してください。");
        }

        static bool CheckIsEditorOnly(GameObject go)
        {
            if (go.CompareTag("EditorOnly"))
            {
                return true;
            }

            if (go.transform.parent != null)
            {
                return CheckIsEditorOnly(go.transform.parent.gameObject);
            }

            return false;
        }
    }
}
