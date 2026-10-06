using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// Demo inventory controller managing mobile touch interactions, drag-and-drop equipping,
    /// paper-doll dual-wielding, on-screen action buttons, and interactive loadout persistence.
    /// </summary>
    [DisallowMultipleComponent]
    public class DemoInventoryUI : MonoBehaviour, IDropHandler
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

        [Header("Status Feedback Text (Optional)")]
        [Tooltip("Optional TextMeshProUGUI element to display status notifications.")]
        [SerializeField]
        private TextMeshProUGUI statusFeedbackText;

        [Header("Mobile Touch Controls (On-Screen)")]
        [Tooltip("Button to unequip all items.")]
        [SerializeField]
        private Button unequipAllButton;

        [Tooltip("Button to save active loadout preset.")]
        [SerializeField]
        private Button savePresetButton;

        [Tooltip("Button to load saved loadout preset.")]
        [SerializeField]
        private Button loadPresetButton;

        [Tooltip("Button to toggle weapon socket state between in-hand (Drawn) and stowed (Holstered).")]
        [SerializeField]
        private Button toggleHolsterButton;

        [Tooltip("Optional text element on the holster toggle button.")]
        [SerializeField]
        private TextMeshProUGUI toggleHolsterText;

        [Header("Body Profile Switcher (Mobile Showcase)")]
        [Tooltip("Available body scale profiles to demonstrate adaptive fitting (e.g. Standard, Dwarf, Giant).")]
        [SerializeField]
        private List<BodyScaleProfileSO> availableProfiles = new();

        [Tooltip("Button to switch to Standard/Default body profile.")]
        [SerializeField]
        private Button profileStandardButton;

        [Tooltip("Button to switch to Dwarf body profile.")]
        [SerializeField]
        private Button profileDwarfButton;

        [Tooltip("Button to switch to Giant body profile.")]
        [SerializeField]
        private Button profileGiantButton;

        private readonly Dictionary<WardrobeItemSO, GameObject> _itemButtonMap = new();

        private void Start()
        {
            InitSlots();
            InitMobileButtons();
            PopulateInventoryGrid();

            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquipmentChanged += HandleManagerEquipmentChanged;
                wardrobeManager.OnLoadoutChanged += HandleManagerLoadoutChanged;
                wardrobeManager.OnSocketStateChanged += HandleSocketStateChanged;
            }

            SetFeedback("Demo Ready: Tap or drag items to equip/dual-wield. Drop outside to unequip.");
        }

        private void OnDestroy()
        {
            if (wardrobeManager != null)
            {
                wardrobeManager.OnEquipmentChanged -= HandleManagerEquipmentChanged;
                wardrobeManager.OnLoadoutChanged -= HandleManagerLoadoutChanged;
                wardrobeManager.OnSocketStateChanged -= HandleSocketStateChanged;
            }
        }

        private void Update()
        {
            // Desktop keyboard fallback shortcuts for developer convenience
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
            else if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                ToggleHolsterState();
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

        private void InitMobileButtons()
        {
            if (unequipAllButton != null) unequipAllButton.onClick.AddListener(UnequipAll);
            if (savePresetButton != null) savePresetButton.onClick.AddListener(SaveCurrentLoadout);
            if (loadPresetButton != null) loadPresetButton.onClick.AddListener(LoadSavedLoadout);
            if (toggleHolsterButton != null) toggleHolsterButton.onClick.AddListener(ToggleHolsterState);

            if (profileStandardButton != null) profileStandardButton.onClick.AddListener(() => SetProfileByIndex(0));
            if (profileDwarfButton != null) profileDwarfButton.onClick.AddListener(() => SetProfileByIndex(1));
            if (profileGiantButton != null) profileGiantButton.onClick.AddListener(() => SetProfileByIndex(2));
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

                // Add mobile drag-and-drop capability
                var draggable = btnObj.AddComponent<DraggableItemUI>();
                draggable.Initialize(itemSO);
                draggable.OnItemClicked += EquipItem;

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
        /// If the item can fit in multiple slots (e.g. 1H weapon in MainHand or OffHand),
        /// it prioritizes an open/empty slot before replacing an occupied one.
        /// </summary>
        public void EquipItem(WardrobeItemSO itemSO)
        {
            if (wardrobeManager == null || itemSO == null) return;

            EquipmentSlot targetSlot = ResolveBestSlotForItem(itemSO);

            var result = wardrobeManager.Equip(itemSO, targetSlot);
            if (!result.IsSuccess)
            {
                SetFeedback($"Equip failed: {result.ErrorMessage}");
            }
            else
            {
                SetFeedback($"Equipped {itemSO.ItemName} in {targetSlot}");
            }
        }

        private EquipmentSlot ResolveBestSlotForItem(WardrobeItemSO itemSO)
        {
            if (itemSO.AllowedSlots != null && itemSO.AllowedSlots.Count > 0)
            {
                // Check if any allowed slot is currently open (supports dual-wielding)
                for (int i = 0; i < itemSO.AllowedSlots.Count; i++)
                {
                    EquipmentSlot candidate = itemSO.AllowedSlots[i];
                    if (wardrobeManager.GetEquippedItemData(candidate) == null)
                    {
                        return candidate;
                    }
                }
                return itemSO.AllowedSlots[0];
            }
            return itemSO.TargetSlot;
        }

        /// <summary>
        /// Toggles equipped weapons between in-hand (Drawn) and stowed (Holstered) attachment states.
        /// </summary>
        public void ToggleHolsterState()
        {
            if (wardrobeManager == null) return;

            // Prioritize MainHand, then OffHand
            EquipmentSlot targetSlot = EquipmentSlot.MainHand;
            if (wardrobeManager.GetEquippedItemData(targetSlot) == null)
            {
                targetSlot = EquipmentSlot.OffHand;
            }

            if (wardrobeManager.GetEquippedItemData(targetSlot) == null)
            {
                SetFeedback("No weapon equipped to holster!");
                return;
            }

            SocketAttachmentState currentState = wardrobeManager.GetItemSocketState(targetSlot);
            SocketAttachmentState newState = (currentState == SocketAttachmentState.Drawn)
                ? SocketAttachmentState.Holstered
                : SocketAttachmentState.Drawn;

            bool success = wardrobeManager.SetItemSocketState(targetSlot, newState);
            if (success)
            {
                string stateStr = newState == SocketAttachmentState.Holstered ? "Holstered" : "In Hand";
                SetFeedback($"Weapon state: {stateStr}");
                UpdateHolsterButtonText(newState);
            }
            else
            {
                SetFeedback("Equipped weapon does not support holstering!");
            }
        }

        private void UpdateHolsterButtonText(SocketAttachmentState state)
        {
            if (toggleHolsterText != null)
            {
                toggleHolsterText.text = state == SocketAttachmentState.Holstered
                    ? "Draw Weapon"
                    : "Holster Weapon";
            }
        }

        private void HandleSocketStateChanged(EquipmentSlot slot, SocketAttachmentState state)
        {
            UpdateHolsterButtonText(state);
        }

        /// <summary>
        /// Applies a body scale profile from the available profiles list by index.
        /// </summary>
        public void SetProfileByIndex(int index)
        {
            if (wardrobeManager == null) return;
            if (availableProfiles == null || availableProfiles.Count == 0)
            {
                ApplyProfileFallbackByIndex(index);
                return;
            }

            if (index >= 0 && index < availableProfiles.Count)
            {
                ApplyProfile(availableProfiles[index]);
            }
        }

        private void ApplyProfileFallbackByIndex(int index)
        {
            CharacterBodyScale bodyScale = wardrobeManager.GetComponent<CharacterBodyScale>();
            if (bodyScale == null)
            {
                bodyScale = wardrobeManager.gameObject.AddComponent<CharacterBodyScale>();
            }

            switch (index)
            {
                case 1: // Dwarf
                    bodyScale.BaseProfile = null;
                    bodyScale.OverridePropScale = true;
                    bodyScale.InstancePropScaleMultiplier = 0.75f;
                    wardrobeManager.SetBodyScale(bodyScale);
                    SetFeedback("Body Profile: Dwarf (0.75x Prop Scale)");
                    break;
                case 2: // Giant
                    bodyScale.BaseProfile = null;
                    bodyScale.OverridePropScale = true;
                    bodyScale.InstancePropScaleMultiplier = 1.35f;
                    wardrobeManager.SetBodyScale(bodyScale);
                    SetFeedback("Body Profile: Giant (1.35x Prop Scale)");
                    break;
                default: // Standard
                    bodyScale.BaseProfile = null;
                    bodyScale.OverridePropScale = false;
                    bodyScale.InstancePropScaleMultiplier = 1f;
                    wardrobeManager.SetBodyScale(bodyScale);
                    SetFeedback("Body Profile: Standard (1.0x Prop Scale)");
                    break;
            }
        }

        public void ApplyProfile(BodyScaleProfileSO profile)
        {
            if (wardrobeManager == null) return;

            CharacterBodyScale bodyScale = wardrobeManager.GetComponent<CharacterBodyScale>();
            if (bodyScale == null)
            {
                bodyScale = wardrobeManager.gameObject.AddComponent<CharacterBodyScale>();
            }

            bodyScale.BaseProfile = profile;
            bodyScale.OverridePropScale = false;
            wardrobeManager.SetBodyScale(bodyScale);

            string profName = profile != null ? profile.ProfileId : "Default";
            SetFeedback($"Body Profile: {profName} (Adaptive Fit Applied)");
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
            SetFeedback($"Preset Saved ({loadout.entries.Count} items)");
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
                SetFeedback("No saved loadout found! Save a preset first.");
                return;
            }

            string json = PlayerPrefs.GetString(SaveKey);
            WardrobeLoadout loadout = WardrobeLoadout.FromJson(json);
            wardrobeManager.ApplyLoadout(loadout, FindItemById);

            Debug.Log($"[DemoInventoryUI] Preset restored ({loadout.entries.Count} items).");
            SetFeedback($"Preset Restored ({loadout.entries.Count} items)");
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
                SetFeedback("All items unequipped.");
            }
        }

        /// <summary>
        /// Handles drop events on the inventory background or grid, unequipping dragged items.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (wardrobeManager == null || eventData.pointerDrag == null) return;

            if (eventData.pointerDrag.TryGetComponent<EquipmentSlotUI>(out var slotUI))
            {
                wardrobeManager.Unequip(slotUI.SlotType);
                SetFeedback($"Unequipped {slotUI.SlotType}");
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
