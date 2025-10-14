using UnityEngine;
using UnityEditor;
using ClusterWorldTools.Editor.Common;

namespace ClusterWorldTools.Editor.Utility
{
    public class ClusterTextureImportSettingWindow : EditorWindow
    {
        bool settingChanged = false;

        [MenuItem("WorldTools/テクスチャインポート設定")]
        public static void CreateWindow()
        {
            ClusterTextureImportSettings.LoadFromPrefs();
            var window = GetWindow<ClusterTextureImportSettingWindow>();
            window.titleContent = new GUIContent("テクスチャインポート設定");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("設定はプロジェクトごとに保存されます");
            int textureSizePCIndex = EditorGUILayout.Popup("最大サイズ（PC）", ClusterTextureImportSettings.TEXTURE_SIZE_PC_INDEX, ClusterTextureImportSettings.TEXTURE_SIZE_LIST);
            if (textureSizePCIndex != ClusterTextureImportSettings.TEXTURE_SIZE_PC_INDEX)
            {
                EditorSettingUtil.SaveSetting(ClusterTextureImportSettings.PREFS_KEY_TEXTURE_SIZE_PC_SELECTED, textureSizePCIndex);
                ClusterTextureImportSettings.TEXTURE_SIZE_PC_INDEX = textureSizePCIndex;
                ClusterTextureImportSettings.MAX_TEXTURE_SIZE_PC = System.Convert.ToInt32(ClusterTextureImportSettings.TEXTURE_SIZE_LIST[textureSizePCIndex]);
                settingChanged = true;
            }

            int textureSizeMobileIndex = EditorGUILayout.Popup("最大サイズ（モバイル）", ClusterTextureImportSettings.TEXTURE_SIZE_MOBILE_INDEX, ClusterTextureImportSettings.TEXTURE_SIZE_LIST);
            if (textureSizeMobileIndex != ClusterTextureImportSettings.TEXTURE_SIZE_MOBILE_INDEX)
            {
                EditorSettingUtil.SaveSetting(ClusterTextureImportSettings.PREFS_KEY_TEXTURE_SIZE_MOBILE_SELECTED, textureSizeMobileIndex);
                ClusterTextureImportSettings.TEXTURE_SIZE_MOBILE_INDEX = textureSizeMobileIndex;
                ClusterTextureImportSettings.MAX_TEXTURE_SIZE_MOBILE = System.Convert.ToInt32(ClusterTextureImportSettings.TEXTURE_SIZE_LIST[textureSizeMobileIndex]);
                settingChanged = true;
            }

            int textureFormatIndex = EditorGUILayout.Popup("圧縮（モバイル）", ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_INDEX, ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_LIST);
            if (textureFormatIndex != ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_INDEX)
            {
                EditorSettingUtil.SaveSetting(ClusterTextureImportSettings.PREFS_KEY_TEXTURE_IMPORT_FORMAT_SELECTED, textureFormatIndex);
                ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_INDEX = textureFormatIndex;
                ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT = ClusterTextureImportSettings.TextureFormatIndex2TextureFormat(textureFormatIndex);
                settingChanged = true;
            }

            bool applyTextureLimit = EditorGUILayout.Toggle("制限を適用", ClusterTextureImportSettings.APPLY_TEXTURE_LIMIT);
            if(applyTextureLimit != ClusterTextureImportSettings.APPLY_TEXTURE_LIMIT)
            {
                EditorSettingUtil.SaveSetting(ClusterTextureImportSettings.PREFS_KEY_APPLY_TEXTURE_LIMIT, applyTextureLimit);
                ClusterTextureImportSettings.APPLY_TEXTURE_LIMIT = applyTextureLimit;
                settingChanged = true;
            }

            if(settingChanged && GUILayout.Button("すべて再インポート"))
            {
                if (EditorUtility.DisplayDialog("すべて再インポート", "すべてのアセットを再インポートします\n（時間がかかります）", "OK", "キャンセル"))
                {
                    Close();
                    AssetDatabase.ImportAsset("Assets", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                    settingChanged = false;
                }
            }
        }
    }
}
