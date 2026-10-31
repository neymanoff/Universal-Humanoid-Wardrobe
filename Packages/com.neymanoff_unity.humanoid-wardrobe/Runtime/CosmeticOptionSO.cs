using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Defines a single selectable cosmetic appearance option (Hairstyle or Beard).
    /// <para>
    /// <b>3D Model Requirements:</b>
    /// <list type="bullet">
    /// <item>Static/rigid hair or beard meshes should include a <see cref="HumanoidAttachmentPoint"/> component (defaulting to <see cref="HumanBodyBones.Head"/>) or rely on <see cref="DefaultAttachmentBone"/>.</item>
    /// <item>Deforming/skinned hair or beard meshes rigged to the humanoid skeleton should include a <see cref="SkinnedMeshRemapper"/> component authored with a matching rest pose and bone names.</item>
    /// <item>For color tinting, the material shader must expose at least one color property listed in <see cref="ColorShaderProperties"/> (e.g. <c>_BaseColor</c> for URP Lit or <c>_Color</c> for Standard/Legacy shaders), ideally with a white/grayscale base albedo texture so multiplicative tinting produces accurate hues.</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "NewCosmeticOption", menuName = "Humanoid Wardrobe/Cosmetic Option")]
    public class CosmeticOptionSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique, stable string identifier used for save/load persistence. Falls back to normalized asset name if empty.")]
        [SerializeField]
        private string optionId;

        [Tooltip("Human-readable display name for UI.")]
        [SerializeField]
        private string displayName;

        [Tooltip("Whether this option represents a Hairstyle or a Beard.")]
        [SerializeField]
        private CosmeticCategory category = CosmeticCategory.Hair;

        [Tooltip("Optional 2D icon for UI selection grids.")]
        [SerializeField]
        private Sprite icon;

        [Header("Visual Prefab & Skeleton Attachment")]
        [Tooltip("The 3D prefab to instantiate. Supports SkinnedMeshRemapper, HumanoidAttachmentPoint, or direct parenting to DefaultAttachmentBone.")]
        [SerializeField]
        private GameObject prefab;

        [Tooltip("Fallback humanoid bone used when the prefab does not carry a SkinnedMeshRemapper or HumanoidAttachmentPoint.")]
        [SerializeField]
        private HumanBodyBones defaultAttachmentBone = HumanBodyBones.Head;

        [Tooltip("Optional profile-specific prefab variants (e.g. Dwarf, Giant). If a character's active BodyScaleProfileSO matches, that prefab is spawned instead.")]
        [SerializeField]
        private List<ProfilePrefabOverride> profilePrefabOverrides = new();

        [Header("Material & Color Tinting")]
        [Tooltip("If enabled, CharacterAppearance applies per-instance hair/beard color via MaterialPropertyBlock.")]
        [SerializeField]
        private bool supportsColorTinting = true;

        [Tooltip("Shader color property names to set via MaterialPropertyBlock (e.g., '_BaseColor' for URP, '_Color' for Standard).")]
        [SerializeField]
        private List<string> colorShaderProperties = new() { "_BaseColor", "_Color" };

        [Tooltip("Optional 0-based material slot indices to tint on each renderer. Leave empty to tint all material slots on the renderer.")]
        [SerializeField]
        private List<int> targetMaterialIndices = new();

        [Tooltip("Optional child Renderer GameObject names to tint. Leave empty to tint all Renderers inside the spawned cosmetic prefab.")]
        [SerializeField]
        private List<string> targetRendererNames = new();

        [Tooltip("When enabled, clears _BaseMap and _MainTex on the per-character runtime material clone so solid color tinting is not darkened by an existing texture (useful for untextured stub meshes sharing a dark material).")]
        [SerializeField]
        private bool clearBaseTextureOnTint = false;

        /// <summary>
        /// Stable persistence identifier for this cosmetic option.
        /// </summary>
        public string OptionId => !string.IsNullOrEmpty(optionId)
            ? optionId
            : name.ToLowerInvariant().Replace(" ", "_");

        /// <summary>
        /// Display name for UI presentation.
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(displayName)
            ? displayName
            : name;

        /// <summary>
        /// Cosmetic category (<see cref="CosmeticCategory.Hair"/> or <see cref="CosmeticCategory.Beard"/>).
        /// </summary>
        public CosmeticCategory Category => category;

        /// <summary>
        /// Optional UI icon sprite.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Default 3D prefab for this option.
        /// </summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// Fallback bone when neither <see cref="SkinnedMeshRemapper"/> nor <see cref="HumanoidAttachmentPoint"/> is present.
        /// </summary>
        public HumanBodyBones DefaultAttachmentBone => defaultAttachmentBone;

        /// <summary>
        /// Profile-specific prefab overrides for adaptive character scaling.
        /// </summary>
        public IReadOnlyList<ProfilePrefabOverride> ProfilePrefabOverrides => profilePrefabOverrides;

        /// <summary>
        /// Whether this option receives per-instance color tinting.
        /// </summary>
        public bool SupportsColorTinting => supportsColorTinting;

        /// <summary>
        /// Shader color property names targeted during <see cref="MaterialPropertyBlock"/> updates.
        /// </summary>
        public IReadOnlyList<string> ColorShaderProperties => colorShaderProperties;

        /// <summary>
        /// Optional material slot indices (0-based) to tint. Empty means all material slots.
        /// </summary>
        public IReadOnlyList<int> TargetMaterialIndices => targetMaterialIndices;

        /// <summary>
        /// Optional Renderer GameObject names to tint. Empty means all child renderers.
        /// </summary>
        public IReadOnlyList<string> TargetRendererNames => targetRendererNames;

        /// <summary>
        /// When true, clears <c>_BaseMap</c> and <c>_MainTex</c> on the per-instance runtime material clone before tinting.
        /// </summary>
        public bool ClearBaseTextureOnTint => clearBaseTextureOnTint;

        /// <summary>
        /// Resolves the prefab to instantiate for a given character body profile ID.
        /// Falls back to <see cref="Prefab"/> if no matching override exists.
        /// </summary>
        public GameObject GetPrefabForProfile(string profileId)
        {
            if (!string.IsNullOrEmpty(profileId) && profilePrefabOverrides != null)
            {
                for (int i = 0; i < profilePrefabOverrides.Count; i++)
                {
                    if (string.Equals(profilePrefabOverrides[i].profileId, profileId, StringComparison.OrdinalIgnoreCase) &&
                        profilePrefabOverrides[i].prefab != null)
                    {
                        return profilePrefabOverrides[i].prefab;
                    }
                }
            }

            return prefab;
        }

        /// <summary>
        /// Returns true if the given renderer matches the configured <see cref="TargetRendererNames"/> filter (or if no filter is set).
        /// </summary>
        public bool ShouldTintRenderer(Renderer renderer)
        {
            if (!supportsColorTinting || renderer == null)
            {
                return false;
            }

            if (targetRendererNames == null || targetRendererNames.Count == 0)
            {
                return true;
            }

            string rendererName = renderer.gameObject.name;
            for (int i = 0; i < targetRendererNames.Count; i++)
            {
                string filter = targetRendererNames[i];
                if (!string.IsNullOrEmpty(filter) &&
                    string.Equals(rendererName, filter, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(optionId) && !string.IsNullOrEmpty(name))
            {
                optionId = name.ToLowerInvariant().Replace(" ", "_");
            }

            if (colorShaderProperties == null || colorShaderProperties.Count == 0)
            {
                colorShaderProperties = new List<string> { "_BaseColor", "_Color" };
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Initializes fields for automated unit tests without requiring disk assets.
        /// </summary>
        public void ConfigureForTest(
            string testId,
            string testName,
            CosmeticCategory testCategory,
            GameObject testPrefab,
            HumanBodyBones testBone = HumanBodyBones.Head,
            bool testSupportsTinting = true,
            IEnumerable<string> testShaderProperties = null,
            IEnumerable<int> testMaterialIndices = null,
            IEnumerable<string> testRendererNames = null,
            IEnumerable<ProfilePrefabOverride> testProfileOverrides = null)
        {
            optionId = testId;
            displayName = testName;
            category = testCategory;
            prefab = testPrefab;
            defaultAttachmentBone = testBone;
            supportsColorTinting = testSupportsTinting;
            colorShaderProperties = testShaderProperties != null
                ? new List<string>(testShaderProperties)
                : new List<string> { "_BaseColor", "_Color" };
            targetMaterialIndices = testMaterialIndices != null
                ? new List<int>(testMaterialIndices)
                : new List<int>();
            targetRendererNames = testRendererNames != null
                ? new List<string>(testRendererNames)
                : new List<string>();
            profilePrefabOverrides = testProfileOverrides != null
                ? new List<ProfilePrefabOverride>(testProfileOverrides)
                : new List<ProfilePrefabOverride>();
        }
#endif
    }
}
