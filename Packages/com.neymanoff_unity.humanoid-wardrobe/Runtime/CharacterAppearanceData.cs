using System;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Serializable Data Transfer Object (DTO) capturing a character's cosmetic hairstyle, beard,
    /// and color customization state for save/load persistence.
    /// Does not depend on any external save system and does not hold Unity Object references.
    /// </summary>
    [Serializable]
    public class CharacterAppearanceData
    {
        [Tooltip("Stable optionId of the selected hairstyle, or empty string for 'None'.")]
        public string hairOptionId = string.Empty;

        [Tooltip("Stable optionId of the selected beard, or empty string for 'None'.")]
        public string beardOptionId = string.Empty;

        [Tooltip("Selected tint color for hair.")]
        public Color hairColor = Color.white;

        [Tooltip("Selected tint color for beard.")]
        public Color beardColor = Color.white;

        [Tooltip("Whether hair and beard colors are linked (synchronized).")]
        public bool linkColors;

        public CharacterAppearanceData()
        {
        }

        public CharacterAppearanceData(
            string hairOptionId,
            string beardOptionId,
            Color hairColor,
            Color beardColor,
            bool linkColors = false)
        {
            this.hairOptionId = hairOptionId ?? string.Empty;
            this.beardOptionId = beardOptionId ?? string.Empty;
            this.hairColor = hairColor;
            this.beardColor = linkColors ? hairColor : beardColor;
            this.linkColors = linkColors;
        }

        /// <summary>
        /// Serializes this appearance state into a JSON string using <see cref="JsonUtility"/>.
        /// </summary>
        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// Deserializes a <see cref="CharacterAppearanceData"/> instance from a JSON string.
        /// Never throws on null, empty, or malformed input; returns a clean default DTO on error.
        /// </summary>
        public static CharacterAppearanceData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new CharacterAppearanceData();
            }

            try
            {
                CharacterAppearanceData data = JsonUtility.FromJson<CharacterAppearanceData>(json);
                if (data == null)
                {
                    return new CharacterAppearanceData();
                }

                data.hairOptionId ??= string.Empty;
                data.beardOptionId ??= string.Empty;
                if (data.linkColors)
                {
                    data.beardColor = data.hairColor;
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterAppearanceData] Failed to parse JSON appearance data, returning empty state: {ex.Message}");
                return new CharacterAppearanceData();
            }
        }

        /// <summary>
        /// Creates a deep copy of this appearance data.
        /// </summary>
        public CharacterAppearanceData Clone()
        {
            return new CharacterAppearanceData(hairOptionId, beardOptionId, hairColor, beardColor, linkColors);
        }
    }
}
