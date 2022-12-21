using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools
{

    public class WebTriggerGeneratorQuick: WebTriggerGenerator
    {
        class Key
        {
            public string itemName;
            public string key;
            public ClusterVR.CreatorKit.ParameterType type;
            public bool use;
        };
        List<Key> keys = null;

        [MenuItem("WorldTools/Webトリガー生成/かんたん")]
        static public new void CreateWindow()
        {
            EditorWindow window = GetWindow<WebTriggerGeneratorQuick>();
            window.titleContent = new GUIContent("Webトリガー生成（かんたん）");
        }

        override public void OnGUI()
        {
            SerializedObject serialized = new SerializedObject(this);
            serialized.Update();
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                using (var horizontalScope = new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), GUILayout.MaxWidth(48)) || keys == null)
                    {
                        keys = new List<Key>();

                        var gimmicks = FindObjectsOfType<GameObject>(true);
                        foreach (var gimmick in gimmicks)
                        {
                            var gimmickComponents = gimmick.GetComponents<ClusterVR.CreatorKit.Gimmick.IGimmick>();
                            foreach (var gimmickComponent in gimmickComponents)
                            {
                                if (gimmickComponent.Target != ClusterVR.CreatorKit.Gimmick.GimmickTarget.Global) continue;

                                Key gimmickKey = new Key();
                                gimmickKey.itemName = gimmick.name;
                                gimmickKey.key = gimmickComponent.Key;
                                gimmickKey.type = gimmickComponent.ParameterType;
                                gimmickKey.use = true;

                                if (gimmickKey.type != ClusterVR.CreatorKit.ParameterType.Bool && gimmickKey.type != ClusterVR.CreatorKit.ParameterType.Signal) continue;

                                keys.Add(gimmickKey);
                            }
                        }
                    }

                    if (GUILayout.Button("書き出す"))
                    {
                        triggerList.triggers.Clear();
                        foreach (var gimmickKey in keys)
                        {
                            if (gimmickKey.use == false) continue;
                            if (gimmickKey.type == ClusterVR.CreatorKit.ParameterType.Bool) GenerateOnOffTrigger(gimmickKey.itemName, gimmickKey.key);
                            else if (gimmickKey.type == ClusterVR.CreatorKit.ParameterType.Signal) GenerateSignalTrigger(gimmickKey.itemName, gimmickKey.key);
                        }
                        SaveJson();
                    }
                }

                for (int i = 0; i < keys.Count; i++)
                {
                    var gimmickKey = keys[i];
                    using (var horizontalScope = new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{gimmickKey.type.ToString()}: {gimmickKey.itemName}/{gimmickKey.key}");
                        gimmickKey.use = EditorGUILayout.Toggle(gimmickKey.use);
                    }
                }

                if (error)
                {
                    Color defaultColor = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("値が正しいことを確認してください。");
                    GUI.color = defaultColor;
                }

                scrollPosition = scroll.scrollPosition;
            }

            serialized.ApplyModifiedProperties();
        }

        void GenerateOnOffTrigger(string name, string key)
        {
            List<State> s = new List<State>();
            State activate = new State(key, ClusterVR.CreatorKit.ParameterType.Bool);
            activate.value = "true";
            s.Add(activate);
            trigger.category = name;
            trigger.displayName = key + "_ON";
            triggerList.triggers.Add(new TriggerJSON(trigger, s));

            State deactive = new State(key, ClusterVR.CreatorKit.ParameterType.Bool);
            deactive.value = "false";
            s[0] = deactive;
            trigger.displayName = key + "_OFF";
            triggerList.triggers.Add(new TriggerJSON(trigger, s));
        }

        void GenerateSignalTrigger(string name, string key)
        {
            List<State> s = new List<State>();
            State activate = new State(key, ClusterVR.CreatorKit.ParameterType.Signal);
            s.Add(activate);
            trigger.category = name;
            trigger.displayName = key + "_Signal";
            triggerList.triggers.Add(new TriggerJSON(trigger, s));
        }
    }
}
