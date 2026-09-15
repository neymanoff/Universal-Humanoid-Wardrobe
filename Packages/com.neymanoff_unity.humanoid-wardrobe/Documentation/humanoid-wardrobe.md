# Universal Humanoid Wardrobe Package Manual

## About Universal Humanoid Wardrobe

The **Universal Humanoid Wardrobe** package provides a lightweight, modular system for dynamic runtime and editor character customization in Unity. It supports equipping, un-equipping, and hot-swapping 3D armor, clothing, weapons, and accessories on any character rigged with Unity's standard **Humanoid** avatar.

The system is split into two core mechanisms:
1. **Skinned Mesh Remapping**: Clothes and armors deform automatically to match the character's active skeleton and animations.
2. **Bone Attachment**: Rigid props, weapons, shields, and accessories attach directly to humanoid bones with customizable transform offsets and automatic mirroring.

---

## Technical Details

### Requirements
* **Unity Version**: 2022.3 LTS, 2023 LTS, or Unity 6 (6000.0+).
* **Render Pipeline**: Compatible with Built-in Render Pipeline, Universal Render Pipeline (URP), and High Definition Render Pipeline (HDRP).
* **Rig Type**: Target characters must have their `Animation Type` set to **Humanoid** in Model Import Settings.

### Core Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                       WardrobeManager                       │
│    (Placed on Character GameObject alongside Animator)      │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
               ▼                               ▼
     [Skinned Mesh Item]             [Rigid Bone Item]
               │                               │
               ▼                               ▼
     SkinnedMeshRemapper            HumanoidAttachmentPoint
(Remaps bones to active skeleton)   (Parents to HumanBodyBones with offsets)
```

---

## Core Components Reference

### 1. `WardrobeManager`
The central component attached to the character root (requires `Animator`).

* **Properties**:
  * `defaultLoadout`: List of equipment slots and prefabs to automatically instantiate and equip on `Start()`.
* **Events**:
  * `event Action<EquipmentSlot, GameObject> OnEquipmentChanged`: Triggered whenever an item is equipped or unequipped in any slot.
* **Public API**:
  * `GameObject Equip(EquipmentSlot slot, GameObject prefab)`: Equips a prefab directly to the specified slot.
  * `GameObject EquipItemSO(WardrobeItemSO itemSO, EquipmentSlot requestedSlot)`: Equips an item ScriptableObject, enforcing two-handed weapon rules and slot compatibility.
  * `void Unequip(EquipmentSlot slot)`: Removes and destroys the equipped item in the given slot.
  * `void UnequipAll()`: Unequips all active items.
  * `GameObject GetEquippedItem(EquipmentSlot slot)`: Returns the currently instantiated GameObject in the slot.
  * `WardrobeItemSO GetEquippedItemData(EquipmentSlot slot)`: Returns the metadata ScriptableObject for the equipped slot.

### 2. `SkinnedMeshRemapper`
Placed on clothing or armor prefabs that contain `SkinnedMeshRenderer` components.

* **Functionality**:
  * `Remap(Transform targetSkeletonRoot)`: Scans the target character's bone hierarchy and rebinds all `clothingRenderer.bones` to the matching target bones.
  * Automatically cleans up unused duplicate bones instantiated with the clothing prefab.

### 3. `HumanoidAttachmentPoint`
Placed on rigid equippable prefabs (weapons, shields, helmets, rings, pendants).

* **Properties**:
  * `useCustomBone`: If enabled, overrides the default slot bone.
  * `targetBone`: The `HumanBodyBones` enum defining which bone to attach to.
  * `localPosition`: Vector3 local offset relative to the bone socket.
  * `localRotation`: Vector3 Euler angle offset relative to the bone socket.
  * `localScale`: Vector3 scale override (typically `Vector3.one`).
  * `autoMirrorForLeftSlot`: When equipped in `OffHand` or `LeftRing`, automatically mirrors `position.x`, `rotation.y`, and `rotation.z` for seamless dual-wielding.

### 4. `WardrobeItemSO`
ScriptableObject asset holding item metadata and restrictions:

* **Item Slot Restrictions**:
  * `SpecificSlotOnly`: Fits only the specified `targetSlot`.
  * `OneHanded`: Fits either `MainHand` or `OffHand`.
  * `TwoHanded`: Equips into `MainHand` and automatically clears the `OffHand` slot.
  * `AnyRing`: Fits either `LeftRing` or `RightRing`.

---

## Workflow: Creating New Items

### Creating a Skinned Clothing Item
1. Export your clothing model from Blender/Maya rigged to the same skeleton proportions as your base character.
2. In Unity, configure the model's Rig as **Humanoid**.
3. Create a Prefab from the imported model.
4. Add the `SkinnedMeshRemapper` component to the root of the prefab.
5. Create a `WardrobeItemSO` (Right-click > *Create > Humanoid Wardrobe > Wardrobe Item*), set the target slot (e.g. `Chest` or `Legs`), and assign the prefab.

### Creating a Weapon / Rigid Prop Item
1. Create a Prefab containing your weapon or prop mesh.
2. Add the `HumanoidAttachmentPoint` component to the root.
3. Configure the local position and rotation offsets so the item aligns properly with the hand grip or socket.
4. Create a `WardrobeItemSO`, set slot restriction (e.g. `OneHanded` or `TwoHanded`), and assign the prefab.

---

## Editor Tools

### Previewing Loadouts in the Inspector
When inspecting a GameObject with `WardrobeManager` in the Unity Editor:
1. Populate the **Default Loadout** list with slots and prefabs.
2. Click **Preview Default Loadout** to instantiate and preview the equipment in the Scene View without entering Play Mode.
3. Click **Clear Preview** to clean up the preview instances.

---

## Persistence Across Gameplay & Scenes

When moving a character between scenes or reloading levels:
1. **DontDestroyOnLoad**: If the character GameObject is preserved across scene transitions, all remapped meshes and attached bone children persist automatically.
2. **Loadout Rehydration**: If characters are spawned dynamically on each level, serialize the equipped item IDs via `WardrobeLoadout` and call `EquipItemSO` during the character's initialization phase.