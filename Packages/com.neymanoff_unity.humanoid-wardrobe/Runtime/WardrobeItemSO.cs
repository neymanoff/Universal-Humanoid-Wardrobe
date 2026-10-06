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
    /// Alternative 3D prefab used for a specific character body profile (e.g. Dwarf, Orc).
    /// </summary>
    [System.Serializable]
    public struct ProfilePrefabOverride
    {
        [Tooltip("Target body profile identifier (e.g. 'Dwarf', 'Orc', 'Heavy', 'Female').")]
        public string profileId;

        [Tooltip("Alternative 3D prefab spawned when equipped on a character matching this profile.")]
        public GameObject prefab;

        public ProfilePrefabOverride(string profileId, GameObject prefab)
        {
            this.profileId = profileId;
            this.prefab = prefab;
        }
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

        [Header("Profile / Variant Prefabs")]
        [Tooltip("Alternative 3D prefabs for specific body profiles (e.g. custom mesh for Dwarf or Orc). If no match is found, ItemPrefab is used as fallback.")]
        [SerializeField] private List<ProfilePrefabOverride> profilePrefabOverrides = new();

        [Header("Body Clipping Prevention")]
        [Tooltip("Anatomical regions of the base character body concealed by this item. Concealed modular sub-meshes will be deactivated.")]
        [SerializeField] private BodyPartMask hiddenBodyParts = BodyPartMask.None;

        [Tooltip("Blendshape names on the base character body to set to 100% weight to tuck/shrink skin under this clothing item.")]
        [SerializeField] private List<string> shrinkBlendShapes = new();

        public IReadOnlyList<ProfilePrefabOverride> ProfilePrefabOverrides => profilePrefabOverrides;

        /// <summary>
        /// Retrieves the appropriate 3D prefab for the specified body profile.
        /// Falls back to ItemPrefab if no profile match is found or profileId is null/empty.
        /// </summary>
        public GameObject GetPrefabForProfile(string profileId)
        {
            if (!string.IsNullOrEmpty(profileId) && profilePrefabOverrides != null)
            {
                for (int i = 0; i < profilePrefabOverrides.Count; i++)
                {
                    if (string.Equals(profilePrefabOverrides[i].profileId, profileId, System.StringComparison.OrdinalIgnoreCase) &&
                        profilePrefabOverrides[i].prefab != null)
                    {
                        return profilePrefabOverrides[i].prefab;
                    }
                }
            }
            return itemPrefab;
        }

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
        public BodyPartMask HiddenBodyParts => hiddenBodyParts;
        public IReadOnlyList<string> ShrinkBlendShapes => shrinkBlendShapes;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Testing helper to configure item properties in-memory without inspector serialization.
        /// </summary>
        public void ConfigureForTest(
            string id,
            string name,
            GameObject prefab,
            IEnumerable<EquipmentSlot> allowed,
            IEnumerable<EquipmentSlot> additional = null,
            BodyPartMask hiddenParts = BodyPartMask.None,
            IEnumerable<string> shrinkShapes = null,
            IEnumerable<ProfilePrefabOverride> prefabOverrides = null)
        {
            this.itemId = id;
            this.itemName = name;
            this.itemPrefab = prefab;
            this.allowedSlots = allowed != null ? new List<EquipmentSlot>(allowed) : new List<EquipmentSlot>();
            this.additionalOccupiedSlots = additional != null ? new List<EquipmentSlot>(additional) : new List<EquipmentSlot>();
            this.hiddenBodyParts = hiddenParts;
            this.shrinkBlendShapes = shrinkShapes != null ? new List<string>(shrinkShapes) : new List<string>();
            this.profilePrefabOverrides = prefabOverrides != null ? new List<ProfilePrefabOverride>(prefabOverrides) : new List<ProfilePrefabOverride>();
        }
#endif
    }
}
