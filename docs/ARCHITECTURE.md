# Universal Humanoid Wardrobe — Architecture Specification

This document details the architectural foundation, design principles, class structures, UML diagrams, and integration patterns of the **Universal Humanoid Wardrobe** package.

---

## 1. Architectural Principles

### 1.1. Gameplay-Agnostic / Host-Game-Agnostic
The wardrobe module is strictly **Unity-specific** (relying on `UnityEngine`, `Animator`, `HumanBodyBones`, `SkinnedMeshRenderer`), but it is completely **Gameplay-Agnostic**:
* It does not know or care about combat calculations, weapon stats, durability, player levels, or inventory currencies.
* It does not impose an inventory architecture (grid, weight, slots, or tree-based).
* Its single responsibility is **managing visual equipment representation, bone bindings, socket transforms, and equipment occupancy state**.

### 1.2. Split Compatibility Guarantees
* **Rigid Items (Socket Attachment)**: **High Universality**. Attached directly to standard `HumanBodyBones` transforms (e.g. `RightHand`, `LeftHand`, `Head`). Functions out-of-the-box on virtually any valid Unity Humanoid avatar.
* **Skinned Items (Mesh Remapping)**: **Rig-Dependent**. Humanoid animation retargeting retargets motion, but it does *not* automatically deform meshes across wildly different bind poses, body proportions, or rest poses. Skinned clothing meshes require matching rest poses and compatible bone hierarchies (including helper/twist bones).

---

## 2. UML Class Diagram

```mermaid
classDiagram
    class EquipmentSlot {
        <<enumeration>>
        Head
        Shoulders
        Chest
        Hands
        Legs
        Feet
        Neck
        Back
        MainHand
        OffHand
        LeftRing
        RightRing
    }

    class EquipResultStatus {
        <<enumeration>>
        Success
        InvalidSlot
        SlotOccupied
        MissingPrefab
        MissingBone
        IncompatibleRig
    }

    class EquipResult {
        +EquipResultStatus Status
        +EquippedItemInstance Instance
        +string ErrorMessage
        +bool IsSuccess
    }

    class WardrobeItemSO {
        -string itemId
        -string itemName
        -List~EquipmentSlot~ allowedSlots
        -List~EquipmentSlot~ additionalOccupiedSlots
        -Sprite icon
        -GameObject itemPrefab
        +string ItemId
        +string ItemName
        +IReadOnlyList~EquipmentSlot~ AllowedSlots
        +IReadOnlyList~EquipmentSlot~ AdditionalOccupiedSlots
        +Sprite Icon
        +GameObject ItemPrefab
        +CanEquipIntoSlot(EquipmentSlot slot) bool
        +GetOccupiedSlots(EquipmentSlot requestedSlot) List~EquipmentSlot~
    }

    class EquippedItemInstance {
        +WardrobeItemSO ItemData
        +GameObject InstanceObject
        +EquipmentSlot PrimarySlot
        +IReadOnlyList~EquipmentSlot~ OccupiedSlots
        +bool OccupiesSlot(EquipmentSlot slot) bool
    }

    class EquipmentRuleResolver {
        +CanEquip(WardrobeItemSO item, EquipmentSlot requestedSlot, IReadOnlyDictionary~EquipmentSlot, EquippedItemInstance~ currentSlots) EquipResultStatus
        +ResolveConflictingSlots(WardrobeItemSO item, EquipmentSlot requestedSlot, IReadOnlyDictionary~EquipmentSlot, EquippedItemInstance~ currentSlots) List~EquipmentSlot~
    }

    class WardrobeManager {
        -Animator _animator
        -Dictionary~EquipmentSlot, EquippedItemInstance~ _slotToInstance
        -List~EquippedItemInstance~ _equippedInstances
        +event Action~EquipmentSlot, WardrobeItemSO, GameObject~ OnItemEquipped
        +event Action~EquipmentSlot, WardrobeItemSO~ OnItemUnequipped
        +event Action~WardrobeLoadout~ OnLoadoutChanged
        +Equip(WardrobeItemSO item, EquipmentSlot slot) EquipResult
        +Unequip(EquipmentSlot slot) bool
        +UnequipAll() void
        +IsSlotOccupied(EquipmentSlot slot) bool
        +GetEquippedInstance(EquipmentSlot slot) EquippedItemInstance
        +GetEquippedItemData(EquipmentSlot slot) WardrobeItemSO
        +GetCurrentLoadout() WardrobeLoadout
        +ApplyLoadout(WardrobeLoadout loadout, Func~string, WardrobeItemSO~ itemResolver) void
    }

    class SkinnedMeshRemapper {
        +Remap(Transform targetSkeletonRoot) void
        -BuildBoneMapRecursive(Transform current, Dictionary~string, Transform~ map) void
        -CleanupDuplicateSkeleton() void
    }

    class HumanoidAttachmentPoint {
        -bool useCustomBone
        -HumanBodyBones targetBone
        -Vector3 localPosition
        -Vector3 localRotation
        -Vector3 localScale
        -bool autoMirrorForLeftSlot
        +ApplyOffsets(bool isLeftSlot) void
    }

    class WardrobeLoadout {
        +List~EquippedSlotEntry~ entries
        +ToJson() string
        +FromJson(string json)$ WardrobeLoadout
    }

    class EquippedSlotEntry {
        +EquipmentSlot slot
        +string itemId
    }

    WardrobeManager "1" *-- "*" EquippedItemInstance : manages
    EquippedItemInstance "1" o-- "1" WardrobeItemSO : references
    WardrobeManager ..> EquipmentRuleResolver : validates via
    WardrobeManager ..> EquipResult : returns
    WardrobeManager ..> SkinnedMeshRemapper : executes
    WardrobeManager ..> HumanoidAttachmentPoint : executes
    WardrobeLoadout "1" *-- "*" EquippedSlotEntry : contains
    EquippedSlotEntry ..> EquipmentSlot : uses
    WardrobeItemSO ..> EquipmentSlot : uses
```

---

## 3. Multi-Slot Occupancy Model

In standard RPG equipment models, weapons such as Greatswords or Staves are "Two-Handed": they are initiated in `MainHand`, but logically and visually occupy both `MainHand` and `OffHand`.

### How Multi-Slot Occupancy Works
1. When `GreatSword` is equipped into `MainHand`:
   * `WardrobeItemSO.AllowedSlots` = `[MainHand]`.
   * `WardrobeItemSO.AdditionalOccupiedSlots` = `[OffHand]`.
   * The resolver identifies that both `MainHand` and `OffHand` will be occupied.
   * Any item already in `OffHand` (or `MainHand`) is unequipped first.
   * A single `EquippedItemInstance` is created holding the spawned 3D weapon GameObject.
   * Both slots point to this single instance:
     ```text
     _slotToInstance[MainHand] ──► [EquippedItemInstance (GreatSword)]
     _slotToInstance[OffHand]  ──► [EquippedItemInstance (GreatSword)]
     ```
2. **Atomic Unequip**: Calling `Unequip(EquipmentSlot.OffHand)` or `Unequip(EquipmentSlot.MainHand)` removes both slot references, destroys the visual GameObject once, and fires `OnItemUnequipped` cleanly.
3. **Predictable Querying**: `IsSlotOccupied(EquipmentSlot.OffHand)` returns `true` and returns the `GreatSword` instance, preventing conflicting equipment or empty off-hand anomalies.

---

## 4. Sequence Diagrams

### 4.1. Equipping a Two-Handed Weapon (Multi-Slot Flow)

```mermaid
sequenceDiagram
    autonumber
    participant Game as Host Game / Inventory
    participant WM as WardrobeManager
    participant Rule as EquipmentRuleResolver
    participant Inst as EquippedItemInstance
    participant Prefab as Weapon GameObject

    Game->>WM: Equip(greatSwordSO, EquipmentSlot.MainHand)
    WM->>Rule: CanEquip(greatSwordSO, MainHand, _slotToInstance)
    Rule-->>WM: EquipResultStatus.Success
    WM->>Rule: ResolveConflictingSlots(greatSwordSO, MainHand, _slotToInstance)
    Rule-->>WM: [MainHand, OffHand]
    loop Each Conflicting Slot (e.g. OffHand Shield)
        WM->>WM: Unequip(slot)
    end
    WM->>Prefab: Instantiate(greatSwordSO.ItemPrefab, hostTransform)
    WM->>Prefab: Configure HumanoidAttachmentPoint (RightHand)
    WM->>Inst: new EquippedItemInstance(greatSwordSO, Prefab, [MainHand, OffHand])
    WM->>WM: _slotToInstance[MainHand] = Inst
    WM->>WM: _slotToInstance[OffHand] = Inst
    WM-->>Game: Fire OnItemEquipped(MainHand, greatSwordSO, Prefab)
    WM-->>Game: Fire OnLoadoutChanged(newLoadout)
    WM-->>Game: Return EquipResult(Success, Inst)
```

### 4.2. Runtime Persistence & Loadout Rehydration

```mermaid
sequenceDiagram
    autonumber
    participant Session as Game Session / Save Data
    participant PlayerPrefab as Spawned Player Character
    participant WM as WardrobeManager
    participant Resolver as Item Database / Addressables

    Session->>PlayerPrefab: Instantiate on Level Load
    Session->>WM: ApplyLoadout(savedLoadout, itemResolver)
    WM->>WM: UnequipAll()
    loop Each entry in savedLoadout.entries
        WM->>Resolver: ResolveItem(entry.itemId)
        Resolver-->>WM: WardrobeItemSO
        WM->>WM: Equip(resolvedItemSO, entry.slot)
    end
    WM-->>Session: Fire OnLoadoutChanged
```

---

## 5. Architectural Boundaries & Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                       HOST GAME LAYER                       │
│     (Game Inventory, Character Stats, Save/Load System)     │
└──────────────────────────────┬──────────────────────────────┘
                               │ Calls Equip / ApplyLoadout
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    WARDROBE CORE PACKAGE                    │
│                                                             │
│   ┌────────────────────────┐     ┌──────────────────────┐   │
│   │ EquipmentRuleResolver  │◄────┤   WardrobeItemSO     │   │
│   │  (Conflict & Slots)    │     │   (Stable ItemId)    │   │
│   └───────────┬────────────┘     └──────────────────────┘   │
│               │                                             │
│               ▼                                             │
│   ┌────────────────────────┐     ┌──────────────────────┐   │
│   │    WardrobeManager     │────►│ EquippedItemInstance │   │
│   │ (Visual & State Mgmt)  │     │(Multi-slot reference)│   │
│   └───────────┬────────────┘     └──────────────────────┘   │
│               │                                             │
│       ┌───────┴──────────────┐                              │
│       ▼                      ▼                              │
│ ┌──────────────────┐   ┌───────────────────────────┐        │
│ │SkinnedMeshRemapper│  │  HumanoidAttachmentPoint  │        │
│ │ (Remap to Hips)  │   │   (Bone socket offsets)   │        │
│ └──────────────────┘   └───────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
                               │ Outward Events Only
                               ▼
               OnItemEquipped / OnItemUnequipped
                       OnLoadoutChanged
```

### Outward-Only Notification
* The Wardrobe Core **never calls into the host game's inventory**.
* The host game controls the wardrobe by calling `Equip` / `Unequip`.
* The wardrobe communicates changes outward exclusively via events:
  * `OnItemEquipped(EquipmentSlot primarySlot, WardrobeItemSO item, GameObject instance)`
  * `OnItemUnequipped(EquipmentSlot primarySlot, WardrobeItemSO item)`
  * `OnLoadoutChanged(WardrobeLoadout currentLoadout)`

---

## 6. Future Expansion Points (Roadmap Alignment)

1. **`WardrobeRigProfile`**: ScriptableObject mapping per-rig socket offsets (e.g. Orc vs Dwarf vs Elf), custom bone aliases, and bounds presets.
2. **Body Coverage & Mesh Clipping (`HideBodyParts`)**: Declarative flags on `WardrobeItemSO` (e.g. `HideTorso`, `HideLegs`) to toggle sub-mesh visibility or swap body geometry to prevent skin poke-through.
3. **Rig Remap Profile**: Explicit bone-to-bone alias mapping to handle non-standard twist/helper bones across different modeling DCCs.
