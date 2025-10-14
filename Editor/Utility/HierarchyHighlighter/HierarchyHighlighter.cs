using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace ClusterWorldTools.Editor.Utility.HierarchyHighlighter
{
    public class HierarchyHighlighter
    {
        const float iconWidth = 16f;

        static List<Type> visualizeComponentTypes = new()
        {
            typeof(Collider),
            typeof(Renderer),
            typeof(Light),
            typeof(AudioSource),
            typeof(Canvas),
            typeof(VideoPlayer),
            typeof(Animator),
            typeof(PlayableDirector),
            typeof(ClusterVR.CreatorKit.Gimmick.IGimmick),
            typeof(ClusterVR.CreatorKit.Trigger.ITrigger),
            typeof(ClusterVR.CreatorKit.Item.Implements.Item),
            typeof(ClusterVR.CreatorKit.World.ISpawnPoint),
            typeof(ClusterVR.CreatorKit.World.IDespawnHeight),
            typeof(ClusterVR.CreatorKit.World.IWorldGate),
            typeof(ClusterVR.CreatorKit.Gimmick.Supplements.PlayableSwitch),
        };

        static Dictionary<Type, Texture> specialIcons = new()
        {
        };

        public delegate bool EditorOnlyChecker(GameObject gameObject);

        static List<EditorOnlyChecker> editorOnlyCheckers = new() { CheckEditorOnlyTag };

        public struct HierarchyDrawContext
        {
            public GameObject gameObject;
            public Rect selectionRect;
            public bool isFolding;
            public bool isHovered;
            public bool isSelected;
            public Rect drawRect;
            public Rect backgroundRect;
            public Rect overlayRect;
            public GameObjectInfo gameObjectInfo;
        }
        public delegate void HierarchyItemDrawAction(HierarchyDrawContext context);
        static event HierarchyItemDrawAction additionalHierarchyDrawActions;

        static readonly Color editorOnlyOverlayColor = new Color(1f, 0.2f, 0f, 0.2f);

        static Type sceneHierarchyWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static PropertyInfo sceneHierarchyWindow = sceneHierarchyWindowType.GetProperty("lastInteractedHierarchyWindow", BindingFlags.Public | BindingFlags.Static);

        public class GameObjectInfo
        {
            public List<Texture> componentIcons = null;
            public List<Texture> childrenIcons = null;
            public List<string> childrenTags = null;
            public LayerMask childrenLayers = 0;
        }
        static Dictionary<GameObject, GameObjectInfo> gameObjectInfos = new();

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            HierarchyHighlighterSettings.instance.LoadSettings();
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyChanged += Refresh;
        }

        public static void AddVisualizeComponentType(Type componentType, Texture icon = null)
        {
            if (componentType == null) return;

            if (!visualizeComponentTypes.Contains(componentType))
            {
                visualizeComponentTypes.Add(componentType);

                if (icon != null)
                {
                    specialIcons[componentType] = icon;
                }

                Refresh();
            }
        }

        public static void AddVisualizeComponentTypes(IEnumerable<Type> componentTypes)
        {
            bool changed = false;
            foreach (var componentType in componentTypes)
            {
                if (componentType != null && !visualizeComponentTypes.Contains(componentType))
                {
                    visualizeComponentTypes.Add(componentType);
                    changed = true;
                }
            }

            if (changed)
            {
                Refresh();
            }
        }

        public static void AddVisualizeComponentTypeByName(string typeName, Texture icon = null)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                AddVisualizeComponentType(type, icon);
            }
        }

        public static void AddEditorOnlyChecker(EditorOnlyChecker checker)
        {
            if (checker != null && !editorOnlyCheckers.Contains(checker))
            {
                editorOnlyCheckers.Add(checker);
            }
        }

        public static void RegisterAdditionalHierarchyDrawAction(HierarchyItemDrawAction action)
        {
            if (action != null)
            {
                additionalHierarchyDrawActions += action;
            }
        }

        public static void UnregisterAdditionalHierarchyDrawAction(HierarchyItemDrawAction action)
        {
            if (action != null)
            {
                additionalHierarchyDrawActions -= action;
            }
        }

        static bool CheckEditorOnlyTag(GameObject gameObject)
        {
            return gameObject.CompareTag("EditorOnly");
        }

        static void Refresh()
        {
            if (EditorApplication.isPlaying) return;

            gameObjectInfos.Clear();
            EditorApplication.RepaintHierarchyWindow();
        }

        static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (selectionRect.yMin == 0f)
            {
                var toggleRect = new Rect(selectionRect.x + selectionRect.width - iconWidth, selectionRect.y, iconWidth, selectionRect.height);
                HierarchyHighlighterSettings.instance.ShowComponents = EditorGUI.Toggle(toggleRect, HierarchyHighlighterSettings.instance.ShowComponents);
                selectionRect.xMax -= iconWidth;
            }

            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            var isFolding = CheckIsFolding(instanceID, gameObject);
            var isHovered = CheckIsHovered(selectionRect);
            var isSelected = CheckIsSelected(instanceID);

            var rect = GetDrawArea(selectionRect);
            var backgroundRect = GetBackgroundArea(selectionRect);
            var overlayRect = rect;

            var gameObjectInfo = GetGameObjectInfo(gameObject);

            var isEditorOnly = editorOnlyCheckers.Any(checker => checker(gameObject));
            if (isEditorOnly)
            {
                DrawBackground(overlayRect, editorOnlyOverlayColor);
            }

            if (additionalHierarchyDrawActions != null)
            {
                var context = new HierarchyDrawContext
                {
                    gameObject = gameObject,
                    selectionRect = selectionRect,
                    isFolding = isFolding,
                    isHovered = isHovered,
                    isSelected = isSelected,
                    drawRect = rect,
                    backgroundRect = backgroundRect,
                    overlayRect = overlayRect,
                    gameObjectInfo = gameObjectInfo
                };

                try
                {
                    additionalHierarchyDrawActions.Invoke(context);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            if (HierarchyHighlighterSettings.instance.ShowTagsAndLayers)
            {
                foreach (var tagSetting in HierarchyHighlighterSettings.instance.GetTagsSettings())
                {
                    if (!UnityEditorInternal.InternalEditorUtility.tags.Contains(tagSetting.name))
                        continue;
                    if (gameObject.CompareTag(tagSetting.name) || (isFolding && gameObjectInfo.childrenTags.Contains(tagSetting.name)))
                    {
                        backgroundRect = DrawBackground(backgroundRect, tagSetting.color);
                    }
                }

                LayerMask layer = (1 << gameObject.layer);
                if (isFolding)
                    layer |= gameObjectInfo.childrenLayers;
                foreach (var layerSetting in HierarchyHighlighterSettings.instance.GetLayersSettings())
                {
                    if ((layer & (1 << LayerMask.NameToLayer(layerSetting.name))) != 0)
                    {
                        backgroundRect = DrawBackground(backgroundRect, layerSetting.color);
                    }
                }
            }

            if (HierarchyHighlighterSettings.instance.ShowComponents)
            {
                foreach (var image in gameObjectInfo.componentIcons)
                {
                    rect = DrawIcon(rect, image);
                }
                if (isFolding)
                {
                    foreach (var image in gameObjectInfo.childrenIcons)
                    {
                        rect = DrawIcon(rect, image);
                    }
                }
            }
        }

        static Rect GetDrawArea(Rect selectionRect)
        {
            var rect = selectionRect;
            rect.xMin += iconWidth;
            return rect;
        }

        static Rect GetBackgroundArea(Rect selectionRect)
        {
            var rect = selectionRect;
            rect.xMax = rect.xMin - iconWidth;
            rect.xMin = 32f;
            return rect;
        }

        static GameObjectInfo GetGameObjectInfo(GameObject gameObject)
        {
            if (gameObjectInfos.TryGetValue(gameObject, out var info))
            {
                return info;
            }

            var gameObjectInfo = new GameObjectInfo();

            var components = gameObject.GetComponents<Component>();
            var visualizeComponents = components.Where(component =>
            {
                if (component == null)
                {
                    return true;
                }

                return visualizeComponentTypes.Any(type =>
                {
                    var componentType = component.GetType();
                    return type.IsAssignableFrom(componentType);
                });
            });
            var componentIcons = visualizeComponents.Select(component => GetComponentIcon(component)).Distinct().ToList();
            gameObjectInfo.componentIcons = componentIcons;

            var childrenIcons = new List<Texture>();
            var childrenTags = new List<string>();
            LayerMask childrenLayers = 0;
            foreach (Transform child in gameObject.transform)
            {
                var childInfo = GetGameObjectInfo(child.gameObject);
                childrenIcons.AddRange(childInfo.componentIcons);
                childrenIcons.AddRange(childInfo.childrenIcons);
                childrenTags.Add(child.tag);
                childrenTags.AddRange(childInfo.childrenTags);
                childrenLayers |= (1 << child.gameObject.layer);
                childrenLayers |= childInfo.childrenLayers;
            }

            gameObjectInfo.childrenIcons = childrenIcons.Distinct().ToList();
            gameObjectInfo.childrenTags = childrenTags.Distinct().ToList();
            gameObjectInfo.childrenLayers = childrenLayers;

            gameObjectInfos[gameObject] = gameObjectInfo;
            return gameObjectInfo;
        }

        static Texture GetComponentIcon(Component component)
        {
            if (component == null)
            {
                return EditorGUIUtility.IconContent("console.warnicon.sml").image;
            }
            var matchedSpecialIcons = specialIcons.Where(pair => pair.Key.IsAssignableFrom(component.GetType())).ToList();
            if (matchedSpecialIcons.Any())
            {
                return matchedSpecialIcons.First().Value;
            }

            return AssetPreview.GetMiniThumbnail(component);
        }

        static bool CheckIsFolding(int instanceID, GameObject gameObject)
        {
            if (gameObject.transform.childCount == 0) return false;

            int[] expandedIDs = (int[])sceneHierarchyWindowType.GetMethod("GetExpandedIDs", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sceneHierarchyWindow.GetValue(null), null);
            return !expandedIDs.Contains(instanceID);
        }

        static bool CheckIsHovered(Rect rect)
        {
            rect.xMin = 0;
            return rect.Contains(Event.current.mousePosition);
        }

        static bool CheckIsSelected(int instanceID)
        {
            return Selection.Contains(instanceID);
        }

        static Rect DrawBackground(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            rect.xMin = Mathf.Min(rect.xMin + 2f, rect.xMax);
            rect.yMin = Mathf.Min(rect.yMin + 1f, rect.yMax);
            return rect;
        }

        static void DrawName(Rect rect, GameObject gameObject)
        {
            EditorGUI.LabelField(rect, gameObject.name);
        }

        static Rect DrawIcon(Rect rect, Texture iconTexture)
        {
            if (iconTexture == null)
                return rect;

            var iconRect = new Rect(rect.x + rect.width - iconWidth, rect.y, iconWidth, rect.height);
            GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit, true);
            rect.xMax -= iconWidth;
            return rect;
        }

        static Rect DrawIcon(Rect rect, GUIContent icon)
        {
            return DrawIcon(rect, icon.image);
        }

        static Rect DrawIcon(Rect rect, string iconName)
        {
            return DrawIcon(rect, EditorGUIUtility.IconContent(iconName));
        }

    }
}
