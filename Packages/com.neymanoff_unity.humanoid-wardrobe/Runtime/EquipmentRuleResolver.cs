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
            return ValidateEquipRequest(item, requestedSlot, null, false);
        }

        /// <summary>
        /// Validates whether the item can be equipped into the requested slot for a specific character body profile.
        /// </summary>
        public static EquipResultStatus ValidateEquipRequest(
            WardrobeItemSO item,
            EquipmentSlot requestedSlot,
            string profileId,
            bool allowDualTwoHanded = false)
        {
            if (item == null)
            {
                return EquipResultStatus.ItemNull;
            }

            if (item.GetPrefabForProfile(profileId) == null)
            {
                return EquipResultStatus.MissingPrefab;
            }

            if (!item.CanEquipIntoSlot(requestedSlot, allowDualTwoHanded))
            {
                return EquipResultStatus.InvalidSlot;
            }

            return EquipResultStatus.Success;
        }

        /// <summary>
        /// Resolves the effective target slot for hand equipment.
        /// Pure off-hand items (such as shields) always stay in OffHand.
        /// Dual-hand weapons (1H weapons, or 2H weapons when allowDualTwoHanded is enabled) can only be placed in OffHand
        /// if MainHand is already occupied by a single-hand weapon; otherwise they automatically route to MainHand first.
        /// </summary>
        public static EquipmentSlot ResolveEffectiveHandSlot(
            WardrobeItemSO item,
            EquipmentSlot requestedSlot,
            IReadOnlyDictionary<EquipmentSlot, EquippedItemInstance> currentSlots,
            bool allowDualTwoHanded = false)
        {
            if (item == null || requestedSlot != EquipmentSlot.OffHand)
            {
                return requestedSlot;
            }

            if (item.IsDualHandCapable(allowDualTwoHanded))
            {
                bool hasSingleHandWeaponInMain = false;
                if (currentSlots != null &&
                    currentSlots.TryGetValue(EquipmentSlot.MainHand, out var mainInstance) &&
                    mainInstance != null &&
                    mainInstance.OccupiedSlots.Count == 1)
                {
                    hasSingleHandWeaponInMain = true;
                }

                if (!hasSingleHandWeaponInMain)
                {
                    return EquipmentSlot.MainHand;
                }
            }

            return requestedSlot;
        }

        /// <summary>
        /// Finds all currently occupied slots that conflict with the incoming item's required slots.
        /// </summary>
        public static List<EquipmentSlot> GetConflictingSlots(
            WardrobeItemSO item,
            EquipmentSlot requestedSlot,
            IReadOnlyDictionary<EquipmentSlot, EquippedItemInstance> currentSlots,
            bool allowDualTwoHanded = false)
        {
            List<EquipmentSlot> conflictingSlots = new();
            if (item == null || currentSlots == null) return conflictingSlots;

            List<EquipmentSlot> neededSlots = item.GetOccupiedSlots(requestedSlot, allowDualTwoHanded);

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
