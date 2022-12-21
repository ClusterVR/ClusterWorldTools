using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ClusterWorldTools
{
    public class KeySearch : EditorWindow
    {
        class Key : System.IComparable<Key>
        {
            public string key;

            public class GameObjectAndType : System.IEquatable<GameObjectAndType>, System.IComparable<GameObjectAndType>
            {
                public GameObject gameObject;
                public System.Type type;

                public int CompareTo(GameObjectAndType other)
                {
                    var compare = string.Compare(gameObject.name, other.gameObject.name);
                    if (compare != 0) return compare;
                    return gameObject == other.gameObject ? string.Compare(type.Name, other.type.Name) : gameObject.GetInstanceID() > other.gameObject.GetInstanceID() ? 1 : -1;
                }

                public bool Equals(GameObjectAndType other)
                {
                    return gameObject == other.gameObject && type == other.type;
                }

                public override int GetHashCode()
                {
                    return (gameObject?.GetHashCode() ?? 0) ^ (type?.GetHashCode() ?? 0);
                }
            }
            public SortedSet<GameObjectAndType> triggers, gimmicks;

            public int CompareTo(Key other)
            {
                return string.Compare(key, other.key);
            }
        }
        SortedSet<Key> keys;

        string searchText = "";
        bool showTriggers = true, showGimmicks = true;

        Key selected;

        Vector2 keysScrollPosition, gameobjectsScrollPosition;

        [MenuItem("WorldTools/Key検索")]
        static public void CreateWindow()
        {
            EditorWindow window = GetWindow<KeySearch>();
            window.titleContent = new GUIContent("Key検索");
        }

        public void OnGUI()
        {
            if (keys == null)
            {
                keys = new SortedSet<Key>();
                Search();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                var buttonStyle = EditorStyles.miniButton;
                buttonStyle.alignment = TextAnchor.MiddleLeft;

                using (var scope = new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.5f), GUILayout.ExpandWidth(false)))
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(keysScrollPosition))
                    {
                        var style = EditorStyles.largeLabel;
                        style.fixedWidth = scope.rect.width;
                        EditorGUILayout.LabelField("Keys", style);

                        searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
                        var filteredKeys = searchText == "" ? keys : keys.Where(key => key.key.Contains(searchText, System.StringComparison.OrdinalIgnoreCase));

                        var defaultColor = GUI.backgroundColor;
                        foreach (var key in filteredKeys)
                        {
                            GUI.backgroundColor = selected == key ? Color.blue : defaultColor;
                            if (GUILayout.Button(key.key, buttonStyle)) selected = key;
                        }
                        GUI.backgroundColor = defaultColor;

                        keysScrollPosition = scroll.scrollPosition;
                    }
                }
                using (var scope = new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.5f), GUILayout.ExpandWidth(false)))
                {
                    if (selected != null)
                    {
                        using (var scroll = new EditorGUILayout.ScrollViewScope(gameobjectsScrollPosition))
                        {
                            var style = EditorStyles.largeLabel;
                            style.fixedWidth = scope.rect.width;
                            EditorGUILayout.LabelField($"Key=\"{selected.key}\"", style);
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                showTriggers = GUILayout.Toggle(showTriggers, "Triggers");
                                showGimmicks = GUILayout.Toggle(showGimmicks, "Gimmicks");
                            }

                            var gameObjectsInSelectedKey = new SortedSet<Key.GameObjectAndType>();
                            if (showTriggers) gameObjectsInSelectedKey.UnionWith(selected.triggers);
                            if (showGimmicks) gameObjectsInSelectedKey.UnionWith(selected.gimmicks);

                            foreach (var p in gameObjectsInSelectedKey)
                            {
                                if (GUILayout.Button($"{p.gameObject.name} / {p.type.Name}", buttonStyle)) Selection.activeObject = p.gameObject;
                            }
                            gameobjectsScrollPosition = scroll.scrollPosition;
                        }
                    }
                }
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh")))
            {
                selected = null;
                Search();
            }
        }

        public void Search()
        {
            keys.Clear();
            var allObjects = FindObjectsOfType<GameObject>(true);

            foreach (var gameObject in allObjects)
            {
                var triggerComponents = gameObject.GetComponents<ClusterVR.CreatorKit.Trigger.ITrigger>();
                if (triggerComponents != null)
                {
                    foreach (var triggerComponent in triggerComponents)
                    {
                        foreach (var triggerParam in triggerComponent.TriggerParams) AddKeyTrigger(triggerParam.RawKey, gameObject, triggerComponent.GetType());
                    }
                }

                var gimmickComponents = gameObject.GetComponents<ClusterVR.CreatorKit.Gimmick.IGimmick>();
                if(gimmickComponents!=null)
                {
                    foreach (var gimmickComponent in gimmickComponents) AddKeyGimmick(gimmickComponent.Key, gameObject, gimmickComponent.GetType());
                }
            }
        }

        Key GetKey(string key)
        {
            var existKey = keys.Where(x => x.key == key).FirstOrDefault();
            if (existKey == null)
            {
                var newKey = new Key();
                newKey.key = key;
                newKey.triggers = new SortedSet<Key.GameObjectAndType>();
                newKey.gimmicks = new SortedSet<Key.GameObjectAndType>();
                keys.Add(newKey);
                existKey = newKey;
            }
            return existKey;
        }

        void AddKeyTrigger(string key, GameObject gameObject, System.Type type)
        {
            var existKey = GetKey(key);

            if (existKey.triggers.Where(x => x.gameObject == gameObject && x.type == type).Count() == 0)
            {
                var _gameObject = new Key.GameObjectAndType();
                _gameObject.gameObject = gameObject;
                _gameObject.type = type;
                existKey.triggers.Add(_gameObject);
            }
        }
        void AddKeyGimmick(string key, GameObject gameObject, System.Type type)
        {
            var existKey = GetKey(key);

            if (existKey.gimmicks.Where(x => x.gameObject == gameObject && x.type == type).Count() == 0)
            {
                var _gameObject = new Key.GameObjectAndType();
                _gameObject.gameObject = gameObject;
                _gameObject.type = type;
                existKey.gimmicks.Add(_gameObject);
            }
        }
    }
}
