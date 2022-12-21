using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools
{
    public static class ClusterTextureImportSettings
    {
        public const string PREFS_KEY_PREFIX = "ClusterCreatorKitExtend_";
        public const string PREFS_KEY_TEXTURE_SIZE_PC_SELECTED = PREFS_KEY_PREFIX + "TextureSizePC";
        public const string PREFS_KEY_TEXTURE_SIZE_MOBILE_SELECTED = PREFS_KEY_PREFIX + "TextureSizeMobile";
        public const string PREFS_KEY_TEXTURE_IMPORT_FORMAT_SELECTED = PREFS_KEY_PREFIX + "TextureImportFormat";
        public const string PREFS_KEY_APPLY_TEXTURE_LIMIT = PREFS_KEY_PREFIX + "ApplyTextureLimit";

        public static int MAX_TEXTURE_SIZE_PC = 1024;
        public static int TEXTURE_SIZE_PC_INDEX = 5;
        public static int MAX_TEXTURE_SIZE_MOBILE = 1024;
        public static int TEXTURE_SIZE_MOBILE_INDEX = 5;
        public static readonly string[] TEXTURE_SIZE_LIST = { "32", "64", "128", "256", "512", "1024", "2048" };
        public static bool APPLY_TEXTURE_LIMIT = false;

        public static TextureImporterFormat TEXTURE_IMPORTER_FORMAT = TextureImporterFormat.ASTC_8x8;
        public static int TEXTURE_IMPORTER_FORMAT_INDEX = 1;
        public static readonly string[] TEXTURE_IMPORTER_FORMAT_LIST = { "High", "Mid", "Low" };
        public static TextureImporterFormat TextureFormatIndex2TextureFormat(int index)
        {
            switch (index)
            {
                case 0: return TextureImporterFormat.ASTC_12x12;
                case 1: return TextureImporterFormat.ASTC_8x8;
                default:return TextureImporterFormat.ASTC_4x4;
            }
        }

        public static void LoadFromPrefs()
        {
            TEXTURE_SIZE_PC_INDEX = EditorPrefs.GetInt(PREFS_KEY_TEXTURE_SIZE_PC_SELECTED, 5);
            MAX_TEXTURE_SIZE_PC = System.Convert.ToInt32(TEXTURE_SIZE_LIST[TEXTURE_SIZE_PC_INDEX]);

            TEXTURE_SIZE_MOBILE_INDEX = EditorPrefs.GetInt(PREFS_KEY_TEXTURE_SIZE_MOBILE_SELECTED, 5);
            MAX_TEXTURE_SIZE_MOBILE = System.Convert.ToInt32(TEXTURE_SIZE_LIST[TEXTURE_SIZE_MOBILE_INDEX]);

            TEXTURE_IMPORTER_FORMAT_INDEX = EditorPrefs.GetInt(PREFS_KEY_TEXTURE_IMPORT_FORMAT_SELECTED, 1);
            TEXTURE_IMPORTER_FORMAT = TextureFormatIndex2TextureFormat(TEXTURE_IMPORTER_FORMAT_INDEX);

            APPLY_TEXTURE_LIMIT = EditorPrefs.GetBool(PREFS_KEY_APPLY_TEXTURE_LIMIT, false);
        }
    }

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
            EditorGUILayout.LabelField("設定は全てのプロジェクトで共有されます");
            int textureSizePCIndex = EditorGUILayout.Popup("最大サイズ（PC）", ClusterTextureImportSettings.TEXTURE_SIZE_PC_INDEX, ClusterTextureImportSettings.TEXTURE_SIZE_LIST);
            if (textureSizePCIndex != ClusterTextureImportSettings.TEXTURE_SIZE_PC_INDEX)
            {
                EditorPrefs.SetInt(ClusterTextureImportSettings.PREFS_KEY_TEXTURE_SIZE_PC_SELECTED, textureSizePCIndex);
                ClusterTextureImportSettings.TEXTURE_SIZE_PC_INDEX = textureSizePCIndex;
                ClusterTextureImportSettings.MAX_TEXTURE_SIZE_PC = System.Convert.ToInt32(ClusterTextureImportSettings.TEXTURE_SIZE_LIST[textureSizePCIndex]);
                settingChanged = true;
            }

            int textureSizeMobileIndex = EditorGUILayout.Popup("最大サイズ（モバイル）", ClusterTextureImportSettings.TEXTURE_SIZE_MOBILE_INDEX, ClusterTextureImportSettings.TEXTURE_SIZE_LIST);
            if (textureSizeMobileIndex != ClusterTextureImportSettings.TEXTURE_SIZE_MOBILE_INDEX)
            {
                EditorPrefs.SetInt(ClusterTextureImportSettings.PREFS_KEY_TEXTURE_SIZE_MOBILE_SELECTED, textureSizeMobileIndex);
                ClusterTextureImportSettings.TEXTURE_SIZE_MOBILE_INDEX = textureSizeMobileIndex;
                ClusterTextureImportSettings.MAX_TEXTURE_SIZE_MOBILE = System.Convert.ToInt32(ClusterTextureImportSettings.TEXTURE_SIZE_LIST[textureSizeMobileIndex]);
                settingChanged = true;
            }

            int textureFormatIndex = EditorGUILayout.Popup("圧縮（モバイル）", ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_INDEX, ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_LIST);
            if (textureFormatIndex != ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_INDEX)
            {
                EditorPrefs.SetInt(ClusterTextureImportSettings.PREFS_KEY_TEXTURE_IMPORT_FORMAT_SELECTED, textureFormatIndex);
                ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT_INDEX = textureFormatIndex;
                ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT = ClusterTextureImportSettings.TextureFormatIndex2TextureFormat(textureFormatIndex);
                settingChanged = true;
            }

            bool applyTextureLimit = EditorGUILayout.Toggle("制限を適用", ClusterTextureImportSettings.APPLY_TEXTURE_LIMIT);
            if(applyTextureLimit != ClusterTextureImportSettings.APPLY_TEXTURE_LIMIT)
            {
                EditorPrefs.SetBool(ClusterTextureImportSettings.PREFS_KEY_APPLY_TEXTURE_LIMIT, applyTextureLimit);
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

    public class ClusterTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            ClusterTextureImportSettings.LoadFromPrefs();

            if (ClusterTextureImportSettings.APPLY_TEXTURE_LIMIT)
            {
                TextureImporter importer = assetImporter as TextureImporter;

                OptimizeMainSettings(importer);
                OptimizePlatformSettings(importer, "Android");
                OptimizePlatformSettings(importer, "iOS");
            }
        }

        public static void OptimizeMainSettings(in TextureImporter importer)
        {
            importer.maxTextureSize= Mathf.Min(importer.maxTextureSize, ClusterTextureImportSettings.MAX_TEXTURE_SIZE_PC);
        }

        public static void OptimizePlatformSettings(in TextureImporter importer, in string platform)
        {
            int maxTextureSize;
            TextureImporterFormat textureFormat;

            importer.GetPlatformTextureSettings(platform, out maxTextureSize, out textureFormat);

            maxTextureSize = Mathf.Min(maxTextureSize, ClusterTextureImportSettings.MAX_TEXTURE_SIZE_MOBILE);
            textureFormat = ClusterTextureImportSettings.TEXTURE_IMPORTER_FORMAT;

            importer.SetPlatformTextureSettings(platform, maxTextureSize, textureFormat);
        }
    }
}