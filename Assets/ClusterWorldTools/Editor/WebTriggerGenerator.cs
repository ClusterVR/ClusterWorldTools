using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;

namespace ClusterWorldTools
{
    public class WebTriggerGenerator : EditorWindow
    {
        protected enum ParameterType
        {
            Signal,
            Bool,
            Integer,
            Float
        }

        [System.Serializable]
        protected class State
        {
            [SerializeField] public string key;

            [SerializeField] public ParameterType type;
            [SerializeField] public string value;
            public State(string _key, ClusterVR.CreatorKit.ParameterType _type)
            {
                key = _key;

                switch(_type)
                {
                    case ClusterVR.CreatorKit.ParameterType.Bool:
                        type = ParameterType.Bool;
                        value = "true";
                        break;
                    case ClusterVR.CreatorKit.ParameterType.Integer:
                        type = ParameterType.Integer;
                        value = "0";
                        break;
                    case ClusterVR.CreatorKit.ParameterType.Float:
                        type = ParameterType.Float;
                        value = "0.0";
                        break;
                    default:
                        type = ParameterType.Signal;
                        value = "";
                        break;
                }
            }
        }

        [System.Serializable]
        protected class StateJSON
        {
            [SerializeField] public string key;
            [SerializeField] public string type;
            [SerializeField] public string value;
        }

        [System.Serializable]
        protected class Trigger
        {
            [SerializeField] public string category;
            [SerializeField] public bool showConfirmDialog;
            [SerializeField] public string displayName;
            [SerializeField] public Color color;
            public Trigger()
            {
                category = "";
                showConfirmDialog = false;
                displayName = "";
                color = Color.gray;
            }
            public Trigger(TriggerJSON json)
            {
                category = json.category;
                showConfirmDialog = json.showConfirmDialog;
                displayName = json.displayName;
                color.r = json.color[0];
                color.g = json.color[1];
                color.b = json.color[2];
            }
        }

        [System.Serializable]
        protected class TriggerJSON
        {
            [SerializeField] public string category;
            [SerializeField] public bool showConfirmDialog;
            [SerializeField] public string displayName;
            [SerializeField] public StateJSON[] state;
            [SerializeField] public float[] color;

            public TriggerJSON(Trigger trigger, List<State> triggerState)
            {
                category = trigger.category;
                showConfirmDialog = trigger.showConfirmDialog;
                displayName = trigger.displayName;

                color = new float[3];
                color[0] = trigger.color.r;
                color[1] = trigger.color.g;
                color[2] = trigger.color.b;

                state = new StateJSON[triggerState.Count];
                for (int i = 0; i < triggerState.Count; i++)
                {
                    var ts = triggerState[i];
                    state[i] = new StateJSON();
                    state[i].key = ts.key;
                    state[i].type = ts.type.ToString().ToLower();
                    state[i].value = ts.value;
                }
            }
        }

        [System.Serializable]
        protected class TriggerJSONList
        {
            [SerializeField] public List<TriggerJSON> triggers = new List<TriggerJSON>();
        }

        [SerializeField] protected Trigger trigger = new Trigger();
        [SerializeField] protected List<State> state = new List<State>();
        [SerializeField] protected GameObject gimmick;
        [SerializeField] protected int stateNum = 0;

        protected bool error = false;

        [SerializeField]protected TriggerJSONList triggerList = new TriggerJSONList();
        protected string jsonFilePath = "";

        protected Vector2 scrollPosition;

        protected ReorderableList reorderableList = null;
        ReorderableList stateRordarableList = null;

        [MenuItem("WorldTools/Webトリガー生成/詳細")]
        static public void CreateWindow()
        {
            EditorWindow window = GetWindow<WebTriggerGenerator>();
            while (window.GetType() != typeof(WebTriggerGenerator))
            {
                window.Close();
                window = GetWindow<WebTriggerGenerator>();
            }
            window.titleContent = new GUIContent("Webトリガー生成（詳細）");
        }

        virtual public void OnGUI()
        {
            if (GUILayout.Button("JSONを読み込む")) OpenJson();
            SerializedObject serialized = new SerializedObject(this);
            serialized.Update();
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                using (var horizontalScope = new GUILayout.HorizontalScope())
                {
                    SerializedProperty gimmickProperty = serialized.FindProperty("gimmick");
                    EditorGUILayout.PropertyField(gimmickProperty, true);

                    if (GUILayout.Button("自動設定")) TriggerFromGimmick();
                }

                SerializedProperty triggerProperty = serialized.FindProperty("trigger");
                EditorGUILayout.PropertyField(triggerProperty, true);

                if (stateRordarableList == null)
                {
                    stateRordarableList = new ReorderableList(state, typeof(State));
                    stateRordarableList.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, "States"); };
                    float height = stateRordarableList.elementHeight;
                    stateRordarableList.elementHeight = height * 3;
                    stateRordarableList.drawElementCallback += (rect, index, isActive, isFocused) =>
                    {
                        var s = state[index];
                        rect.height /= 3;
                        s.key = EditorGUI.TextField(rect, "Key", s.key);
                        rect.y += height;
                        s.type = (ParameterType)EditorGUI.EnumPopup(rect, "Type", s.type);
                        rect.y += height;
                        switch(s.type)
                        {
                            case ParameterType.Bool:
                                string[] boolValue = { "true", "false" };
                                s.value = boolValue[EditorGUI.Popup(rect, "Value", s.value == "true" ? 0 : 1, boolValue)];
                                break;
                            case ParameterType.Float:
                                try
                                {
                                    s.value = EditorGUI.FloatField(rect, "Value", (float)Convert.ToDouble(s.value)).ToString();
                                }
                                catch
                                {
                                    s.value = "0.0";
                                }
                                break;
                            case ParameterType.Integer:
                                try
                                {
                                    s.value = EditorGUI.IntField(rect, "Value", Convert.ToInt32(s.value)).ToString();
                                }
                                catch
                                {
                                    s.value = "0";
                                }
                                break;
                        }
                    };
                    stateRordarableList.onAddCallback += (list) =>
                    {
                        state.Add(new State("", ClusterVR.CreatorKit.ParameterType.Signal));
                    };
                }
                stateRordarableList.DoLayoutList();

                if (GUILayout.Button("トリガーを追加")) GenerateJSON();

                if (error)
                {
                    Color defaultColor = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("値が正しいことを確認してください。");
                    GUI.color = defaultColor;
                }

                if (reorderableList == null)
                {
                    reorderableList = new ReorderableList(triggerList.triggers, typeof(TriggerJSON));
                    reorderableList.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, "Triggers"); };
                    reorderableList.onCanAddCallback += (list) => { return false; };
                    reorderableList.drawElementCallback += (rect, index, isActive, isFocused) =>
                    {
                        var t = triggerList.triggers[index];
                        EditorGUI.LabelField(rect, $"{t.category}/{t.displayName}");
                        rect.x = rect.x + rect.width - 64;
                        rect.width = 64;
                        if(GUI.Button(rect, "編集"))
                        {
                            trigger = new Trigger(triggerList.triggers[index]);
                            state = new List<State>();
                            stateRordarableList = null;
                            foreach (var s in triggerList.triggers[index].state)
                            {
                                State _s;
                                switch(s.type)
                                {
                                    case "bool": _s = new State(s.key, ClusterVR.CreatorKit.ParameterType.Bool); break;
                                    case "integer": _s = new State(s.key, ClusterVR.CreatorKit.ParameterType.Integer); break;
                                    case "float": _s = new State(s.key, ClusterVR.CreatorKit.ParameterType.Float); break;
                                    default: _s = new State(s.key, ClusterVR.CreatorKit.ParameterType.Signal); break;
                                }
                                _s.value = s.value;
                                state.Add(_s);
                            }
                        }
                    };
                }
                reorderableList.DoLayoutList();

                scrollPosition = scroll.scrollPosition;
            }
            if (GUILayout.Button("書き出す")) SaveJson();

            serialized.ApplyModifiedProperties();
        }

        protected void TriggerFromGimmick()
        {
            if (gimmick == null) return;

            ClusterVR.CreatorKit.Gimmick.IGimmick[] gimmickComponents = gimmick.GetComponents<ClusterVR.CreatorKit.Gimmick.IGimmick>();
            if (gimmickComponents.Length == 0) return;

            trigger.category = gimmick.name;
            trigger.displayName = gimmick.name;

            state.Clear();

            foreach (var gimmickComponent in gimmickComponents)
            {
                if (gimmickComponent.Target != ClusterVR.CreatorKit.Gimmick.GimmickTarget.Global) continue;

                State s = new State(gimmickComponent.Key, gimmickComponent.ParameterType);

                state.Add(s);
            }
        }

        protected void GenerateJSON()
        {
            error = false;
            try
            {
                triggerList.triggers.Add(new TriggerJSON(trigger, state));
            }
            catch
            {
                Debug.LogError("TypeおよびValueに正しい値が入力されていることを確認してください。");
                error = true;
            }
        }

        protected void OpenJson()
        {
            jsonFilePath = EditorUtility.OpenFilePanel("", "Assets", "json");
            if (jsonFilePath == string.Empty) return;

            string json = File.ReadAllText(jsonFilePath);

            triggerList = JsonUtility.FromJson<TriggerJSONList>(json);

            reorderableList = null;
        }

        protected void SaveJson()
        {
            jsonFilePath = EditorUtility.SaveFilePanel("", "Assets", $"WebTrigger_{SceneManager.GetActiveScene().name}", "json");
            if (jsonFilePath == string.Empty) return;

            string json = "{\"triggers\":[";

            for (int i = 0; i < triggerList.triggers.Count; i++)
            {
                var t = triggerList.triggers[i];

                json += "{";
                json += "\"category\":\"" + t.category + "\",";
                json += "\"showConfirmDialog\":" + t.showConfirmDialog.ToString().ToLower() + ",";
                json += "\"displayName\":\"" + t.displayName + "\",";
                json += "\"color\":[" + t.color[0].ToString() + "," + t.color[1].ToString() + "," + t.color[2].ToString() + "],";
                json += "\"state\":[";

                for (int j = 0; j < t.state.Length; j++)
                {
                    var s = triggerList.triggers[i].state[j];
                    json += "{\"key\":\"" + s.key + "\",";
                    json += "\"type\":\"" + s.type + "\"";

                    if (s.type != "signal") json += ",\"value\":" + s.value;
                    json += "}";
                    if (j < t.state.Length - 1) json += ",";
                }
                json += "]}";
                if (i < triggerList.triggers.Count - 1) json += ",";
            }
            json += "]}";

            File.WriteAllText(jsonFilePath, json);

            GetWindow<ClusterVR.CreatorKit.Editor.Preview.WebTrigger.WebTriggerWindow>();
        }
    }
}