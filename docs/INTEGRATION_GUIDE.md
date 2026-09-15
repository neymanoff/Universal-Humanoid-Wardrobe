# Universal Humanoid Wardrobe — Integration & User Guide

This guide walks you through integrating the **Universal Humanoid Wardrobe** package into your Unity project, authoring equippable items, handling equipment persistence, and listening to wardrobe events.

---

## 1. Character Setup

### Step 1.1: Verify Model Import Settings
1. Select your character model file (`.fbx`) in the Project view.
2. In the Inspector, navigate to the **Rig** tab.
3. Ensure **Animation Type** is set to **Humanoid**.
4. Click **Configure...** to ensure all standard humanoid bones (Hips, Spine, Head, Arms, Legs) are mapped cleanly.

> [!NOTE]
> **Compatibility Note**: Rigid items (weapons, props, helmets) attach universally to any valid Humanoid avatar. Skinned clothing meshes require matching rest poses and compatible bone proportions with your host character model.

### Step 1.2: Add `WardrobeManager`
1. Select your character prefab or instance.
2. Ensure the GameObject contains an `Animator` component with a Humanoid avatar.
3. Add the `WardrobeManager` component.

---

## 2. Authoring Equippable Items

### Step 2.1: Creating `WardrobeItemSO`
Each item in your game is represented by a `WardrobeItemSO` ScriptableObject:

1. Right-click in Project view > **Create** > **Humanoid Wardrobe** > **Wardrobe Item**.
2. Configure core fields:
   * **Item Id**: A permanent, unique string identifier (e.g. `armor_chest_slavic_001`, `weapon_sword_iron_01`). **Never rely on asset filenames for save games.**
   * **Item Name**: In-game display name.
   * **Allowed Slots**: The slots this item can legally be equipped into (e.g. `MainHand` and `OffHand` for a versatile one-handed sword; `LeftRing` and `RightRing` for rings).
   * **Additional Occupied Slots**: Slots additionally occupied when equipped (e.g., for Two-Handed weapons, add `OffHand`).
   * **Icon**: UI sprite for inventory screens.
   * **Item Prefab**: The 3D prefab to spawn.

---

### Step 2.2: Creating Skinned Clothing & Armor
For items that deform with the body (tunics, pants, boots, gloves):

1. Export the clothing rigged to the same skeleton template as your character.
2. Create a Prefab from the imported FBX.
3. Add the `SkinnedMeshRemapper` component to the root of the prefab.
4. The remapper automatically reparents and maps bones to the host character's skeleton at runtime and ensures `rootBone` evaluates relative to the skeleton's root bone (`Hips`).

---

### Step 2.3: Creating Rigid Weapons, Props & Accessories
For items attached to bone sockets (swords, shields, jewelry, helmets):

1. Create a prefab containing your item mesh and colliders.
2. Add the `HumanoidAttachmentPoint` component to the root.
3. In the Inspector, configure:
   * **Target Bone**: Default humanoid bone (e.g. `RightHand`, `Head`).
   * **Offsets**: Local position, rotation, and scale relative to the socket.
   * **Auto Mirror for Left Slot**: Inverts X position and Y/Z rotation when equipped into `OffHand` or `LeftRing`.
4. **Tip — Capture Offsets in Scene**:
   * Temporarily place the weapon under your character's hand bone in the scene.
   * Align it visually.
   * Right-click `HumanoidAttachmentPoint` in the Inspector > **Capture Current Transform as Offsets**.
   * Apply overrides to the prefab.

---

## 3. Integrating with Gameplay & Inventory

The Wardrobe package is strictly **outward-notifying**: your game controls the wardrobe, and the wardrobe fires clean C# events.

### 3.1. Equipping an Item & Handling `EquipResult`
Call `Equip` with the target item and requested slot. The method returns a detailed `EquipResult`:

```csharp
using UnityEngine;
using Neymanoff.HumanoidWardrobe;

public class InventoryEquipmentController : MonoBehaviour
{
    [SerializeField] private WardrobeManager wardrobeManager;

    public void TryEquipFromInventory(WardrobeItemSO itemSO, EquipmentSlot slot)
    {
        EquipResult result = wardrobeManager.Equip(itemSO, slot);

        if (result.IsSuccess)
        {
            Debug.Log($"Successfully equipped {itemSO.ItemName} in {slot}!");
        }
        else
        {
            Debug.LogWarning($"Equip failed ({result.Status}): {result.ErrorMessage}");
            // e.g. Notify UI that slot is occupied or invalid
        }
    }
}
```

---

### 3.2. Subscribing to Wardrobe Events
Listen to wardrobe events to update character stats, play audio, or recalculate armor ratings:

```csharp
using UnityEngine;
using Neymanoff.HumanoidWardrobe;

public class CharacterStatsBridge : MonoBehaviour
{
    [SerializeField] private WardrobeManager wardrobe;

    private void OnEnable()
    {
        wardrobe.OnItemEquipped += HandleItemEquipped;
        wardrobe.OnItemUnequipped += HandleItemUnequipped;
        wardrobe.OnLoadoutChanged += HandleLoadoutChanged;
    }

    private void OnDisable()
    {
        wardrobe.OnItemEquipped -= HandleItemEquipped;
        wardrobe.OnItemUnequipped -= HandleItemUnequipped;
        wardrobe.OnLoadoutChanged -= HandleLoadoutChanged;
    }

    private void HandleItemEquipped(EquipmentSlot slot, WardrobeItemSO item, GameObject instance)
    {
        Debug.Log($"[Stats] Applied modifiers for {item.ItemName} on {slot}");
    }

    private void HandleItemUnequipped(EquipmentSlot slot, WardrobeItemSO item)
    {
        // Safe: item is passed directly even though the GameObject is already destroyed
        Debug.Log($"[Stats] Removed modifiers for {item.ItemName} from {slot}");
    }

    private void HandleLoadoutChanged(WardrobeLoadout loadout)
    {
        Debug.Log($"[SaveSystem] Loadout updated with {loadout.entries.Count} items.");
    }
}
```

---

## 4. Cross-Scene Persistence (Save & Load)

The recommended pattern for persistence is **Loadout Rehydration** via stable `ItemId`s.

### Saving the Loadout
Extract the lightweight serializable DTO:

```csharp
WardrobeLoadout loadout = wardrobeManager.GetCurrentLoadout();
string json = loadout.ToJson();
// Save `json` into PlayerPrefs, cloud save, or game save file
```

Example serialized JSON:
```json
{
  "entries": [
    { "slot": 0, "itemId": "helmet_slavic_001" },
    { "slot": 2, "itemId": "armor_chest_slavic_001" },
    { "slot": 8, "itemId": "weapon_greatsword_iron_001" }
  ]
}
```

### Loading & Rehydrating on Character Spawn
When the player spawns into a new level:

```csharp
public void InitializeCharacterLoadout(WardrobeManager characterWardrobe, string savedJson)
{
    WardrobeLoadout loadout = WardrobeLoadout.FromJson(savedJson);

    // Pass an item resolver (e.g. your Addressables manager or ScriptableObject catalog)
    characterWardrobe.ApplyLoadout(loadout, itemId => {
        return itemCatalog.GetItemById(itemId);
    });
}
```

---

## 5. In-Editor Preview Workflow

Inspect character outfits directly in the Scene view:
1. Select your character in the hierarchy.
2. In the `WardrobeManager` inspector, configure the **Default Loadout**.
3. Click **Preview Default Loadout** to instantiate preview objects.
4. Click **Clear Preview** to cleanly remove instances without scene hierarchy leaks.
