using System;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Categories of optional character cosmetic features managed independently of equipment slots.
    /// </summary>
    public enum CosmeticCategory
    {
        Hair = 0,
        Beard = 1
    }

    /// <summary>
    /// Bitmask flags specifying which cosmetic features should be temporarily hidden
    /// by an equipped item or an external visual outfit adapter.
    /// </summary>
    [Flags]
    public enum CosmeticVisibilityMask
    {
        None = 0,
        Hair = 1 << 0,
        Beard = 1 << 1,
        All = Hair | Beard
    }

    /// <summary>
    /// Inspector-configurable named color swatch preset for hair and beard customization.
    /// </summary>
    [Serializable]
    public struct CosmeticColorPreset
    {
        [Tooltip("Stable identifier for this color preset (e.g., 'dark_brown', 'blonde').")]
        public string presetId;

        [Tooltip("Human-readable label for UI or Inspector display.")]
        public string displayName;

        [Tooltip("Linear/sRGB tint color applied to the cosmetic material properties.")]
        public Color color;
    }

    /// <summary>
    /// Structured result returned when restoring saved <see cref="CharacterAppearanceData"/>.
    /// Reports whether any requested option IDs could not be resolved from the catalog.
    /// </summary>
    public readonly struct AppearanceApplyResult
    {
        /// <summary>
        /// True if all non-empty hair and beard option IDs were found and applied.
        /// </summary>
        public bool IsFullyResolved => !HairIdMissing && !BeardIdMissing;

        /// <summary>
        /// True if the saved hair option ID was non-empty but could not be found in the resolver/catalog.
        /// </summary>
        public bool HairIdMissing { get; }

        /// <summary>
        /// True if the saved beard option ID was non-empty but could not be found in the resolver/catalog.
        /// </summary>
        public bool BeardIdMissing { get; }

        /// <summary>
        /// The unresolved hair option ID, or empty if resolved/none.
        /// </summary>
        public string MissingHairId { get; }

        /// <summary>
        /// The unresolved beard option ID, or empty if resolved/none.
        /// </summary>
        public string MissingBeardId { get; }

        public AppearanceApplyResult(bool hairIdMissing, string missingHairId, bool beardIdMissing, string missingBeardId)
        {
            HairIdMissing = hairIdMissing;
            MissingHairId = missingHairId ?? string.Empty;
            BeardIdMissing = beardIdMissing;
            MissingBeardId = missingBeardId ?? string.Empty;
        }
    }
}
