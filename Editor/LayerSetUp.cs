using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ClusterWorldTools
{
    public class LayerSetUp : EditorWindow
    {
        static readonly Dictionary<int, string> LAYERNAMES = new Dictionary<int, string>
        {
            { 6,  "Accessory" },
            { 7,  "AccessoryPreview" },
            { 9,  "FIRSTPERSON_ONLY_LAYER" },
            { 10, "THIRDPERSON_ONLY_LAYER"},
            { 11, "RidingItem" },
            { 14, "InteractableItem" },
            { 15, "OtherAvatar" },
            { 16, "OwnAvatar" },
            { 18, "GrabbingItem" },
            { 19, "VenueLayer0" },
            { 20, "VenueLayer1" },
            { 21, "PostProcessing" },
            { 22, "PerformerOnly" },
            { 23, "Performer" },
            { 24, "Audience" },
            { 29, "VenueLayer2" },
        };

        [MenuItem("WorldTools/レイヤー自動設定")]
        static public void Menu()
        {
            if (EditorUtility.DisplayDialog("Cluster Creator Tools", "レイヤー設定を変更します", "OK", "Cancel"))
            {
                SetLayer();
                EditorUtility.DisplayDialog("Cluster Creator Tools", "レイヤー設定を変更しました", "OK");
            }
        }
        static private void SetLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray) return;

            foreach (var layername in LAYERNAMES)
            {
                var layer = layers.GetArrayElementAtIndex(layername.Key);
                layer.stringValue = layername.Value;
            }
            tagManager.ApplyModifiedProperties();
        }
    }
}