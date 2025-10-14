using ClusterVR.CreatorKit.World;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClusterWorldTools.Editor.Common
{
    public class EditorSettingUtil
    {
        const string SETTINGS_KEY_PREFIX = "ClusterWorldToolsSettings/";

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
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{key} の設定読み込みに失敗しました。デフォルト値を使用します。{ex.Message}");
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
