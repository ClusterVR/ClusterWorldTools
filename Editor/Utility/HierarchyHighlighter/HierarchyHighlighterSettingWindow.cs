using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ClusterWorldTools.Editor.Utility.HierarchyHighlighter
{
    public class HierarchyHighlighterSettingWindow : EditorWindow
    {
        [SerializeField]List<HierarchyHighlighterSettings.ColorSetting> tagsSettings = new();
        [SerializeField]List<HierarchyHighlighterSettings.ColorSetting> layersSettings = new();

        [MenuItem("WorldTools/設定/HierarchyHighlighter", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchyHighlighterSettingWindow>();
            window.titleContent = new GUIContent("HierarchyHighlighterSetting");
        }

        void OnEnable()
        {
            RebuildGUI();
        }

        void RebuildGUI()
        {
            HierarchyHighlighterSettings.instance.LoadColorSettings();
            tagsSettings = HierarchyHighlighterSettings.instance.GetTagsSettings();
            layersSettings = HierarchyHighlighterSettings.instance.GetLayersSettings();

            rootVisualElement.Clear();

            rootVisualElement.Add(new Label("Tags"));
            rootVisualElement.Add(CreateListView(tagsSettings, "tagsSettings", InternalEditorUtility.tags.ToList()));

            rootVisualElement.Add(new Label("Layers"));
            rootVisualElement.Add(CreateListView(layersSettings, "layersSettings", InternalEditorUtility.layers.ToList()));

            var buttonsElement = new VisualElement();

            var saveButton = new Button();
            saveButton.text = "Save";
            saveButton.style.height = new StyleLength(30f);
            saveButton.style.width = new StyleLength(120f);
            saveButton.clicked += SaveSettings;
            buttonsElement.Add(saveButton);

            var resetButton = new Button();
            resetButton.text = "Reset";
            resetButton.style.height = new StyleLength(15f);
            resetButton.style.width = new StyleLength(120f);
            resetButton.clicked += () => {
                if (EditorUtility.DisplayDialog("Reset", "タグ・レイヤ表示設定を初期化しますか？", "初期化する", "Cancel"))
                {
                    HierarchyHighlighterSettings.instance.ResetSettings();
                    RebuildGUI();
                }
            };
            buttonsElement.Add(resetButton);

            rootVisualElement.Add(buttonsElement);
        }

        VisualElement CreateListView(List<HierarchyHighlighterSettings.ColorSetting> property, string propertyName, List<string> choices)
        {
            var listView = new ListView(property);
            listView.reorderMode = ListViewReorderMode.Simple;
            listView.reorderable = false;
            listView.showAddRemoveFooter = true;
            listView.showBorder = true;
            listView.showFoldoutHeader = false;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.makeItem = () => {
                var element = new BindableElement();
                element.style.paddingLeft = 16f;

                var dropdownField = new DropdownField();
                dropdownField.bindingPath = "name";
                dropdownField.choices = choices;
                element.Add(dropdownField);

                var colorField = new ColorField();
                colorField.bindingPath = "color";
                element.Add(colorField);

                return element;
            };
            listView.bindItem = (element, i) => {
                if (property[i] == null) property[i] = new HierarchyHighlighterSettings.ColorSetting();
                var bindableElement = element as BindableElement;
                var serializedObject = new SerializedObject(this);
                var settingsProperty = serializedObject.FindProperty(propertyName);
                var itemProperty = settingsProperty.GetArrayElementAtIndex(i);
                bindableElement.BindProperty(itemProperty);
            };
            return listView;
        }

        void SaveSettings()
        {
            var serializedObject = new SerializedObject(this);
            serializedObject.Update();
            HierarchyHighlighterSettings.instance.SaveColorSettings(tagsSettings, layersSettings);
        }
    }
}
