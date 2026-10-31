using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="CharacterAppearance"/> providing live Editor and PlayMode
    /// controls to test hairstyle, beard, color presets, and visibility restrictions directly in the Inspector.
    /// </summary>
    [CustomEditor(typeof(CharacterAppearance))]
    public class CharacterAppearanceEditor : UnityEditor.Editor
    {
        private CharacterAppearance _appearance;

        private void OnEnable()
        {
            _appearance = (CharacterAppearance)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool propsChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (propsChanged && _appearance != null)
            {
                _appearance.SetColors(_appearance.HairColor, _appearance.BeardColor, _appearance.LinkHairAndBeardColor);
            }

            if (_appearance == null)
            {
                return;
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Cosmetic Appearance Quick Controls", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                $"Active Hair: {(!string.IsNullOrEmpty(_appearance.CurrentHairId) ? _appearance.CurrentHairId : "None")} (Visible: {_appearance.IsHairVisible})\n" +
                $"Active Beard: {(!string.IsNullOrEmpty(_appearance.CurrentBeardId) ? _appearance.CurrentBeardId : "None")} (Visible: {_appearance.IsBeardVisible})\n" +
                $"Restrictions: {_appearance.ActiveVisibilityRestrictions}",
                MessageType.Info);

            CosmeticCatalogSO catalog = _appearance.Catalog;
            if (catalog != null)
            {
                DrawCategoryButtons("Hair Options", catalog.HairOptions, isHair: true);
                DrawCategoryButtons("Beard Options", catalog.BeardOptions, isHair: false);
                DrawColorPresetButtons(catalog.ColorPresets);
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Default Cosmetics", GUILayout.Height(26)))
                {
                    SerializedProperty defaultHairProp = serializedObject.FindProperty("defaultHairOption");
                    SerializedProperty defaultBeardProp = serializedObject.FindProperty("defaultBeardOption");
                    _appearance.SetHair(defaultHairProp?.objectReferenceValue as CosmeticOptionSO);
                    _appearance.SetBeard(defaultBeardProp?.objectReferenceValue as CosmeticOptionSO);
                    EditorUtility.SetDirty(_appearance);
                }

                if (GUILayout.Button("Clear All (None)", GUILayout.Height(26)))
                {
                    _appearance.ClearAllCosmetics();
                    EditorUtility.SetDirty(_appearance);
                }
            }
        }

        private void DrawCategoryButtons(string label, IReadOnlyList<CosmeticOptionSO> options, bool isHair)
        {
            if (options == null || options.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("None"))
                {
                    if (isHair)
                    {
                        _appearance.ClearHair();
                    }
                    else
                    {
                        _appearance.ClearBeard();
                    }
                    EditorUtility.SetDirty(_appearance);
                }

                for (int i = 0; i < options.Count; i++)
                {
                    CosmeticOptionSO opt = options[i];
                    if (opt == null)
                    {
                        continue;
                    }

                    if (GUILayout.Button(opt.DisplayName))
                    {
                        if (isHair)
                        {
                            _appearance.SetHair(opt);
                        }
                        else
                        {
                            _appearance.SetBeard(opt);
                        }
                        EditorUtility.SetDirty(_appearance);
                    }
                }
            }
        }

        private void DrawColorPresetButtons(IReadOnlyList<CosmeticColorPreset> presets)
        {
            if (presets == null || presets.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField("Color Presets (Applies to Hair / Linked)", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < presets.Count; i++)
                {
                    CosmeticColorPreset preset = presets[i];
                    string btnLabel = !string.IsNullOrEmpty(preset.displayName) ? preset.displayName : preset.presetId;
                    if (GUILayout.Button(btnLabel))
                    {
                        _appearance.SetHairColor(preset.color);
                        EditorUtility.SetDirty(_appearance);
                    }
                }
            }
        }
    }
}
