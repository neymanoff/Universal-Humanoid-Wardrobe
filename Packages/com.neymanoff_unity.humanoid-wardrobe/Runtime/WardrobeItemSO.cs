using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Legacy restriction mode (kept for backwards compatibility with existing assets).
    /// </summary>
    public enum ItemSlotRestriction
    {
        SpecificSlotOnly = 0,
        OneHanded = 1,
        TwoHanded = 2,
        OffHandOnly = 3,
        MainHandOnly = 4,
        AnyRing = 5
    }

    /// <summary>
    /// ScriptableObject representing an equippable item in the wardrobe system.
    /// Holds a stable ItemId, slot occupancy rules, UI metadata, and the 3D prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWardrobeItem", menuName = "Humanoid Wardrobe/Wardrobe Item")]
    public class WardrobeItemSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Permanent unique identifier used for save games and network synchronization. Never rely on asset names.")]
        [SerializeField] private string itemId = "";

        [Tooltip("Display name of the item.")]
        [SerializeField] private string itemName = "New Item";

        [Header("Slot Rules")]
        [Tooltip("Slots this item can legally be equipped into. If empty, falls back to Target Slot and Restriction.")]
        [SerializeField] private List<EquipmentSlot> allowedSlots = new();

        [Tooltip("Additional slots occupied simultaneously when this item is equipped (e.g. OffHand for a Two-Handed weapon).")]
        [SerializeField] private List<EquipmentSlot> additionalOccupiedSlots = new();

        [Header("Legacy Configuration (Fallback)")]
        [SerializeField] private EquipmentSlot targetSlot = EquipmentSlot.Head;
        [SerializeField] private ItemSlotRestriction restriction = ItemSlotRestriction.SpecificSlotOnly;

        [Header("Visuals")]
        [Tooltip("2D sprite icon representing the item in inventory and equipment slots.")]
        [SerializeField] private Sprite icon;

        [Tooltip("3D prefab to spawn. Must contain SkinnedMeshRemapper or HumanoidAttachmentPoint.")]
        [SerializeField] private GameObject itemPrefab;

        public string ItemId
        {
            get
            {
                if (string.IsNullOrEmpty(itemId))
                {
                    return name.ToLowerInvariant();
                }
                return itemId;
            }
        }

        public string ItemName => itemName;
        public Sprite Icon => icon;
        public GameObject ItemPrefab => itemPrefab;
        public EquipmentSlot TargetSlot => targetSlot;
        public ItemSlotRestriction Restriction => restriction;

        public IReadOnlyList<EquipmentSlot> AllowedSlots
        {
            get
            {
                if (allowedSlots != null && allowedSlots.Count > 0)
                {
                    return allowedSlots;
                }
                return GetLegacyAllowedSlots();
            }
        }

        public IReadOnlyList<EquipmentSlot> AdditionalOccupiedSlots => additionalOccupiedSlots;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name.ToLowerInvariant();
            }

            // Sync legacy fields with new lists if new lists are empty
            if (allowedSlots == null || allowedSlots.Count == 0)
            {
                allowedSlots = new List<EquipmentSlot>(GetLegacyAllowedSlots());
            }

            if (restriction == ItemSlotRestriction.TwoHanded && (additionalOccupiedSlots == null || additionalOccupiedSlots.Count == 0))
            {
                additionalOccupiedSlots = new List<EquipmentSlot> { EquipmentSlot.OffHand };
            }
        }

        /// <summary>
        /// Checks if this item can be equipped into the requested slot.
        /// </summary>
        public bool CanEquipIntoSlot(EquipmentSlot slot)
        {
            IReadOnlyList<EquipmentSlot> slots = AllowedSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == slot) return true;
            }
            return false;
        }

        /// <summary>
        /// Legacy compatibility check.
        /// </summary>
        public bool CanFitInSlot(EquipmentSlot slot) => CanEquipIntoSlot(slot);

        /// <summary>
        /// Returns all slots that will be occupied if equipped into the given primary slot.
        /// </summary>
        public List<EquipmentSlot> GetOccupiedSlots(EquipmentSlot requestedSlot)
        {
            List<EquipmentSlot> occupied = new() { requestedSlot };
            if (additionalOccupiedSlots != null)
            {
                for (int i = 0; i < additionalOccupiedSlots.Count; i++)
                {
                    if (!occupied.Contains(additionalOccupiedSlots[i]))
                    {
                        occupied.Add(additionalOccupiedSlots[i]);
                    }
                }
            }
            else if (restriction == ItemSlotRestriction.TwoHanded && requestedSlot == EquipmentSlot.MainHand)
            {
                occupied.Add(EquipmentSlot.OffHand);
            }
            return occupied;
        }

        private List<EquipmentSlot> GetLegacyAllowedSlots()
        {
            return restriction switch
            {
                ItemSlotRestriction.OneHanded => new List<EquipmentSlot> { EquipmentSlot.MainHand, EquipmentSlot.OffHand },
                ItemSlotRestriction.TwoHanded => new List<EquipmentSlot> { EquipmentSlot.MainHand },
                ItemSlotRestriction.MainHandOnly => new List<EquipmentSlot> { EquipmentSlot.MainHand },
                ItemSlotRestriction.OffHandOnly => new List<EquipmentSlot> { EquipmentSlot.OffHand },
                ItemSlotRestriction.AnyRing => new List<EquipmentSlot> { EquipmentSlot.LeftRing, EquipmentSlot.RightRing },
                _ => new List<EquipmentSlot> { targetSlot }
            };
        }
    }
}
