using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// Demo inventory controller that populates an inventory grid from a list of WardrobeItemSOs
    /// and connects clicks to the target character's WardrobeManager.
    /// </summary>
    [DisallowMultipleComponent]
    public class DemoInventoryUI : MonoBehaviour
    {
        [Header("Target Character")] 
        [Tooltip("The character's WardrobeManager to equip items on")] 
        [SerializeField] private WardrobeManager wardrobeManager;
        
        [Header("Available Items Database")]
        [Tooltip("List of item ScriptableObjects to display in the inventory grid")]
        [SerializeField] private List<WardrobeItemSO> availableItems = new();
        
        [Header("UI Grid Setup")]
        [Tooltip("Container Transform with a GridLayoutGroup for inventory buttons.")]
        [SerializeField] private Transform inventoryGridContainer;

        [Tooltip("Button prefab instantiated for each item in the grid.")] 
        [SerializeField] private GameObject inventoryItemButtonPrefab;
        
        [Header("Paper-doll Slots")]
        [Tooltip("List of equipment slots on the character paper-doll")]
        [SerializeField] private List<EquipmentSlotUI> equipmentSlots = new();
        
        private void Start()
        {
           InitSlots();
           PopulateInventoryGrid();
        }

        private void InitSlots()
        {
            if (wardrobeManager == null)
            {
                Debug.LogWarning("[DemoInventoryUI] WardrobeManager is not assigned in the Inspector");
                return;
            }
            foreach (var slotUI in equipmentSlots)
            {
                if (slotUI != null)
                {
                    slotUI.Initialize(wardrobeManager);
                }
            }
        }

        private void PopulateInventoryGrid()
        {
            if (inventoryGridContainer == null || inventoryItemButtonPrefab == null) return;

            foreach (Transform child in inventoryGridContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var itemSO in availableItems)
            {
                if (itemSO == null) continue;
                
                GameObject btnObj = Instantiate(inventoryItemButtonPrefab, inventoryGridContainer);
                btnObj.name = $"ItemBtn_{itemSO.ItemName}";
                
                Transform iconTransform = btnObj.transform.Find("ItemIcon");
                Image iconImg = iconTransform != null ? iconTransform.GetComponent<Image>() : btnObj.GetComponent<Image>();
                if (iconImg != null && itemSO.Icon != null)
                {
                    iconImg.sprite = itemSO.Icon;
                }
                
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => EquipItem(itemSO));
                }
            }
        }

        public void EquipItem(WardrobeItemSO itemSO)
        {
            if (wardrobeManager == null || itemSO == null) return;

            GameObject equippedObj = wardrobeManager.EquipItemSO(itemSO, itemSO.TargetSlot);

            if (equippedObj != null)
            {
                EquipmentSlotUI targetSlotUI = equipmentSlots.Find(s => s.SlotType == itemSO.TargetSlot);
                if (targetSlotUI != null)
                {
                    targetSlotUI.SetEquipmentItem(itemSO);
                }
            }
        }

        public void UnequipAll()
        {
            if (wardrobeManager != null)
            {
                wardrobeManager.UnequipAll();
            }
        }
    }
}