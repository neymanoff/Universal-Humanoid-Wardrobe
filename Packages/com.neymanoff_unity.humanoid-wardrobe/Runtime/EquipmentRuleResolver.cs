using System.Collections.Generic;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Pure domain helper that validates equipment requests and determines conflicting slots.
    /// Decouples equipment rules from visual lifecycle and Unity components.
    /// </summary>
    public static class EquipmentRuleResolver
    {
        /// <summary>
        /// Validates whether the item can be equipped into the requested slot.
        /// </summary>
        public static EquipResultStatus ValidateEquipRequest(WardrobeItemSO item, EquipmentSlot requestedSlot)
        {
            if (item == null)
            {
                return EquipResultStatus.ItemNull;
            }

            if (item.ItemPrefab == null)
            {
                return EquipResultStatus.MissingPrefab;
            }

            if (!item.CanEquipIntoSlot(requestedSlot))
            {
                return EquipResultStatus.InvalidSlot;
            }

            return EquipResultStatus.Success;
        }

        /// <summary>
        /// Finds all currently occupied slots that conflict with the incoming item's required slots.
        /// </summary>
        public static List<EquipmentSlot> GetConflictingSlots(
            WardrobeItemSO item,
            EquipmentSlot requestedSlot,
            IReadOnlyDictionary<EquipmentSlot, EquippedItemInstance> currentSlots)
        {
            List<EquipmentSlot> conflictingSlots = new();
            if (item == null || currentSlots == null) return conflictingSlots;

            List<EquipmentSlot> neededSlots = item.GetOccupiedSlots(requestedSlot);

            for (int i = 0; i < neededSlots.Count; i++)
            {
                EquipmentSlot slot = neededSlots[i];
                if (currentSlots.TryGetValue(slot, out var existingInstance) && existingInstance != null)
                {
                    // Add all slots occupied by the existing instance so it is unequipped completely
                    for (int j = 0; j < existingInstance.OccupiedSlots.Count; j++)
                    {
                        EquipmentSlot existingSlot = existingInstance.OccupiedSlots[j];
                        if (!conflictingSlots.Contains(existingSlot))
                        {
                            conflictingSlots.Add(existingSlot);
                        }
                    }
                }
            }

            return conflictingSlots;
        }
    }
}
