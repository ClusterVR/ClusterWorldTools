using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditorInternal;

namespace ClusterWorldTools.Editor.Tool.WebTriggerGenerator
{
    public class WebTriggerGenerator : EditorWindow
    {
        [SerializeField] protected Trigger trigger = new();
        [SerializeField] GameObject gimmick;
        [SerializeField] int stateNum = 0;
        [SerializeField] protected Triggers triggerList = new();

        string jsonFilePath = "";

        protected Vector2 scrollPosition;
        protected bool error = false;

        ReorderableList reorderableList = null;
        ReorderableList stateRordarableList = null;

        [MenuItem("WorldTools/Webトリガー生成/詳細")]
        public static void CreateWindow()
        {
            EditorWindow window = GetWindow<WebTriggerGenerator>();
            while (window.GetType() != typeof(WebTriggerGenerator))
            {
                window.Close();
                window = GetWindow<WebTriggerGenerator>();
            }

            window.titleContent = new GUIContent("Webトリガー編集");
        }

        virtual public void OnGUI()
        {
            if (GUILayout.Button("JSONを読み込む"))
                OpenJson();
            var serialized = new SerializedObject(this);
            serialized.Update();
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                using (var horizontalScope = new GUILayout.HorizontalScope())
                {
                    var gimmickProperty = serialized.FindProperty("gimmick");
                    EditorGUILayout.PropertyField(gimmickProperty, true);

                    if (GUILayout.Button("自動設定")) TriggerFromGimmick();
                }

                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("トリガー情報");
                trigger.category = EditorGUILayout.TextField("Category", trigger.category);
                trigger.displayName = EditorGUILayout.TextField("Display Name", trigger.displayName);
                trigger.showConfirmDialog = EditorGUILayout.Toggle("Show Confirm Dialog", trigger.showConfirmDialog);
                var color = EditorGUILayout.ColorField("Color", new Color(trigger.color[0], trigger.color[1], trigger.color[2]));
                trigger.color = new[] { color.r, color.g, color.b };

                if (stateRordarableList == null)
                {
                    stateRordarableList = new ReorderableList(trigger.state, typeof(State));
                    stateRordarableList.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, "States"); };
                    var height = stateRordarableList.elementHeight;
                    stateRordarableList.elementHeight = height * 3;
                    stateRordarableList.drawElementCallback += (rect, index, isActive, isFocused) =>
                    {
                        var triggerState = trigger.state[index];
                        rect.height /= 3;
                        triggerState.key = EditorGUI.TextField(rect, "Key", triggerState.key);
                        rect.y += height;

                        triggerState.type = ((TriggerParameterType)EditorGUI.EnumPopup(rect, "Type", triggerState.typeEnum)).ToString().ToLower();
                        rect.y += height;
                        switch (triggerState.typeEnum)
                        {
                            case TriggerParameterType.Bool:
                                string[] boolValue = { "true", "false" };
                                triggerState.value = boolValue[EditorGUI.Popup(rect, "Value", triggerState.value == "true" ? 0 : 1, boolValue)];
                                break;
                            case TriggerParameterType.Float:
                                try
                                {
                                    triggerState.value = EditorGUI.FloatField(rect, "Value", (float)Convert.ToDouble(triggerState.value)).ToString();
                                }
                                catch
                                {
                                    triggerState.value = "0.0";
                                }

                                break;
                            case TriggerParameterType.Integer:
                                try
                                {
                                    triggerState.value = EditorGUI.IntField(rect, "Value", Convert.ToInt32(triggerState.value)).ToString();
                                }
                                catch
                                {
                                    triggerState.value = "0";
                                }

                                break;
                        }
                    };
                    stateRordarableList.onAddCallback += (list) => { trigger.state.Add(new State(trigger.state.LastOrDefault() ?? new State())); };
                }

                stateRordarableList.DoLayoutList();

                if (GUILayout.Button($"トリガーを{(triggerList.triggers.Any(t => t.category == trigger?.category && t.displayName == trigger?.displayName) ? "更新" : "追加")}"))
                    GenerateJSON();

                if (error)
                {
                    var defaultColor = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("値が正しいことを確認してください。");
                    GUI.color = defaultColor;
                }

                if (reorderableList == null)
                {
                    reorderableList = new ReorderableList(triggerList.triggers, typeof(Trigger));
                    reorderableList.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, "Triggers"); };
                    reorderableList.onCanAddCallback += (list) => { return false; };
                    reorderableList.drawElementCallback += (rect, index, isActive, isFocused) =>
                    {
                        var t = triggerList.triggers[index];

                        var colorRect = new Rect(rect);
                        colorRect.height -= 1;
                        colorRect.width = colorRect.height;
                        EditorGUI.DrawRect(colorRect, new Color(t.color[0], t.color[1], t.color[2]));
                        rect.xMin += colorRect.width;

                        EditorGUI.LabelField(rect, $"{t.category}/{t.displayName}");
                        rect.x = rect.x + rect.width - 64;
                        rect.width = 64;
                        if (GUI.Button(rect, "編集"))
                        {
                            trigger = new Trigger(triggerList.triggers[index]);
                            stateRordarableList = null;
                        }
                    };
                }

                reorderableList.DoLayoutList();

                scrollPosition = scroll.scrollPosition;
            }

            if (GUILayout.Button("書き出す"))
                SaveJson();

            serialized.ApplyModifiedProperties();
        }

        void TriggerFromGimmick()
        {
            if (gimmick == null) return;

            var gimmickComponents = gimmick.GetComponents<ClusterVR.CreatorKit.Gimmick.IGimmick>();
            if (gimmickComponents.Length == 0) return;

            trigger.category = gimmick.name;
            trigger.displayName = gimmick.name;

            trigger.state.Clear();

            foreach (var gimmickComponent in gimmickComponents)
            {
                if (gimmickComponent.Target != ClusterVR.CreatorKit.Gimmick.GimmickTarget.Global)
                    continue;

                var s = new State() { key = gimmickComponent.Key, type = gimmickComponent.ParameterType.ToString().ToLower() };

                trigger.state.Add(s);
            }
        }

        void GenerateJSON()
        {
            error = false;
            try
            {
                var newTrigger = new Trigger(trigger);
                var alreadyExists = triggerList.triggers.FindIndex(t => t.category == trigger.category && t.displayName == trigger.displayName);
                if (alreadyExists >= 0)
                {
                    triggerList.triggers[alreadyExists] = newTrigger;
                }
                else
                {
                    triggerList.triggers.Add(newTrigger);
                }
            }
            catch
            {
                Debug.LogError("TypeおよびValueに正しい値が入力されていることを確認してください。");
                error = true;
            }
        }

        void OpenJson()
        {
            jsonFilePath = EditorUtility.OpenFilePanel("", "Assets", "json");
            if (jsonFilePath == string.Empty)
                return;

            triggerList = WebTriggerExporter.OpenJson(jsonFilePath);

            reorderableList = null;
        }

        protected void SaveJson()
        {
            jsonFilePath = EditorUtility.SaveFilePanel("", "Assets", $"WebTrigger_{SceneManager.GetActiveScene().name}", "json");
            WebTriggerExporter.ExportJson(jsonFilePath, triggerList);

            ClusterVR.CreatorKit.Editor.Preview.WebTrigger.WebTriggerWindow.ShowWindow();
        }
    }
}