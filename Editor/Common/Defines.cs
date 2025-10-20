using System;
using System.Linq;
using UnityEditor;

namespace ClusterWorldTools.Editor.Common
{
    [InitializeOnLoad]
    static class Defines
    {
        const string ClusterWorldToolsSymbol_OLD = "CLUSTER_WORLD_TOOLS";
        const string ClusterWorldToolsSymbol = "ClusterWorldTools";

        static Defines()
        {
            AddScriptingDefineSymbol();
        }

        static void AddScriptingDefineSymbol()
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            var symbols = (string.IsNullOrEmpty(defines) ? Array.Empty<string>() : defines.Split(';')).ToList();

            if (symbols.Any(symbol => symbol.Trim() == ClusterWorldToolsSymbol_OLD))
            {
                symbols.Remove(ClusterWorldToolsSymbol_OLD);
            }

            if (symbols.Any(symbol => symbol.Trim() == ClusterWorldToolsSymbol))
                return;

            symbols.Add(ClusterWorldToolsSymbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols.ToArray());
        }
    }
}
