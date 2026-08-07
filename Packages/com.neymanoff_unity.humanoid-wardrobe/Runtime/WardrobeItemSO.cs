using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    ///<summary>
    /// Rules for how item ca be equipped across hands/slots.
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
    ///<summary>
    /// ScriptableObject representing an equippable item in the wardrobe system.
    /// Holds UI metadata, slot target, and 3D prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWardrobeItem" ,menuName = "Humanoid Wardrobe/Wardrobe Item")]
    public class WardrobeItemSO : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("The display name of the item")]
        [SerializeField] private string itemName = "New Item";
        
        [Tooltip("Restriction mode for equipping this item")]
        [SerializeField] private ItemSlotRestriction restriction = ItemSlotRestriction.SpecificSlotOnly;

        [Tooltip("The equipment slot this item fits into")] [SerializeField]
        private EquipmentSlot targetSlot = EquipmentSlot.Head;
        
        [Header("Visuals (UI)")]
        [Tooltip("The 2D sprite icon representing the item in the inventory grid.")]
        [SerializeField] private Sprite icon;
        
        [Header("Visuals (3D)")]
        [Tooltip("The 3D prefab to spawn. Must contain SkinnedMeshRemapper or HumanoidAttachmentPoint.")]
        [SerializeField] private GameObject itemPrefab;
        
        public string ItemName => itemName;
        public EquipmentSlot TargetSlot => targetSlot;
        public Sprite Icon => icon;
        public GameObject ItemPrefab => itemPrefab;
        public ItemSlotRestriction Restriction => restriction;

        ///<summary>
        /// Checks if this item can be legally equipped into the requsted slot.
        ///</summary>
        public bool CanFitInSlot(EquipmentSlot slot)
        {
            switch (restriction)
            {
                case ItemSlotRestriction.SpecificSlotOnly:
                    return slot == targetSlot;
                case ItemSlotRestriction.OneHanded:
                    return slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand;
                case ItemSlotRestriction.TwoHanded:
                    return slot == EquipmentSlot.MainHand;
                case ItemSlotRestriction.MainHandOnly:
                    return slot == EquipmentSlot.MainHand;
                case ItemSlotRestriction.OffHandOnly:
                    return slot == EquipmentSlot.OffHand;
                case ItemSlotRestriction.AnyRing:
                    return slot == EquipmentSlot.LeftRing || slot == EquipmentSlot.RightRing;
                default:
                    return slot == targetSlot;
            }
        }
    }
}
