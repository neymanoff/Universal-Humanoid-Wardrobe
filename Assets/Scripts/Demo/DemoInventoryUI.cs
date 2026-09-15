using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// Demo inventory controller managing inventory grid display, paper-doll slots,
    /// dynamic item equip/unequip, and interactive loadout persistence (Save/Load).
    /// </summary>
    [DisallowMultipleComponent]
    public class DemoInventoryUI : MonoBehaviour
    {
        private const string SaveKey = "WardrobeDemo_SavedLoadout";

        [Header("Target Character")]
        [Tooltip("The character's WardrobeManager to equip items on.")]
        [SerializeField]
        private WardrobeManager wardrobeManager;

        [Header("Available Items Database")]
        [Tooltip("List of item ScriptableObjects to display in the inventory grid.")]
        [SerializeField]
        private List<WardrobeItemSO> availableItems = new();

        [Header("UI Grid Setup")]
        [Tooltip("Container Transform with a GridLayoutGroup for inventory buttons.")]
        [SerializeField]
        private Transform inventoryGridContainer;

        [Tooltip("Button prefab instantiated for each item in the grid.")]
        [SerializeField]
        private GameObject inventoryItemButtonPrefab;

        [Header("Paper-doll Slots")]
        [Tooltip("List of equipment slots on the character paper-doll.")]
        [SerializeField]
        private List<EquipmentSlotUI> equipmentSlots = new();

        [Header("Persistence Demo Feedback (Optional)")]
        [Tooltip("Optional TextMeshProUGUI element to display save/load notifications.")]
        [SerializeField]
        private TextMeshProUGUI statusFeedbackText;

        private readonly Dictionary<WardrobeItemSO, GameObject> _itemButtonMap = new();

        private void Start()
        {
            InitSlots();
            PopulateInventoryGrid();

            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquipmentChanged += HandleManagerEquipmentChanged;
                wardrobeManager.OnLoadoutChanged += HandleManagerLoadoutChanged;
            }

            SetFeedback("Demo Ready: Click items to equip | [F5] Save | [F9] Load | [C] Clear");
        }

        private void OnDestroy()
        {
            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquipmentChanged -= HandleManagerEquipmentChanged;
                wardrobeManager.OnLoadoutChanged -= HandleManagerLoadoutChanged;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveCurrentLoadout();
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                LoadSavedLoadout();
            }
            else if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                UnequipAll();
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

        /// <summary>
        /// Equips the specified WardrobeItemSO onto the character.
        /// </summary>
        public void EquipItem(WardrobeItemSO itemSO)
        {
            if (wardrobeManager == null || itemSO == null) return;

            EquipmentSlot targetSlot = (itemSO.AllowedSlots != null && itemSO.AllowedSlots.Count > 0)
                ? itemSO.AllowedSlots[0]
                : itemSO.TargetSlot;

            var result = wardrobeManager.Equip(itemSO, targetSlot);
            if (!result.IsSuccess)
            {
                SetFeedback($"Equip failed: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Saves the active loadout to PlayerPrefs in JSON format.
        /// </summary>
        public void SaveCurrentLoadout()
        {
            if (wardrobeManager == null) return;

            WardrobeLoadout loadout = wardrobeManager.GetCurrentLoadout();
            string json = loadout.ToJson(prettyPrint: true);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();

            Debug.Log($"[DemoInventoryUI] Preset saved ({loadout.entries.Count} items):\n{json}");
            SetFeedback($"Preset Saved ({loadout.entries.Count} items) [F5]");
        }

        /// <summary>
        /// Restores a previously saved loadout from PlayerPrefs.
        /// </summary>
        public void LoadSavedLoadout()
        {
            if (wardrobeManager == null) return;

            if (!PlayerPrefs.HasKey(SaveKey))
            {
                Debug.LogWarning("[DemoInventoryUI] No saved loadout preset found in PlayerPrefs.");
                SetFeedback("No saved loadout found! Press [F5] to save first.");
                return;
            }

            string json = PlayerPrefs.GetString(SaveKey);
            WardrobeLoadout loadout = WardrobeLoadout.FromJson(json);
            wardrobeManager.ApplyLoadout(loadout, FindItemById);

            Debug.Log($"[DemoInventoryUI] Preset restored ({loadout.entries.Count} items).");
            SetFeedback($"Preset Restored ({loadout.entries.Count} items) [F9]");
        }

        /// <summary>
        /// Resolves an item from available inventory by stable ItemId.
        /// </summary>
        public WardrobeItemSO FindItemById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < availableItems.Count; i++)
            {
                if (availableItems[i] != null && string.Equals(availableItems[i].ItemId, id, StringComparison.OrdinalIgnoreCase))
                {
                    return availableItems[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Unequips all items from the character.
        /// </summary>
        public void UnequipAll()
        {
            if (wardrobeManager != null)
            {
                wardrobeManager.UnequipAll();
                SetFeedback("All items unequipped [C]");
            }
        }

        private void HandleManagerEquipmentChanged(EquipmentSlot slot, GameObject equippedObject)
        {
            RefreshAllUI();
        }

        private void HandleManagerLoadoutChanged(WardrobeLoadout loadout)
        {
            RefreshAllUI();
        }

        private void RefreshAllUI()
        {
            if (wardrobeManager == null) return;

            HashSet<WardrobeItemSO> currentlyEquippedSO = new();
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                WardrobeItemSO equippedSO = wardrobeManager.GetEquippedItemData(slot);
                if (equippedSO != null)
                {
                    currentlyEquippedSO.Add(equippedSO);
                }

                EquipmentSlotUI slotUI = equipmentSlots.Find(s => s.SlotType == slot);
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

        private void SetFeedback(string message)
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = message;
            }
        }
    }
}
