using ClusterVR.CreatorKit.World;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClusterWorldTools.Editor.Common
{
    public class ResourceUtil
    {
        public const string ASSET_DIRECTORY_LOCAL = "Assets/ClusterWorldTools";
        public const string ASSET_DIRECTORY_PACKAGE = "Packages/mu.cluster.cluster-world-tools";

        public static string AssetPath(in string file)
        {
            string path = $"{ASSET_DIRECTORY_PACKAGE}/{file}";
            if(File.Exists(path) == false) path = $"{ASSET_DIRECTORY_LOCAL}/{file}";
            if (File.Exists(path) == false)
            {
                Debug.LogError($"{path}\n必要なアセットが見つかりません。拡張機能を導入しなおしてください。");
            }
            return path;
        }
    }
}
