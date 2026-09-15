using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Serialized entry mapping a slot to a stable ItemId.
    /// </summary>
    [Serializable]
    public struct EquippedSlotEntry
    {
        public EquipmentSlot slot;
        public string itemId;

        public EquippedSlotEntry(EquipmentSlot slot, string itemId)
        {
            this.slot = slot;
            this.itemId = itemId;
        }
    }

    /// <summary>
    /// Lightweight Data Transfer Object (DTO) capturing a snapshot of a character's equipped wardrobe.
    /// Designed for serialization, save games, and cross-scene level rehydration.
    /// </summary>
    [Serializable]
    public class WardrobeLoadout
    {
        public List<EquippedSlotEntry> entries = new();

        public WardrobeLoadout() { }

        public WardrobeLoadout(IEnumerable<EquippedSlotEntry> entries)
        {
            if (entries != null)
            {
                this.entries.AddRange(entries);
            }
        }

        /// <summary>
        /// Serializes the loadout into a JSON string.
        /// </summary>
        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// Reconstructs a loadout from a JSON string.
        /// </summary>
        public static WardrobeLoadout FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new WardrobeLoadout();
            return JsonUtility.FromJson<WardrobeLoadout>(json) ?? new WardrobeLoadout();
        }
    }
}
