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
        private const string AppearanceSaveKey = "WardrobeDemo_SavedAppearance";

        [Header("Target Character")]
        [Tooltip("The character's WardrobeManager to equip items on.")]
        [SerializeField]
        private WardrobeManager wardrobeManager;

        [Tooltip("Optional CharacterAppearance component on the character for hairstyle, beard, and color customization.")]
        [SerializeField]
        private CharacterAppearance characterAppearance;

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

        [Header("Cosmetic Appearance Controls (Optional)")]
        [Tooltip("Button to cycle through available hairstyles (including 'None').")]
        [SerializeField]
        private Button cycleHairButton;

        [Tooltip("Button to cycle through available beards (including 'None').")]
        [SerializeField]
        private Button cycleBeardButton;

        [Tooltip("Button to cycle through hair color presets from the cosmetic catalog.")]
        [SerializeField]
        private Button cycleHairColorButton;

        [Tooltip("Button to cycle through beard color presets from the cosmetic catalog.")]
        [SerializeField]
        private Button cycleBeardColorButton;

        [Tooltip("Optional Toggle to link/unlink hair and beard colors.")]
        [SerializeField]
        private Toggle linkColorsToggle;

        [Tooltip("Button to toggle helmet visual visibility while keeping the helmet equipped in the Head slot.")]
        [SerializeField]
        private Button toggleHelmetVisibilityButton;

        [Tooltip("Optional text element on the helmet visibility toggle button.")]
        [SerializeField]
        private TextMeshProUGUI toggleHelmetVisibilityText;

        private readonly Dictionary<WardrobeItemSO, GameObject> _itemButtonMap = new();
        private int _currentHairColorPresetIndex;
        private int _currentBeardColorPresetIndex;

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
            if (wardrobeManager != null && characterAppearance == null)
            {
                characterAppearance = wardrobeManager.GetComponent<CharacterAppearance>();
            }

            if (unequipAllButton != null) unequipAllButton.onClick.AddListener(UnequipAll);
            if (savePresetButton != null) savePresetButton.onClick.AddListener(SaveCurrentLoadout);
            if (loadPresetButton != null) loadPresetButton.onClick.AddListener(LoadSavedLoadout);
            if (toggleHolsterButton != null) toggleHolsterButton.onClick.AddListener(ToggleHolsterState);

            if (profileStandardButton != null) profileStandardButton.onClick.AddListener(() => SetProfileByIndex(0));
            if (profileDwarfButton != null) profileDwarfButton.onClick.AddListener(() => SetProfileByIndex(1));
            if (profileGiantButton != null) profileGiantButton.onClick.AddListener(() => SetProfileByIndex(2));

            if (cycleHairButton != null) cycleHairButton.onClick.AddListener(CycleHairstyle);
            if (cycleBeardButton != null) cycleBeardButton.onClick.AddListener(CycleBeard);
            if (cycleHairColorButton != null) cycleHairColorButton.onClick.AddListener(CycleHairColor);
            if (cycleBeardColorButton != null) cycleBeardColorButton.onClick.AddListener(CycleBeardColor);
            if (toggleHelmetVisibilityButton != null)
            {
                toggleHelmetVisibilityButton.onClick.AddListener(ToggleHelmetVisibility);
                UpdateHelmetButtonText();
            }
            if (linkColorsToggle != null)
            {
                if (characterAppearance != null)
                {
                    linkColorsToggle.SetIsOnWithoutNotify(characterAppearance.LinkHairAndBeardColor);
                }
                linkColorsToggle.onValueChanged.AddListener(SetLinkColors);
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

                // Add mobile drag-and-drop capability (handles both drag and non-drag click)
                var draggable = btnObj.AddComponent<DraggableItemUI>();
                draggable.Initialize(itemSO);
                draggable.OnItemClicked += EquipItem;

                _itemButtonMap[itemSO] = btnObj;
            }
        }

        /// <summary>
        /// Equips the specified WardrobeItemSO onto the character.
        /// If the item can fit in multiple slots (e.g. 1H weapon in MainHand or OffHand),
        /// it prioritizes an open/empty slot before replacing the primary slot.
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
                EquipmentSlot actualSlot = result.Instance != null ? result.Instance.PrimarySlot : targetSlot;
                SetFeedback($"Equipped {itemSO.ItemName} in {actualSlot}");
            }
        }

        private EquipmentSlot ResolveBestSlotForItem(WardrobeItemSO itemSO)
        {
            bool allowDualTwoHanded = wardrobeManager != null && wardrobeManager.AllowDualTwoHanded;
            IReadOnlyList<EquipmentSlot> allowed = itemSO.GetAllowedSlots(allowDualTwoHanded);
            if (allowed != null && allowed.Count > 0)
            {
                bool offHandBlockedByTwoHanded = false;
                if (wardrobeManager != null && !allowDualTwoHanded)
                {
                    WardrobeItemSO mainHandItem = wardrobeManager.GetEquippedItemData(EquipmentSlot.MainHand);
                    if (mainHandItem != null && mainHandItem.IsTwoHandedWeapon)
                    {
                        offHandBlockedByTwoHanded = true;
                    }
                }

                // 1. Check if any allowed slot is currently open and not blocked by a 2H weapon
                for (int i = 0; i < allowed.Count; i++)
                {
                    EquipmentSlot candidate = allowed[i];
                    if (candidate == EquipmentSlot.OffHand && offHandBlockedByTwoHanded)
                    {
                        continue;
                    }

                    if (wardrobeManager.GetEquippedItemData(candidate) == null)
                    {
                        return candidate;
                    }
                }

                // 2. If all allowed slots are occupied, replace the primary allowed slot (e.g. MainHand)
                // so clicking a 1H weapon never knocks out a Shield in OffHand
                return allowed[0];
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
        /// Cycles through available hairstyles in the character's <see cref="CosmeticCatalogSO"/>, including "None".
        /// </summary>
        public void CycleHairstyle()
        {
            if (characterAppearance == null && wardrobeManager != null)
            {
                characterAppearance = wardrobeManager.GetComponent<CharacterAppearance>();
            }

            if (characterAppearance == null || characterAppearance.Catalog == null)
            {
                SetFeedback("CharacterAppearance or CosmeticCatalogSO is not assigned.");
                return;
            }

            IReadOnlyList<CosmeticOptionSO> options = characterAppearance.Catalog.HairOptions;
            if (options == null || options.Count == 0)
            {
                characterAppearance.ClearHair();
                SetFeedback("Hair: None");
                return;
            }

            string currentId = characterAppearance.CurrentHairId;
            if (string.IsNullOrEmpty(currentId))
            {
                characterAppearance.SetHair(options[0]);
                SetFeedback($"Hair: {options[0].DisplayName}");
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && string.Equals(options[i].OptionId, currentId, StringComparison.OrdinalIgnoreCase))
                {
                    int nextIndex = i + 1;
                    if (nextIndex >= options.Count)
                    {
                        characterAppearance.ClearHair();
                        SetFeedback("Hair: None");
                    }
                    else
                    {
                        characterAppearance.SetHair(options[nextIndex]);
                        SetFeedback($"Hair: {options[nextIndex].DisplayName}");
                    }
                    return;
                }
            }

            characterAppearance.SetHair(options[0]);
            SetFeedback($"Hair: {options[0].DisplayName}");
        }

        /// <summary>
        /// Cycles through available beards in the character's <see cref="CosmeticCatalogSO"/>, including "None".
        /// </summary>
        public void CycleBeard()
        {
            if (characterAppearance == null && wardrobeManager != null)
            {
                characterAppearance = wardrobeManager.GetComponent<CharacterAppearance>();
            }

            if (characterAppearance == null || characterAppearance.Catalog == null)
            {
                SetFeedback("CharacterAppearance or CosmeticCatalogSO is not assigned.");
                return;
            }

            IReadOnlyList<CosmeticOptionSO> options = characterAppearance.Catalog.BeardOptions;
            if (options == null || options.Count == 0)
            {
                characterAppearance.ClearBeard();
                SetFeedback("Beard: None");
                return;
            }

            string currentId = characterAppearance.CurrentBeardId;
            if (string.IsNullOrEmpty(currentId))
            {
                characterAppearance.SetBeard(options[0]);
                SetFeedback($"Beard: {options[0].DisplayName}");
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && string.Equals(options[i].OptionId, currentId, StringComparison.OrdinalIgnoreCase))
                {
                    int nextIndex = i + 1;
                    if (nextIndex >= options.Count)
                    {
                        characterAppearance.ClearBeard();
                        SetFeedback("Beard: None");
                    }
                    else
                    {
                        characterAppearance.SetBeard(options[nextIndex]);
                        SetFeedback($"Beard: {options[nextIndex].DisplayName}");
                    }
                    return;
                }
            }

            characterAppearance.SetBeard(options[0]);
            SetFeedback($"Beard: {options[0].DisplayName}");
        }

        /// <summary>
        /// Cycles through hair color presets from the assigned <see cref="CosmeticCatalogSO"/>.
        /// </summary>
        public void CycleHairColor()
        {
            if (characterAppearance == null && wardrobeManager != null)
            {
                characterAppearance = wardrobeManager.GetComponent<CharacterAppearance>();
            }

            if (characterAppearance == null || characterAppearance.Catalog == null)
            {
                return;
            }

            IReadOnlyList<CosmeticColorPreset> presets = characterAppearance.Catalog.ColorPresets;
            if (presets == null || presets.Count == 0)
            {
                return;
            }

            _currentHairColorPresetIndex = (_currentHairColorPresetIndex + 1) % presets.Count;
            CosmeticColorPreset preset = presets[_currentHairColorPresetIndex];
            characterAppearance.SetHairColor(preset.color);
            SetFeedback($"Hair Color: {preset.displayName}");
        }

        /// <summary>
        /// Cycles through beard color presets from the assigned <see cref="CosmeticCatalogSO"/>.
        /// Automatically unlinks hair and beard color if needed so independent beard tinting can be previewed.
        /// </summary>
        public void CycleBeardColor()
        {
            if (characterAppearance == null && wardrobeManager != null)
            {
                characterAppearance = wardrobeManager.GetComponent<CharacterAppearance>();
            }

            if (characterAppearance == null || characterAppearance.Catalog == null)
            {
                return;
            }

            IReadOnlyList<CosmeticColorPreset> presets = characterAppearance.Catalog.ColorPresets;
            if (presets == null || presets.Count == 0)
            {
                return;
            }

            _currentBeardColorPresetIndex = (_currentBeardColorPresetIndex + 1) % presets.Count;
            CosmeticColorPreset preset = presets[_currentBeardColorPresetIndex];
            characterAppearance.SetBeardColor(preset.color);
            SetFeedback($"Beard Color: {preset.displayName}");
        }

        /// <summary>
        /// Enables or disables linking between hair and beard colors.
        /// </summary>
        public void SetLinkColors(bool link)
        {
            if (characterAppearance == null && wardrobeManager != null)
            {
                characterAppearance = wardrobeManager.GetComponent<CharacterAppearance>();
            }

            if (characterAppearance != null)
            {
                characterAppearance.LinkHairAndBeardColor = link;
                SetFeedback($"Link Hair & Beard Colors: {(link ? "ON" : "OFF")}");
            }
        }

        /// <summary>
        /// Toggles whether the equipped helmet in <see cref="EquipmentSlot.Head"/> is visually hidden
        /// while remaining equipped (so the player's head, hairstyle, and beard stay visible).
        /// </summary>
        public void ToggleHelmetVisibility()
        {
            if (wardrobeManager == null)
            {
                return;
            }

            bool hidden = wardrobeManager.ToggleHelmetVisual();
            UpdateHelmetButtonText();
            SetFeedback(hidden ? "Helmet Visual: Hidden (Head & Hair Visible)" : "Helmet Visual: Shown");
        }

        private void UpdateHelmetButtonText()
        {
            if (toggleHelmetVisibilityText != null && wardrobeManager != null)
            {
                toggleHelmetVisibilityText.text = wardrobeManager.HideHelmetVisual
                    ? "Show Helmet"
                    : "Hide Helmet";
            }
        }

        /// <summary>
        /// Saves the active loadout (and optional cosmetic appearance) to PlayerPrefs in JSON format.
        /// </summary>
        public void SaveCurrentLoadout()
        {
            if (wardrobeManager == null) return;

            WardrobeLoadout loadout = wardrobeManager.GetCurrentLoadout();
            string json = loadout.ToJson(prettyPrint: true);
            PlayerPrefs.SetString(SaveKey, json);

            if (characterAppearance != null)
            {
                CharacterAppearanceData appearanceData = characterAppearance.GetAppearanceData();
                PlayerPrefs.SetString(AppearanceSaveKey, appearanceData.ToJson(prettyPrint: true));
            }

            PlayerPrefs.Save();

            Debug.Log($"[DemoInventoryUI] Preset saved ({loadout.entries.Count} items):\n{json}");
            SetFeedback($"Preset Saved ({loadout.entries.Count} items)");
        }

        /// <summary>
        /// Restores a previously saved loadout (and optional cosmetic appearance) from PlayerPrefs.
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

            if (characterAppearance != null && PlayerPrefs.HasKey(AppearanceSaveKey))
            {
                string appearanceJson = PlayerPrefs.GetString(AppearanceSaveKey);
                CharacterAppearanceData appearanceData = CharacterAppearanceData.FromJson(appearanceJson);
                characterAppearance.ApplyAppearanceData(appearanceData);
            }

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

            if (eventData.pointerDrag.TryGetComponent<DraggableItemUI>(out var draggable))
            {
                draggable.ClearDragProxy();
            }
            else if (eventData.pointerDrag.TryGetComponent<EquipmentSlotUI>(out var slotUI))
            {
                slotUI.ClearDragProxy();
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

            bool allowDualTwoHanded = wardrobeManager.AllowDualTwoHanded;
            WardrobeItemSO mainHandSO = wardrobeManager.GetEquippedItemData(EquipmentSlot.MainHand);
            if (mainHandSO != null && mainHandSO.IsTwoHandedWeapon && !allowDualTwoHanded)
            {
                EquipmentSlotUI offHandUI = equipmentSlots.Find(s => s.SlotType == EquipmentSlot.OffHand);
                if (offHandUI != null)
                {
                    offHandUI.SetBlockedByTwoHanded(mainHandSO);
                }
            }

            // 1 item = 1 slot: once an item from the inventory grid is equipped in any slot, hide its button
            foreach (var pair in _itemButtonMap)
            {
                WardrobeItemSO itemSO = pair.Key;
                GameObject btnObj = pair.Value;
                if (btnObj != null && itemSO != null)
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
