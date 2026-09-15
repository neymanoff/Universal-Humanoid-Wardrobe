using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Represents an active equipped item instance in the wardrobe.
    /// Tracks the source item data, spawned GameObject, and all slots occupied by this instance (e.g. 2H weapons occupying MainHand and OffHand).
    /// </summary>
    public class EquippedItemInstance
    {
        public WardrobeItemSO ItemData { get; }
        public GameObject InstanceObject { get; }
        public EquipmentSlot PrimarySlot { get; }
        public IReadOnlyList<EquipmentSlot> OccupiedSlots { get; }

        public EquippedItemInstance(
            WardrobeItemSO itemData,
            GameObject instanceObject,
            EquipmentSlot primarySlot,
            IReadOnlyList<EquipmentSlot> occupiedSlots)
        {
            ItemData = itemData;
            InstanceObject = instanceObject;
            PrimarySlot = primarySlot;
            OccupiedSlots = occupiedSlots ?? new[] { primarySlot };
        }

        /// <summary>
        /// Checks if this instance occupies the specified equipment slot.
        /// </summary>
        public bool OccupiesSlot(EquipmentSlot slot)
        {
            if (OccupiedSlots == null) return slot == PrimarySlot;
            for (int i = 0; i < OccupiedSlots.Count; i++)
            {
                if (OccupiedSlots[i] == slot) return true;
            }
            return false;
        }
    }
}
