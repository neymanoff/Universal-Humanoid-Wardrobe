# Universal Humanoid Wardrobe — Integration & User Guide

This guide walks you through integrating the **Universal Humanoid Wardrobe** package into your Unity project, setting up humanoid characters, configuring clothing and weapons, connecting to game inventory systems, and saving equipment state across scenes.

---

## 1. Character Setup

The package works with any 3D model rigged to Unity's standard **Humanoid** avatar.

### Step 1.1: Verify Model Import Settings
1. Select your character model file (`.fbx`) in the Project view.
2. In the Inspector, navigate to the **Rig** tab.
3. Ensure **Animation Type** is set to **Humanoid**.
4. Click **Configure...** to ensure all required bones (Hips, Spine, Head, Arms, Legs) are mapped correctly without errors.

### Step 1.2: Add `WardrobeManager`
1. Open your character prefab or select the character instance in your scene.
2. Ensure the root GameObject contains an `Animator` component with a valid Humanoid Avatar.
3. Add the `WardrobeManager` component:
   * Click **Add Component** > search for **WardrobeManager** (or find it under `Humanoid Wardrobe/WardrobeManager`).
4. *(Optional)* Configure the **Default Loadout** list with items you want equipped automatically on game start.

---

## 2. Creating Equippable Items

The wardrobe system supports two item categories:
1. **Skinned Clothing & Armor** (meshes that deform with body animations).
2. **Rigid Props & Weapons** (static objects attached to specific bones).

---

### Step 2.1: Creating Skinned Clothing / Armor
Skinned meshes (e.g. chest armor, pants, boots, gloves) must share the same bone structure and proportions as your base humanoid rig.

1. **Export from 3D Software**: Export your clothing mesh rigged to the standard skeleton as an FBX file.
2. **Import into Unity**:
   * Set the Rig tab to **Humanoid** (recommended) or ensure bone naming matches your base character.
3. **Create the Prefab**:
   * Drag the FBX into your scene, unpack or create a Prefab Variant.
   * Add the `SkinnedMeshRemapper` component to the root of the prefab.
4. **Configure Bounding & Culling**:
   * Ensure `SkinnedMeshRenderer` components on your clothing have appropriate materials assigned.
   * The `SkinnedMeshRemapper` will automatically bind the bones to the target character skeleton at runtime and clean up duplicate bone nodes.

---

### Step 2.2: Creating Rigid Weapons, Props & Accessories
Rigid items (e.g. swords, shields, staves, rings, static helmets) attach directly to a specific humanoid bone socket.

1. **Create the Prefab**:
   * Create a prefab containing your prop mesh, colliders, and materials.
2. **Add `HumanoidAttachmentPoint`**:
   * Add the `HumanoidAttachmentPoint` component to the root of the prefab.
3. **Configure Offsets**:
   * **Target Bone**: Choose the default bone (e.g., `RightHand` for swords, `LeftHand` for shields, `Head` for helmets).
   * **Local Position / Rotation / Scale**: Adjust offsets so the item aligns with the character's hand grip or head.
   * **Auto Mirror for Left Slot**: When enabled, the X position and Y/Z rotation are automatically inverted when equipped in `OffHand` or `LeftRing`.
4. **Tip — Capture Offsets in Editor**:
   * Place the item temporarily under the character's hand bone in the scene view.
   * Position and rotate it visually until it looks perfect.
   * Right-click the `HumanoidAttachmentPoint` component and select **Capture Current Transform as Offsets**.
   * Apply changes back to the prefab!

---

### Step 2.3: Creating `WardrobeItemSO` (ScriptableObject)
Each equippable item has an associated `WardrobeItemSO` asset defining its metadata and equipping rules:

1. Right-click in the Project view > **Create** > **Humanoid Wardrobe** > **Wardrobe Item**.
2. Configure properties:
   * **Item Name**: Display name (e.g. "Slavic Chest Armor").
   * **Restriction**:
     * `SpecificSlotOnly`: Standard single-slot items (Chest, Legs, Boots).
     * `OneHanded`: Fits either `MainHand` or `OffHand`.
     * `TwoHanded`: Equips into `MainHand` and automatically clears `OffHand`.
     * `AnyRing`: Fits either `LeftRing` or `RightRing`.
   * **Target Slot**: The primary `EquipmentSlot` for this item.
   * **Icon**: 2D UI Sprite for inventory grids and paper-doll silhouettes.
   * **Item Prefab**: The prefab created in Step 2.1 or 2.2.

---

## 3. Integrating with Your Game Systems

The wardrobe module is designed to integrate cleanly with any gameplay codebase.

### 3.1. Listening to Equipment Changes
Subscribe to `OnEquipmentChanged` to update player stats, trigger audio, or spawn particles:

```csharp
using UnityEngine;
using Neymanoff.HumanoidWardrobe;

public class PlayerEquipmentBridge : MonoBehaviour
{
    [SerializeField] private WardrobeManager wardrobeManager;

    private void OnEnable()
    {
        wardrobeManager.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void OnDisable()
    {
        wardrobeManager.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    private void HandleEquipmentChanged(EquipmentSlot slot, GameObject spawnedInstance)
    {
        WardrobeItemSO itemData = wardrobeManager.GetEquippedItemData(slot);

        if (spawnedInstance != null && itemData != null)
        {
            Debug.Log($"[Game] Equipped {itemData.ItemName} in slot {slot}. Applying stats!");
            // e.g. playerStats.AddArmor(itemData.ArmorValue);
        }
        else
        {
            Debug.Log($"[Game] Unequipped item in slot {slot}. Removing stats!");
            // e.g. playerStats.RemoveArmor(...);
        }
    }
}
```

---

### 3.2. Equipping from an Inventory
When the player selects an item in your game's inventory UI:

```csharp
public void OnPlayerClickInventoryItem(WardrobeItemSO selectedItem)
{
    // Equips the item into its designated slot with rule checking
    GameObject instance = wardrobeManager.EquipItemSO(selectedItem, selectedItem.TargetSlot);

    if (instance == null)
    {
        Debug.LogWarning("Failed to equip item (slot conflict or invalid item)");
    }
}
```

---

## 4. Cross-Scene Persistence & Save Systems

To keep equipped items throughout your entire game:

### Approach A: Seamless Scene Carryover (`DontDestroyOnLoad`)
If your character moves between scenes (e.g., from character creation to Level 1):
```csharp
void Awake()
{
    DontDestroyOnLoad(gameObject);
}
```
All remapped clothing meshes and attached bone sockets are child GameObjects of the character and **automatically persist** into the new scene.

---

### Approach B: Loadout Rehydration (Save & Load)
For games where the player is spawned from a prefab on each level:

1. **Extract Loadout on Save**:
```csharp
[System.Serializable]
public class SavedEquipmentSlot
{
    public EquipmentSlot slot;
    public string itemResourcePath; // Or database ID
}

public List<SavedEquipmentSlot> SavePlayerEquipment(WardrobeManager manager)
{
    List<SavedEquipmentSlot> savedList = new();
    foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
    {
        WardrobeItemSO itemData = manager.GetEquippedItemData(slot);
        if (itemData != null)
        {
            savedList.Add(new SavedEquipmentSlot {
                slot = slot,
                itemResourcePath = itemData.name
            });
        }
    }
    return savedList;
}
```

2. **Restore Loadout on Spawn**:
```csharp
public void RestorePlayerEquipment(WardrobeManager manager, List<SavedEquipmentSlot> savedData)
{
    manager.UnequipAll();
    foreach (var entry in savedData)
    {
        WardrobeItemSO itemSO = Resources.Load<WardrobeItemSO>($"Items/{entry.itemResourcePath}");
        if (itemSO != null)
        {
            manager.EquipItemSO(itemSO, entry.slot);
        }
    }
}
```

---

## 5. In-Editor Preview Workflow

You don't need to enter Play Mode to inspect character outfits:
1. Select your character in the Scene hierarchy.
2. In the `WardrobeManager` inspector, populate the **Default Loadout** list.
3. Click **Preview Default Loadout**: the items will instantiate and bind to the skeleton directly in the Scene View.
4. Click **Clear Preview** to remove preview instances.

---

## 6. Troubleshooting FAQ

#### Q: The clothing mesh disappears or flickers when moving the camera.
* **Cause**: Frustum culling mismatch because the `rootBone` is evaluated at the floor origin.
* **Fix**: Ensure `clothingRenderer.rootBone` is set to the skeleton's root bone (e.g. `Hips`), or enable `clothingRenderer.updateWhenOffscreen = true` in the Inspector.

#### Q: Models and textures show up pink or missing after cloning.
* **Cause**: Git LFS binary download failure (404 error from remote LFS server).
* **Fix**: Copy the original FBX and PNG files into the respective `Assets/3D_Models/...` directories. Because the `.meta` files are preserved, Unity will instantly reconnect all GUID references.

#### Q: Remapped clothing doesn't follow leg/arm animations.
* **Cause**: Bone name mismatch between clothing FBX and character FBX (e.g. Blender `spine.001` vs Mixamo `mixamorig:Spine`).
* **Fix**: Export the clothing using the same skeleton bone names as your target humanoid character, or verify that both models share the same rig template.
