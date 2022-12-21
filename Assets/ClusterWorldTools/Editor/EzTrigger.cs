using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit;
using System.Linq;
using ClusterVR.CreatorKit.Editor.Preview;
using ClusterVR.CreatorKit.Editor.Preview.RoomState;
using System;

namespace ClusterWorldTools
{
    public class EzTriggerWindow : EditorWindow
    {
        private const string TOOL_NAME = "トリガー発行";
        private List<IGimmick> gimmicks = new List<IGimmick>();

        private Vector2 scrollPosition;

        private double value = 0.0;

        [MenuItem("WorldTools/" + TOOL_NAME)]
        static public void CreateWindow()
        {
            GetWindow<EzTriggerWindow>(TOOL_NAME);
        }
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                GUILayout.Label(TOOL_NAME + "はプレビュー中のみ有効です。");
                return;
            }

            if(gimmicks.Count == 0)
            {
                GUILayout.Label("Hierarchyでギミックを選択してください。");
                return;
            }

            GUILayout.Label(Selection.activeObject.name, EditorStyles.boldLabel);

            using (var scroll = new GUILayout.ScrollViewScope(scrollPosition))
            {
                foreach (var gimmick in gimmicks)
                {
                    EditorGUILayout.LabelField(gimmick.Key);

                    switch (gimmick.ParameterType)
                    {
                        case ParameterType.Signal:
                            if (GUILayout.Button("Fire"))
                            {
                                if (!Bootstrap.SignalGenerator.TryGet(out var signal)) return;
                                FireGimmick(gimmick, signal);
                            }
                            break;
                        case ParameterType.Bool:
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("True"))
                                {
                                    FireGimmick(gimmick, new StateValue(true));
                                }
                                if (GUILayout.Button("False"))
                                {
                                    FireGimmick(gimmick, new StateValue(false));
                                }
                            }
                            break;
                        case ParameterType.Double:
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                value = EditorGUILayout.DoubleField(value);
                                if (GUILayout.Button("Update"))
                                {
                                    FireGimmick(gimmick, new StateValue(value));
                                }
                            }
                            break;
                        case ParameterType.Float:
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                value = EditorGUILayout.FloatField((float)value);
                                if (GUILayout.Button("Update"))
                                {
                                    FireGimmick(gimmick, new StateValue(value));
                                }
                            }
                            break;
                        case ParameterType.Integer:
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                value = EditorGUILayout.IntField((int)value);
                                if (GUILayout.Button("Update"))
                                {
                                    FireGimmick(gimmick, new StateValue(value));
                                }
                            }
                            break;
                    }

                    scrollPosition = scroll.scrollPosition;
                }
            }
        }

        private string GetStateKey(IGimmick gimmick)
        {
            switch (gimmick.Target)
            {
                case GimmickTarget.Global:
                    return RoomStateKey.GetGlobalKey(gimmick.Key);
                case GimmickTarget.Item:
                    return RoomStateKey.GetItemKey(gimmick.ItemId.Value, gimmick.Key);
                case GimmickTarget.Player:
                    return RoomStateKey.GetPlayerKey(gimmick.Key);
                default:
                    throw new NotImplementedException();
            }
        }

        private void FireGimmick(IGimmick gimmick, StateValue value)
        {
            var key = GetStateKey(gimmick);
            Bootstrap.RoomStateRepository.Update(key, value);
            Bootstrap.GimmickManager.OnStateUpdated(new string[] { key });
        }

        private void UpdateTriggers()
        {
            gimmicks.Clear();

            if (Selection.activeGameObject == null) return;

            var gimmickComponents = Selection.activeGameObject.GetComponents<IGimmick>();
            foreach (var gimmick in gimmickComponents)
            {
                if (gimmick.ParameterType != ParameterType.Signal
                    && gimmick.ParameterType != ParameterType.Bool
                    && gimmick.ParameterType != ParameterType.Double
                    && gimmick.ParameterType != ParameterType.Float
                    && gimmick.ParameterType != ParameterType.Integer)
                {
                    continue;
                }
                if (gimmicks.Exists(x => x.Key == gimmick.Key)) continue;
                gimmicks.Add(gimmick);
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    OnSelectionChanged();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    OnSelectionChanged();
                    break;
            }
        }
        private void OnSelectionChanged()
        {
            var objs = Resources.FindObjectsOfTypeAll(typeof(EzTriggerWindow));
            if (objs.Length == 0) return;

            var window = (EzTriggerWindow)objs[0];
            window.UpdateTriggers();
            window.Repaint();
        }
    }
}