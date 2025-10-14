using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.Linq;
using ClusterWorldTools.Editor.Common;

namespace ClusterWorldTools.Editor.Tool
{
    public sealed class FavoriteListWindow : EditorWindow
    {
        static System.Type cachedProjectWindowType;
        static MethodInfo cachedSetFolderSelectionMethod;

        static System.Type ProjectWindowType => cachedProjectWindowType ??= Assembly.Load("UnityEditor").GetType("UnityEditor.ProjectBrowser");
        static MethodInfo SetFolderSelectionMethod => cachedSetFolderSelectionMethod ??= ProjectWindowType?.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "SetFolderSelection" && m.GetParameters().Length == 2);

        const string SettingKey = "FavoriteAssetList";
        const int HistoryMaxSize = 10;
        readonly string[] tabTexts = { "お気に入り", "履歴" };

        List<Object> favorites = new List<Object>();
        ReorderableList favoritesReordalableList = null;

        List<Object> history = new List<Object>();
        Object select = null;

        int selectedTab = 0;

        Vector2 scrollPositionInFavorite, scrollPositionInHistory;

        [MenuItem("WorldTools/クイックアクセス")] static public void CreateWindow()
        {
            EditorWindow window = GetWindow<FavoriteListWindow>();
            window.titleContent = new GUIContent("クイックアクセス");
        }

        private void OnEnable()
        {
            var savedFavorites = EditorSettingUtil.LoadSetting(SettingKey, null, (s => s.Split(',').Select(s => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(new GUID(s)), typeof(Object))).Where(o => o != null).ToList()));
            if (savedFavorites != null) favorites = savedFavorites;
        }

        bool TryJumpToAsset(Object obj)
        {
            try
            {
                if (ProjectWindowType == null || SetFolderSelectionMethod == null)
                {
                    Debug.LogError("Projectウィンドウへのアクセスに失敗しました");
                    return false;
                }

                int[] folders = { obj.GetInstanceID() };
                var projectWindow = GetWindow(ProjectWindowType, false, "Project", false);
                SetFolderSelectionMethod.Invoke(projectWindow, new System.Object[] { folders, false });
                projectWindow.Focus();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Projectウィンドウの操作に失敗しました。{ex.Message}");
                return false;
            }
        }

        void JumpToAsset(Object obj)
        {
            if (!TryJumpToAsset(obj))
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }

        GUIContent AssetButtonLabel(Object obj, bool isFolder)
        {
            var label = isFolder ? EditorGUIUtility.IconContent("Folder Icon") : EditorGUIUtility.IconContent("TextAsset Icon");
            label.text = obj.name;
            label.tooltip = AssetDatabase.GetAssetPath(obj);

            return label;
        }

        GUIStyle AssetButtonStyle(Object obj, bool isFolder)
        {
            var style = new GUIStyle(EditorStyles.miniButton);
            style.alignment = TextAnchor.MiddleLeft;
            if (isFolder)
            {
                style.fontStyle = FontStyle.Bold;
            }

            return style;
        }

        bool AssetButtonLayout(Object obj)
        {
            var isFolder = ProjectWindowUtil.IsFolder(obj.GetInstanceID());
            return GUILayout.Button(AssetButtonLabel(obj, isFolder), AssetButtonStyle(obj, isFolder));
        }

        bool AssetButton(Rect rect, Object obj)
        {
            var isFolder = ProjectWindowUtil.IsFolder(obj.GetInstanceID());
            if (obj)
            {
                return GUI.Button(rect, AssetButtonLabel(obj, isFolder), AssetButtonStyle(obj, isFolder));
            }
            else
            {
                GUI.Label(rect, "Missing", new GUIStyle(EditorStyles.miniButton));
                return false;
            }
        }

        void SaveFavoriteList()
        {
            EditorSettingUtil.SaveSetting(SettingKey, string.Join(",", favorites.Select(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)))));
        }

        void OnGUI()
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        var success = false;

                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(draggedObject)))
                            {
                                favorites.Add(draggedObject);
                                success = true;
                            }
                        }
                        DragAndDrop.activeControlID = 0;

                        if (success)
                        {
                            SaveFavoriteList();
                            selectedTab = 0;
                        }
                    }
                    Event.current.Use();

                    break;
            }

            selectedTab = GUILayout.Toolbar(selectedTab, tabTexts);
            switch (selectedTab)
            {
                case 0:
                    using (var scope = new EditorGUILayout.ScrollViewScope(scrollPositionInFavorite))
                    {
                        if (favoritesReordalableList == null)
                        {
                            favoritesReordalableList = new ReorderableList(favorites, typeof(Object));
                            favoritesReordalableList.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, "お気に入り"); };
                            favoritesReordalableList.onCanAddCallback += (list) => { return false; };
                            favoritesReordalableList.onCanRemoveCallback += (list) => { return favorites.Count > 0; };
                            favoritesReordalableList.onChangedCallback += (list) =>
                            {
                                SaveFavoriteList();
                            };
                            favoritesReordalableList.elementHeight = EditorStyles.miniButton.fixedHeight;
                            favoritesReordalableList.drawElementCallback += (rect, index, isActive, isFocused) =>
                            {
                                var obj = favorites[index];
                                if (AssetButton(rect, obj))
                                {
                                    JumpToAsset(obj);
                                }

                            };
                        }
                        favoritesReordalableList.DoLayoutList();
                        scrollPositionInFavorite = scope.scrollPosition;
                    }
                    break;
                case 1:
                    using (var scope = new EditorGUILayout.ScrollViewScope(scrollPositionInHistory))
                    {
                        GUILayout.Label($"履歴（最新{HistoryMaxSize}件）");
                        foreach (Object obj in history)
                        {
                            if (AssetButtonLayout(obj))
                            {
                                JumpToAsset(obj);
                            }
                        }

                        scrollPositionInHistory = scope.scrollPosition;
                    }
                    break;
            }
        }

        void Update()
        {
            if (Selection.activeObject != null && Selection.activeObject != select)
            {
                select = Selection.activeObject;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(select))) return;

                history.Remove(select);
                history.Insert(0, select);
                if(history.Count > HistoryMaxSize) {
                    history.RemoveAt(history.Count - 1);
                }
                Repaint();
            }
        }
    }
}
