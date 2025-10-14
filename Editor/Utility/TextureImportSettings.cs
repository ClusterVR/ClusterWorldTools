using UnityEngine;
using UnityEditor;
using ClusterWorldTools.Editor.Common;

namespace ClusterWorldTools.Editor.Utility
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
        public static int TEXTURE_SIZE_MOBILE_INDEX = 4;
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
            TEXTURE_SIZE_PC_INDEX = EditorSettingUtil.LoadSetting(PREFS_KEY_TEXTURE_SIZE_PC_SELECTED, 5, System.Convert.ToInt32);
            MAX_TEXTURE_SIZE_PC = System.Convert.ToInt32(TEXTURE_SIZE_LIST[TEXTURE_SIZE_PC_INDEX]);

            TEXTURE_SIZE_MOBILE_INDEX = EditorSettingUtil.LoadSetting(PREFS_KEY_TEXTURE_SIZE_MOBILE_SELECTED, 5, System.Convert.ToInt32);
            MAX_TEXTURE_SIZE_MOBILE = System.Convert.ToInt32(TEXTURE_SIZE_LIST[TEXTURE_SIZE_MOBILE_INDEX]);

            TEXTURE_IMPORTER_FORMAT_INDEX = EditorSettingUtil.LoadSetting(PREFS_KEY_TEXTURE_IMPORT_FORMAT_SELECTED, 1, System.Convert.ToInt32);
            TEXTURE_IMPORTER_FORMAT = TextureFormatIndex2TextureFormat(TEXTURE_IMPORTER_FORMAT_INDEX);

            APPLY_TEXTURE_LIMIT = EditorSettingUtil.LoadSetting(PREFS_KEY_APPLY_TEXTURE_LIMIT, false, System.Convert.ToBoolean);
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