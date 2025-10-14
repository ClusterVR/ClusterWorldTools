using System.Collections.Generic;
using System.Linq;
using ClusterVR.CreatorKit;

namespace ClusterWorldTools.Editor.Tool.WebTriggerGenerator
{
    [System.Serializable]
    public class Triggers
    {
        public List<Trigger> triggers = new();
    }

    [System.Serializable]
    public class Trigger
    {
        public string displayName;
        public string category;
        public bool showConfirmDialog;
        public float[] color = { 0f, 0f, 0f };
        public List<State> state = new();

        public Trigger(){}

        public Trigger(Trigger original)
        {
            displayName = original.displayName;
            category = original.category;
            showConfirmDialog = original.showConfirmDialog;
            color = original.color;
            // 個別の要素ごとDeep Copy
            state = original.state.Select(s => new State() { key = s.key, type = s.type, value = s.value }).ToList();
        }
    }

    [System.Serializable]
    public class State
    {
        public string key;
        public string type;
        public string value;

        public State()
        {
            key = string.Empty;
            type = "signal";
            value = string.Empty;
        }

        public State(State original)
        {
            key = original.key;
            type = original.type;
            value = original.value;
        }

        static readonly Dictionary<string, TriggerParameterType> typeToEnum = new()
        {
            { "signal", TriggerParameterType.Signal },
            { "bool", TriggerParameterType.Bool },
            { "integer", TriggerParameterType.Integer },
            { "float", TriggerParameterType.Float }
        };
        public TriggerParameterType typeEnum => typeToEnum.TryGetValue(type, out var e) ? e : TriggerParameterType.Signal;
    }

    public enum TriggerParameterType
    {
        Signal,
        Bool,
        Integer,
        Float
    }
}