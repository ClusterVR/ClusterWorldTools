using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ClusterWorldTools.Editor.Tool.WebTriggerGenerator
{
    class WebTriggerExporter
    {
        const string indentBlock = "    ";

        public static Triggers OpenJson(string path)
        {
            var json = File.ReadAllText(path);
            return FromJson(json);
        }

        public static void ExportJson(string path, Triggers triggers)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var json = ToJson(triggers);
            File.WriteAllText(path, json);
        }

        public static Triggers FromJson(string json)
        {
            return JsonUtility.FromJson<Triggers>(json);
        }

        public static string ToJson(Triggers triggerList)
        {
            JsonString json = new();
            json.AddLine("{");
            json.AddLine("\"triggers\": [", 1);

            for (int i = 0; i < triggerList.triggers.Count; i++)
            {
                var trigger = triggerList.triggers[i];

                json.AddLine("{", 2);
                json.AddLine($"\"category\": \"{trigger.category}\"", 3);
                json.AddLine($"\"showConfirmDialog\": {trigger.showConfirmDialog.ToString().ToLower()}");
                json.AddLine($"\"displayName\": \"{trigger.displayName}\"");
                json.AddLine($"\"color\": [ {trigger.color[0]}, {trigger.color[1]}, {trigger.color[2]} ]");
                json.AddLine("\"state\": [");

                for (int j = 0; j < trigger.state.Count; j++)
                {
                    var state = triggerList.triggers[i].state[j];
                    json.AddLine("{", 4);
                    json.AddLine($"\"key\": \"{state.key}\"", 5);
                    json.AddLine($"\"type\": \"{state.type}\"");

                    if (state.typeEnum != TriggerParameterType.Signal)
                        json.AddLine($"\"value\": {state.value}");
                    json.AddLine("}", 4);
                }
                json.AddLine("]", 3);
                json.AddLine("}", 2);
            }
            json.AddLine("]", 1);
            json.AddLine("}", 0);

            return json.json;
        }

        class JsonString
        {
            public string json { get; private set; } = string.Empty;
            int currentLevel = 0;

            public void AddLine(string line, int level)
            {
                if (string.IsNullOrEmpty(json))
                {
                    json = line;
                }
                else
                {
                    var addComma = (level == currentLevel) && !(line.StartsWith(']') || line.StartsWith('}'));
                    json += $"{(addComma ? "," : string.Empty)}{GenerateIndent(level)}{line}";
                }
                currentLevel = level;
            }

            public void AddLine(string line)
            {
                AddLine(line, currentLevel);
            }

            static Dictionary<int, string> indentCache = new();
            static string GenerateIndent(int level)
            {
                if (level < 0)
                    return string.Empty;
                if (indentCache.TryGetValue(level, out var indent))
                {
                    return indent;
                }

                indent = $"{Environment.NewLine}{string.Concat(System.Linq.Enumerable.Repeat(indentBlock, level))}";
                indentCache[level] = indent;
                return indent;
            }
        }
    }
}
