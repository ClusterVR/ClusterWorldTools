using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;

namespace ClusterWorldTools
{
    class GameObjectIdList
    {
        [SerializeField] List<int> list = new List<int>();

        public void Initialize<T>() where T : Component
        {
            list = GameObject.FindObjectsOfType<T>().Select(o => o.gameObject.GetInstanceID()).ToList();
        }

        public bool CheckAndAdd(GameObject gameObject)
        {
            if (list.Contains(gameObject.GetInstanceID())) return true;

            list.Add(gameObject.GetInstanceID());
            return false;
        }

        public void RemoveDeletedMembers()
        {
            list = list.Where(id => EditorUtility.InstanceIDToObject(id) != null).ToList();
        }
    }
}