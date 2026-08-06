using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
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

        [Tooltip("The equipment slot this item fits into")] [SerializeField]
        private EquipmentSlot targetSlot = EquipmentSlot.MainHand;
        
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
    }
}
