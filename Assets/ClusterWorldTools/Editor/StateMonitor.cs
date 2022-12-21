using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using ClusterVR.CreatorKit.Editor.Preview.RoomState;
using ClusterVR.CreatorKit.Editor.Preview;
using ClusterVR.CreatorKit;
using ClusterVR.CreatorKit.Item;
using System.Linq;
using UnityEngine.SceneManagement;

namespace ClusterWorldTools
{
    public class StateMonitor : EditorWindow
    {
        RoomStateRepository roomStateRepository;
        Dictionary<string, StateValue> values;
        string searchText = "";
        [SerializeField] GameObject searchItem;

        Vector2 scrollPosition;

        [MenuItem("WorldTools/トリガーモニター")]
        static public void CreateWindow()
        {
            EditorWindow window = GetWindow<StateMonitor>();
            window.titleContent = new GUIContent("トリガーモニター");
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);

            SerializedObject serialized = new SerializedObject(this);
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("searchItem"), new GUIContent("アイテムで検索"));
            serialized.ApplyModifiedProperties();
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {

                if (EditorApplication.isPlaying) ShowState();
                else EditorGUILayout.LabelField("トリガーモニターはプレビュー中のみ有効です。");

                scrollPosition = scroll.scrollPosition;
            }
        }

        private void ShowState()
        {
            EditorGUILayout.LabelField("トリガーの内容を表示します。");
            if (roomStateRepository != Bootstrap.RoomStateRepository)
            {
                var field = Bootstrap.RoomStateRepository.GetType().GetField("values", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                values = (Dictionary<string, StateValue>)field.GetValue(Bootstrap.RoomStateRepository);
                if (values != null)
                {
                    roomStateRepository = Bootstrap.RoomStateRepository;
                }
            }
            if (values == null) return;

            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var items = rootGameObjects.SelectMany(x => x.GetComponentsInChildren<IItem>(true)).ToDictionary(x => x.Id.Value);

            foreach (var pair in values)
            {
                var arr = pair.Key.Split(new char[] { '.' }, 2);
                var stateType = arr[0];

                var style = new GUIStyle(EditorStyles.label);
                if (System.DateTime.Now - pair.Value.ToDateTime().ToLocalTime() < System.TimeSpan.FromSeconds(1.0))
                {
                    style.normal.textColor = Color.green;
                    style.hover.textColor = Color.green;
                }

                if (stateType == "_g")
                {
                    var stateName = arr[1];
                    if (searchItem != null) continue;
                    if (searchText != "" && !stateName.Contains(searchText)) continue;
                    GUILayout.Label($"[Global] {stateName}: {pair.Value.ToDouble().ToString()}", style);
                }
                else if (stateType == "_i")
                {
                    arr = arr[1].Split(new char[] { '.' }, 2);
                    var itemId = ulong.Parse(arr[0]);
                    var stateName = arr[1];
                    var item = items.ContainsKey(itemId) ? items[itemId] : null;
                    if (item == null || item.gameObject == null) continue;
                    if (searchText != "" && !stateName.Contains(searchText) && !item.ItemName.Contains(searchText)) continue;
                    if (searchItem != null)
                    {
                        IItem _searchItem = searchItem.GetComponent<IItem>();
                        if (_searchItem != null && !_searchItem.Id.Equals(item.Id)) continue;
                    }
                    GUILayout.Label($"{item.gameObject.name} ({item.ItemName})", style);
                    GUILayout.Label($"[Item] {stateName}: {pair.Value.ToDouble().ToString()}", style);
                }
                else if (stateType == "_p")
                {
                    var stateName = arr[1];
                    if (searchItem != null) continue;
                    if (searchText != "" && !stateName.Contains(searchText)) continue;
                    GUILayout.Label($"[Player] {stateName}: {pair.Value.ToDouble().ToString()}", style);
                }
            }
        }
    }
}