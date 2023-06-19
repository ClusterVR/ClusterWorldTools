using ClusterVR.CreatorKit.World;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClusterWorldTools
{
    public class Common
    {
        const string ASSET_DIRECTORY_LOCAL = "Assets/ClusterWorldTools";
        const string ASSET_DIRECTORY_PACKAGE = "Packages/mu.cluster.cluster-world-tools";

        const string SETTINGS_KEY_PREFIX = "ClusterWorldToolsSettings/";


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

        public delegate T ConvertFunc<T>(string s);

        public static T LoadSetting<T>(string key, T defaultValue, ConvertFunc<T> convertFunc)
        {
            T ret;
            var value = EditorUserSettings.GetConfigValue($"{SETTINGS_KEY_PREFIX}{key}");
            if(value == null) return defaultValue;

            try
            {
                ret = convertFunc(value);
            }
            catch
            {
                ret = defaultValue;
            }
            return ret;
        }

        public static void SaveSetting<T>(string key, T value)
        {
            EditorUserSettings.SetConfigValue($"{SETTINGS_KEY_PREFIX}{key}", value.ToString());
        }
    }
}
