using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ClusterWorldTools.Editor.Utility.HierarchyHighlighter
{
    class HierarchyHighlighterSettings : ScriptableSingleton<HierarchyHighlighterSettings>
    {
        static readonly string keyPref = "ClusterWorldToolsSettings/HierarchyHighlighterSettings/";
        static readonly string tagsAndLayersSettingKey = keyPref + "TagsAndLayers";
        static readonly string showComponentsKey = keyPref + "ShowComponents";
        static readonly string showTagsAndLayersKey = keyPref + "ShowTagsAndLayers";

        [System.Serializable]
        public class ColorSetting
        {
            [SerializeField]public string name = "";
            [SerializeField]public Color color = Color.white;
        }

        [System.Serializable]
        public class ColorSettings
        {
            [SerializeField]public List<ColorSetting> tagsSettings = new();
            [SerializeField]public List<ColorSetting> layersSettings = new();
        }
        [SerializeField]ColorSettings tagsAndLayersSetting = new();

        bool showComponents;
        bool showTagsAndLayers;

        public bool ShowComponents
        {
            get => showComponents;
            set
            {
                if (showComponents != value)
                {
                    showComponents = value;
                    SaveBoolSetting(showComponentsKey, showComponents);
                }
            }
        }
        public bool ShowTagsAndLayers
        {
            get => showTagsAndLayers;
            set
            {
                if (showTagsAndLayers != value)
                {
                    showTagsAndLayers = value;
                    SaveBoolSetting(showTagsAndLayersKey, showTagsAndLayers);
                }
            }
        }

        public void LoadSettings()
        {
            LoadColorSettings();
            showComponents = LoadBoolSetting(showComponentsKey, true);
            showTagsAndLayers = LoadBoolSetting(showTagsAndLayersKey, true);
        }

        bool LoadBoolSetting(string key, bool defaultValue)
        {
            return EditorPrefs.HasKey(key) ? EditorPrefs.GetBool(key) : defaultValue;
        }

        void SaveBoolSetting(string key, bool value)
        {
            EditorPrefs.SetBool(key, value);
        }

        public void LoadColorSettings()
        {
            if (EditorPrefs.HasKey(tagsAndLayersSettingKey))
            {
                var json = EditorPrefs.GetString(tagsAndLayersSettingKey);
                tagsAndLayersSetting = JsonUtility.FromJson<ColorSettings>(json);
            }
            else
            {
                ResetSettings();
            }
        }

        public void SaveColorSettings(List<ColorSetting> tagsSettings, List<ColorSetting> layersSettings)
        {
            tagsAndLayersSetting.tagsSettings = new(tagsSettings);
            tagsAndLayersSetting.layersSettings = new(layersSettings);

            var json = JsonUtility.ToJson(tagsAndLayersSetting);
            EditorPrefs.SetString(tagsAndLayersSettingKey, json);

            LoadColorSettings();
        }

        public List<ColorSetting> GetTagsSettings()
        {
            return tagsAndLayersSetting.tagsSettings;
        }

        public List<ColorSetting> GetLayersSettings()
        {
            return tagsAndLayersSetting.layersSettings;
        }

        public void ResetSettings()
        {
            var tagsSettings = new List<ColorSetting>{
                new(){ name = "EditorOnly", color = new(0.3f, 0.5f, 0.7f) },
                new(){ name = "DeactivateOnUpload", color = new(1f, 0.7f, 0.3f) },
            };
            var layersSettings = new List<ColorSetting>{
                new(){ name = "CameraOnly", color = new(0f, 1f, 1f) },
                new(){ name = "PerformerOnly", color = new(0.5f, 1f, 0.3f) },
            };
            SaveColorSettings(tagsSettings, layersSettings);
        }
    }
}
