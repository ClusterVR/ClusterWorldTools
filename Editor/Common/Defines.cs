using System;
using System.Linq;
using UnityEditor;

namespace ClusterWorldTools.Editor.Common
{
    [InitializeOnLoad]
    static class Defines
    {
        const string ClusterWorldToolsSymbol = "CLUSTER_WORLD_TOOLS";

        static Defines()
        {
            AddScriptingDefineSymbol();
        }

        static void AddScriptingDefineSymbol()
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            var symbols = (string.IsNullOrEmpty(defines) ? Array.Empty<string>() : defines.Split(';')).ToList();

            if (symbols.Any(symbol => symbol.Trim() == ClusterWorldToolsSymbol))
                return;

            symbols.Add(ClusterWorldToolsSymbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols.ToArray());
        }
    }
}
