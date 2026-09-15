using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    ///  Demo inventory controller that manages inventory grid items, dynamic hiding of equipped items.
    /// </summary>
    [DisallowMultipleComponent]
    public class DemoInventoryUI : MonoBehaviour
    {
        [Header("Target Character")] [Tooltip("The character's WardrobeManager to equip items on")] [SerializeField]
        private WardrobeManager wardrobeManager;

        [Header("Available Items Database")]
        [Tooltip("List of item ScriptableObjects to display in the inventory grid")]
        [SerializeField]
        private List<WardrobeItemSO> availableItems = new();

        [Header("UI Grid Setup")]
        [Tooltip("Container Transform with a GridLayoutGroup for inventory buttons.")]
        [SerializeField]
        private Transform inventoryGridContainer;

        [Tooltip("Button prefab instantiated for each item in the grid.")] [SerializeField]
        private GameObject inventoryItemButtonPrefab;

        [Header("Paper-doll Slots")] [Tooltip("List of equipment slots on the character paper-doll")] [SerializeField]
        private List<EquipmentSlotUI> equipmentSlots = new();

        private readonly Dictionary<WardrobeItemSO, GameObject> _itemButtonMap = new();

        private void Start()
        {
            InitSlots();
            PopulateInventoryGrid();

            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquipmentChanged += HandleManagerEquipmentChanged;
            }
        }

        private void OnDestroy()
        {
            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquipmentChanged -= HandleManagerEquipmentChanged;
            }
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

            _itemButtonMap.Clear();

            foreach (var itemSO in availableItems)
            {
                if (itemSO == null) continue;

                GameObject btnObj = Instantiate(inventoryItemButtonPrefab, inventoryGridContainer);
                btnObj.name = $"ItemBtn_{itemSO.ItemName}";

                Transform iconTransform = btnObj.transform.Find("ItemIcon");
                Image iconImg = iconTransform != null
                    ? iconTransform.GetComponent<Image>()
                    : btnObj.GetComponent<Image>();
                if (iconImg != null && itemSO.Icon != null)
                {
                    iconImg.sprite = itemSO.Icon;
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => EquipItem(itemSO));
                }

                _itemButtonMap[itemSO] = btnObj;
            }
        }

        public void EquipItem(WardrobeItemSO itemSO)
        {
            if (wardrobeManager == null || itemSO == null) return;
            wardrobeManager.EquipItemSO(itemSO, itemSO.TargetSlot);
        }

        private void HandleManagerEquipmentChanged(EquipmentSlot slot, GameObject equippedObject)
        {
            RefreshAllUI();
        }

        private void RefreshAllUI()
        {
            if (wardrobeManager == null) return;
            HashSet<WardrobeItemSO> currentlyEquippedSO = new();
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                WardrobeItemSO equippedSO = wardrobeManager.GetEquippedItemData(slot);
                if (equippedSO != null)
                {
                    currentlyEquippedSO.Add(equippedSO);
                }
                
                EquipmentSlotUI slotUI = equipmentSlots.Find(s => s.slotType == slot);
                if (slotUI != null)
                {
                    slotUI.SetEquipmentItem(equippedSO);
                }
            }
            
            WardrobeItemSO mainHandSO = wardrobeManager.GetEquippedItemData(EquipmentSlot.MainHand);
            if (mainHandSO != null && mainHandSO.Restriction == ItemSlotRestriction.TwoHanded)
            {
                EquipmentSlotUI offHandUI = equipmentSlots.Find(s => s.SlotType == EquipmentSlot.OffHand);
                if (offHandUI != null)
                {
                    offHandUI.SetBlockedByTwoHanded(mainHandSO);
                }
            }

            foreach (var pair in _itemButtonMap)
            {
                WardrobeItemSO itemSO = pair.Key;
                GameObject btnObj = pair.Value;
                if (btnObj != null)
                {
                    bool isEquipped = currentlyEquippedSO.Contains(itemSO);
                    btnObj.SetActive(!isEquipped);
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