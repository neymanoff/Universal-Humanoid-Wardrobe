using System;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Public contract for managing a character's optional cosmetic appearance (Hairstyle, Beard, and Colors)
    /// and declarative visibility restrictions. Designed for external game adapters with zero coupling
    /// to game-specific classes, races, UI, or inventory systems.
    /// </summary>
    public interface ICharacterAppearance
    {
        /// <summary>
        /// Stable option ID of the currently selected hairstyle, or <see cref="string.Empty"/> when "None" is selected.
        /// Remains unchanged while hair is temporarily hidden by a visibility restriction.
        /// </summary>
        string CurrentHairId { get; }

        /// <summary>
        /// Stable option ID of the currently selected beard, or <see cref="string.Empty"/> when "None" is selected.
        /// Remains unchanged while beard is temporarily hidden by a visibility restriction.
        /// </summary>
        string CurrentBeardId { get; }

        /// <summary>
        /// Currently selected hairstyle definition, or null if "None" is selected.
        /// </summary>
        CosmeticOptionSO CurrentHairOption { get; }

        /// <summary>
        /// Currently selected beard definition, or null if "None" is selected.
        /// </summary>
        CosmeticOptionSO CurrentBeardOption { get; }

        /// <summary>
        /// Spawned hairstyle GameObject instance, or null if "None" is selected.
        /// </summary>
        GameObject CurrentHairInstance { get; }

        /// <summary>
        /// Spawned beard GameObject instance, or null if "None" is selected.
        /// </summary>
        GameObject CurrentBeardInstance { get; }

        /// <summary>
        /// Currently selected hair color.
        /// </summary>
        Color HairColor { get; }

        /// <summary>
        /// Currently selected beard color.
        /// </summary>
        Color BeardColor { get; }

        /// <summary>
        /// When true, setting <see cref="SetHairColor"/> or <see cref="SetBeardColor"/> synchronizes both colors.
        /// </summary>
        bool LinkHairAndBeardColor { get; set; }

        /// <summary>
        /// True when hair is not suppressed by any active visibility restriction.
        /// </summary>
        bool IsHairVisible { get; }

        /// <summary>
        /// True when beard is not suppressed by any active visibility restriction.
        /// </summary>
        bool IsBeardVisible { get; }

        /// <summary>
        /// Combined bitmask of all active visibility restrictions across equipment and external sources.
        /// </summary>
        CosmeticVisibilityMask ActiveVisibilityRestrictions { get; }

        /// <summary>
        /// When true, an item equipped in <see cref="EquipmentSlot.Head"/> remains equipped in the loadout,
        /// but its 3D visual model is hidden and its head/cosmetic hiding restrictions are bypassed so the character's
        /// head, hairstyle, and beard remain visible.
        /// </summary>
        bool HideHelmetVisual { get; set; }

        /// <summary>
        /// Sets whether the equipped helmet in <see cref="EquipmentSlot.Head"/> is visually hidden while remaining equipped.
        /// </summary>
        void SetHelmetVisualHidden(bool hidden);

        /// <summary>
        /// Toggles the visual visibility of the equipped helmet in <see cref="EquipmentSlot.Head"/> and returns the new hidden state.
        /// </summary>
        bool ToggleHelmetVisual();

        /// <summary>
        /// Fired when the active hairstyle or beard option changes.
        /// Parameters: (Category, SelectedOptionOrNull, SpawnedInstanceOrNull).
        /// </summary>
        event Action<CosmeticCategory, CosmeticOptionSO, GameObject> OnCosmeticChanged;

        /// <summary>
        /// Fired when hair color, beard color, or color linking changes.
        /// Parameters: (HairColor, BeardColor, LinkColors).
        /// </summary>
        event Action<Color, Color, bool> OnColorsChanged;

        /// <summary>
        /// Fired when the combined <see cref="ActiveVisibilityRestrictions"/> bitmask changes.
        /// </summary>
        event Action<CosmeticVisibilityMask> OnVisibilityRestrictionsChanged;

        /// <summary>
        /// Fired whenever any appearance state (hair, beard, or colors) changes.
        /// </summary>
        event Action<CharacterAppearanceData> OnAppearanceChanged;

        /// <summary>
        /// Selects a hairstyle by stable <paramref name="optionId"/> from the assigned catalog.
        /// Passing null or empty string clears the hairstyle ("None").
        /// </summary>
        /// <returns>True if the option was applied (or cleared when empty); false if the non-empty ID was not found.</returns>
        bool SetHair(string optionId);

        /// <summary>
        /// Selects a hairstyle directly from a <see cref="CosmeticOptionSO"/> reference.
        /// Passing null clears the hairstyle ("None").
        /// </summary>
        bool SetHair(CosmeticOptionSO option);

        /// <summary>
        /// Clears the active hairstyle ("None").
        /// </summary>
        void ClearHair();

        /// <summary>
        /// Selects a beard by stable <paramref name="optionId"/> from the assigned catalog.
        /// Passing null or empty string clears the beard ("None").
        /// </summary>
        /// <returns>True if the option was applied (or cleared when empty); false if the non-empty ID was not found.</returns>
        bool SetBeard(string optionId);

        /// <summary>
        /// Selects a beard directly from a <see cref="CosmeticOptionSO"/> reference.
        /// Passing null clears the beard ("None").
        /// </summary>
        bool SetBeard(CosmeticOptionSO option);

        /// <summary>
        /// Clears the active beard ("None").
        /// </summary>
        void ClearBeard();

        /// <summary>
        /// Clears both hairstyle and beard ("None").
        /// </summary>
        void ClearAllCosmetics();

        /// <summary>
        /// Sets the hair tint color via per-instance <see cref="MaterialPropertyBlock"/>.
        /// If <see cref="LinkHairAndBeardColor"/> is true, beard color is also updated.
        /// </summary>
        void SetHairColor(Color color);

        /// <summary>
        /// Sets the beard tint color via per-instance <see cref="MaterialPropertyBlock"/>.
        /// If <see cref="LinkHairAndBeardColor"/> is true, hair color is also updated.
        /// </summary>
        void SetBeardColor(Color color);

        /// <summary>
        /// Sets both hair and beard colors and specifies whether they should remain linked.
        /// </summary>
        void SetColors(Color newHairColor, Color newBeardColor, bool linkColors = false);

        /// <summary>
        /// Adds or updates a named visibility suppression source (e.g., "ClassOutfit_FullHelm", "CutsceneHood").
        /// While any active source hides a category, that cosmetic remains hidden without losing its selection or color.
        /// Passing <see cref="CosmeticVisibilityMask.None"/> removes the restriction for <paramref name="sourceId"/>.
        /// </summary>
        void SetVisibilityRestriction(string sourceId, CosmeticVisibilityMask hiddenMask);

        /// <summary>
        /// Removes a previously registered visibility suppression source by <paramref name="sourceId"/>.
        /// Visibility is restored only for categories not suppressed by any remaining source.
        /// </summary>
        bool RemoveVisibilityRestriction(string sourceId);

        /// <summary>
        /// Clears all external visibility restrictions registered via <see cref="SetVisibilityRestriction"/>,
        /// while preserving automatic restrictions from currently equipped <see cref="WardrobeItemSO"/> items.
        /// </summary>
        void ClearExternalVisibilityRestrictions();

        /// <summary>
        /// Exports the current appearance selection and colors into a serializable <see cref="CharacterAppearanceData"/> DTO.
        /// </summary>
        CharacterAppearanceData GetAppearanceData();

        /// <summary>
        /// Restores appearance selection and colors from a <see cref="CharacterAppearanceData"/> DTO.
        /// Missing option IDs are handled gracefully (falling back to "None" for that category without throwing
        /// or corrupting colors or the other valid category).
        /// </summary>
        /// <param name="data">Saved appearance DTO.</param>
        /// <param name="customResolver">Optional custom lookup function; uses the assigned <see cref="CosmeticCatalogSO"/> when null.</param>
        AppearanceApplyResult ApplyAppearanceData(CharacterAppearanceData data, Func<string, CosmeticOptionSO> customResolver = null);
    }
}
