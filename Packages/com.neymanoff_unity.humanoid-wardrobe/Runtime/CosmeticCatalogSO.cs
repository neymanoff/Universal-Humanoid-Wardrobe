using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Inspector-configurable catalog of hairstyle options, beard options, and named color presets.
    /// Assigned to <see cref="CharacterAppearance"/> to resolve options by stable string ID.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCosmeticCatalog", menuName = "Humanoid Wardrobe/Cosmetic Catalog")]
    public class CosmeticCatalogSO : ScriptableObject
    {
        [Header("Hairstyle Options")]
        [Tooltip("Available hairstyle options selectable by optionId.")]
        [SerializeField]
        private List<CosmeticOptionSO> hairOptions = new();

        [Header("Beard Options")]
        [Tooltip("Available beard options selectable by optionId.")]
        [SerializeField]
        private List<CosmeticOptionSO> beardOptions = new();

        [Header("Color Swatch Presets (Optional)")]
        [Tooltip("Named color presets for hair and beard tinting in Inspector or UI.")]
        [SerializeField]
        private List<CosmeticColorPreset> colorPresets = new();

        /// <summary>
        /// Available hairstyle options in this catalog.
        /// </summary>
        public IReadOnlyList<CosmeticOptionSO> HairOptions => hairOptions;

        /// <summary>
        /// Available beard options in this catalog.
        /// </summary>
        public IReadOnlyList<CosmeticOptionSO> BeardOptions => beardOptions;

        /// <summary>
        /// Optional named color presets.
        /// </summary>
        public IReadOnlyList<CosmeticColorPreset> ColorPresets => colorPresets;

        /// <summary>
        /// Finds a hairstyle option by stable <paramref name="optionId"/> (case-insensitive).
        /// Returns null if <paramref name="optionId"/> is null/empty or not found.
        /// </summary>
        public CosmeticOptionSO FindHairOption(string optionId)
        {
            return FindInList(hairOptions, optionId, CosmeticCategory.Hair);
        }

        /// <summary>
        /// Finds a beard option by stable <paramref name="optionId"/> (case-insensitive).
        /// Returns null if <paramref name="optionId"/> is null/empty or not found.
        /// </summary>
        public CosmeticOptionSO FindBeardOption(string optionId)
        {
            return FindInList(beardOptions, optionId, CosmeticCategory.Beard);
        }

        /// <summary>
        /// Finds a cosmetic option by <paramref name="optionId"/> within the specified <paramref name="category"/>.
        /// </summary>
        public CosmeticOptionSO FindOption(string optionId, CosmeticCategory category)
        {
            return category == CosmeticCategory.Beard
                ? FindBeardOption(optionId)
                : FindHairOption(optionId);
        }

        /// <summary>
        /// Searches both hairstyle and beard lists for an option matching <paramref name="optionId"/>.
        /// </summary>
        public CosmeticOptionSO FindOption(string optionId)
        {
            if (string.IsNullOrEmpty(optionId))
            {
                return null;
            }

            CosmeticOptionSO hair = FindHairOption(optionId);
            if (hair != null)
            {
                return hair;
            }

            return FindBeardOption(optionId);
        }

        /// <summary>
        /// Resolves a named color preset by <paramref name="presetId"/> (case-insensitive).
        /// </summary>
        public bool TryGetColorPreset(string presetId, out Color color)
        {
            if (!string.IsNullOrEmpty(presetId) && colorPresets != null)
            {
                for (int i = 0; i < colorPresets.Count; i++)
                {
                    if (string.Equals(colorPresets[i].presetId, presetId, StringComparison.OrdinalIgnoreCase))
                    {
                        color = colorPresets[i].color;
                        return true;
                    }
                }
            }

            color = Color.white;
            return false;
        }

        private static CosmeticOptionSO FindInList(
            List<CosmeticOptionSO> list,
            string optionId,
            CosmeticCategory expectedCategory)
        {
            if (string.IsNullOrEmpty(optionId) || list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                CosmeticOptionSO candidate = list[i];
                if (candidate != null &&
                    candidate.Category == expectedCategory &&
                    string.Equals(candidate.OptionId, optionId, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Initializes catalog lists in-memory for automated unit tests.
        /// </summary>
        public void ConfigureForTest(
            IEnumerable<CosmeticOptionSO> testHairOptions,
            IEnumerable<CosmeticOptionSO> testBeardOptions,
            IEnumerable<CosmeticColorPreset> testColorPresets = null)
        {
            hairOptions = testHairOptions != null ? new List<CosmeticOptionSO>(testHairOptions) : new List<CosmeticOptionSO>();
            beardOptions = testBeardOptions != null ? new List<CosmeticOptionSO>(testBeardOptions) : new List<CosmeticOptionSO>();
            colorPresets = testColorPresets != null ? new List<CosmeticColorPreset>(testColorPresets) : new List<CosmeticColorPreset>();
        }
#endif
    }
}
