# Universal Humanoid Wardrobe — Architecture Specification

This document details the architectural foundation, design principles, class structures, UML diagrams, and integration patterns of the **Universal Humanoid Wardrobe** package.

---

## 1. System Overview and Design Goals

The primary goal of the **Universal Humanoid Wardrobe** package is to provide an extensible, decoupled, and performant character customization subsystem for Unity.

### Key Architectural Principles
1. **Engine Agnostic Decoupling**: The wardrobe core does not dictate gameplay rules, character movement, stat calculations, or inventory architecture. It acts strictly as an equipment visualization and binding manager.
2. **Standard Humanoid Rig Compliance**: Works seamlessly on any character model configured with Unity's standard `Humanoid` Avatar rig.
3. **Dual Attachment Paradigms**:
   * **Skinned Deforming Items**: Clothing and armor meshes dynamically remap their bone bindings to match the host character skeleton without duplicating skeletons.
   * **Rigid Socketed Items**: Weapons, shields, helmets, and accessories parent to designated `HumanBodyBones` sockets with configurable local offsets and auto-mirroring.
4. **State Persistence**: Supports both scene-transferable GameObject retention (`DontDestroyOnLoad`) and lightweight serialization via data transfer objects (`WardrobeLoadout`).

---

## 2. UML Class Diagram

The following diagram illustrates the core components, data structures, and their relationships:

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

    class ItemSlotRestriction {
        <<enumeration>>
        SpecificSlotOnly
        OneHanded
        TwoHanded
        OffHandOnly
        MainHandOnly
        AnyRing
    }

    class WardrobeItemSO {
        -string itemName
        -ItemSlotRestriction restriction
        -EquipmentSlot targetSlot
        -Sprite icon
        -GameObject itemPrefab
        +string ItemName
        +EquipmentSlot TargetSlot
        +Sprite Icon
        +GameObject ItemPrefab
        +ItemSlotRestriction Restriction
        +CanFitInSlot(EquipmentSlot slot) bool
    }

    class WardrobeManager {
        -Animator _animator
        -Dictionary~EquipmentSlot, GameObject~ _equipmentItems
        -Dictionary~EquipmentSlot, WardrobeItemSO~ _equipmentItemData
        +List~DefaultEquipment~ defaultLoadout
        +event Action~EquipmentSlot, GameObject~ OnEquipmentChanged
        +Equip(EquipmentSlot slot, GameObject prefab) GameObject
        +EquipItemSO(WardrobeItemSO itemSO, EquipmentSlot slot) GameObject
        +Unequip(EquipmentSlot slot) void
        +UnequipAll() void
        +GetEquippedItem(EquipmentSlot slot) GameObject
        +GetEquippedItemData(EquipmentSlot slot) WardrobeItemSO
        +GetDefaultBoneForSlot(EquipmentSlot slot)$ HumanBodyBones
        -EquipInternal(EquipmentSlot slot, GameObject prefab, WardrobeItemSO itemSO) GameObject
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
        +bool UseCustomBone
        +HumanBodyBones TargetBone
        +Vector3 LocalPosition
        +Vector3 LocalRotation
        +Vector3 LocalScale
        +ApplyOffsets(bool isLeftSlot) void
    }

    class WardrobeLoadout {
        +List~EquippedSlotEntry~ items
        +ToJson() string
        +FromJson(string json)$ WardrobeLoadout
    }

    class EquippedSlotEntry {
        +EquipmentSlot slot
        +string itemKey
    }

    class IWardrobeInventoryProvider {
        <<interface>>
        +GetAvailableItems() IReadOnlyList~WardrobeItemSO~
        +CanEquipItem(WardrobeItemSO item, EquipmentSlot slot) bool
        +NotifyItemEquipped(WardrobeItemSO item, EquipmentSlot slot) void
        +NotifyItemUnequipped(WardrobeItemSO item, EquipmentSlot slot) void
    }

    WardrobeManager "1" --> "*" WardrobeItemSO : tracks equipped
    WardrobeManager ..> EquipmentSlot : uses
    WardrobeItemSO ..> ItemSlotRestriction : uses
    WardrobeItemSO ..> EquipmentSlot : target
    WardrobeLoadout "1" *-- "*" EquippedSlotEntry : contains
    EquippedSlotEntry ..> EquipmentSlot : references

    WardrobeManager ..> SkinnedMeshRemapper : invokes on spawn
    WardrobeManager ..> HumanoidAttachmentPoint : invokes on spawn
```

---

## 3. Sequence Diagrams

### 3.1. Equipping a Skinned Mesh Item (Clothing / Armor)

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Game / UI Controller
    participant WM as WardrobeManager
    participant Prefab as Clothing Prefab
    participant SMR as SkinnedMeshRemapper
    participant Host as Character Skeleton

    Caller->>WM: EquipItemSO(itemSO, EquipmentSlot.Chest)
    WM->>WM: Validate Slot & Restriction Rules
    WM->>WM: Unequip(EquipmentSlot.Chest)
    WM->>Prefab: Instantiate(itemSO.ItemPrefab, hostTransform)
    WM->>SMR: TryGetComponent<SkinnedMeshRemapper>()
    activate SMR
    WM->>SMR: Remap(hostAnimator.transform)
    SMR->>Host: BuildBoneMapRecursive(targetSkeletonRoot)
    loop Each SkinnedMeshRenderer
        SMR->>Host: Map clothingRenderer.bones to Host Bones
        SMR->>SMR: Set clothingRenderer.rootBone = Target Hips
    end
    SMR->>SMR: CleanupDuplicateSkeleton()
    deactivate SMR
    WM->>WM: Cache instance & SO data
    WM-->>Caller: Fire OnEquipmentChanged(slot, spawnedInstance)
```

### 3.2. Equipping a Rigid Prop Item (Weapons / Accessories)

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Game / UI Controller
    participant WM as WardrobeManager
    participant Prefab as Weapon Prefab
    participant AP as HumanoidAttachmentPoint
    participant Anim as Host Animator

    Caller->>WM: EquipItemSO(itemSO, EquipmentSlot.MainHand)
    WM->>WM: Check 2H Conflicts (Unequip OffHand if 2H)
    WM->>Prefab: Instantiate(itemSO.ItemPrefab, hostTransform)
    WM->>AP: TryGetComponent<HumanoidAttachmentPoint>()
    activate AP
    WM->>Anim: GetBoneTransform(targetBone)
    Anim-->>WM: Transform (RightHand Bone)
    WM->>Prefab: SetParent(boneTransform, false)
    WM->>AP: ApplyOffsets(isLeftSlot: false)
    AP->>Prefab: Set localPosition, localRotation, localScale
    deactivate AP
    WM->>WM: Cache instance & SO data
    WM-->>Caller: Fire OnEquipmentChanged(slot, spawnedInstance)
```

### 3.3. Persistence Flow (Save & Restore Across Scenes)

```mermaid
sequenceDiagram
    autonumber
    participant SceneA as Wardrobe Scene
    participant WM as WardrobeManager
    participant SaveSystem as Game Save / PlayerPrefs
    participant SceneB as Gameplay Level (Spawned Player)

    SceneA->>WM: Player equips items in Wardrobe
    SceneA->>WM: GetCurrentLoadout()
    WM-->>SceneA: WardrobeLoadout (Serializable DTO)
    SceneA->>SaveSystem: SaveLoadout(loadout.ToJson())

    Note over SaveSystem, SceneB: Level Transition / Scene Load

    SceneB->>SaveSystem: LoadLoadout()
    SaveSystem-->>SceneB: JSON string
    SceneB->>SceneB: WardrobeLoadout.FromJson(json)
    SceneB->>WM: ApplyLoadout(loadedLoadout)
    loop Each slot entry in loadout
        SceneB->>WM: EquipItemSO(resolvedItemSO, entry.slot)
    end
```

---

## 4. Architectural Layers & Separation of Concerns

The module divides responsibilities into three distinct layers:

```
┌──────────────────────────────────────────────────────────────┐
│                        GAMEPLAY LAYER                        │
│   (Player Stats, Save System, Inventory Model, Game Rules)   │
└──────────────────────────────┬───────────────────────────────┘
                               │ Event Subscription & Loadout DTO
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                    WARDROBE CORE PACKAGE                     │
│               (Neymanoff.HumanoidWardrobe)                  │
│                                                              │
│  ┌───────────────────────┐        ┌───────────────────────┐  │
│  │    WardrobeManager    │        │    WardrobeItemSO     │  │
│  │ (Runtime Controller)  │        │   (Item Data Model)   │  │
│  └───────────┬───────────┘        └───────────────────────┘  │
│              │                                               │
│       ┌──────┴──────────────┐                                │
│       ▼                     ▼                                │
│ ┌──────────────────┐  ┌───────────────────────────┐          │
│ │SkinnedMeshRemapper│  │  HumanoidAttachmentPoint  │          │
│ │(Deforming Meshes)│  │ (Socket Bone Attachments) │          │
│ └──────────────────┘  └───────────────────────────┘          │
└──────────────────────────────────────────────────────────────┘
                               ▲
                               │ UI Binding
┌──────────────────────────────┴───────────────────────────────┐
│                    OPTIONAL SAMPLES / UI                     │
│             (Neymanoff.HumanoidWardrobe.UI)                 │
│      (DemoInventoryUI, EquipmentSlotUI, Turntable)           │
└──────────────────────────────────────────────────────────────┘
```

### 4.1. Core Layer (`Neymanoff.HumanoidWardrobe`)
* Completely isolated from UI and external game systems.
* Relies only on standard Unity engine modules (`UnityEngine.CoreModule`, `UnityEngine.AnimationModule`).
* Exposes clean events (`OnEquipmentChanged`) and public methods (`EquipItemSO`, `Unequip`, `GetCurrentLoadout`).

### 4.2. UI / Sample Layer (`Neymanoff.HumanoidWardrobe.UI`)
* Provides an out-of-the-box paper-doll equipment UI and grid selector.
* Listens to `WardrobeManager` events to refresh slots dynamically.
* Can be deleted or replaced entirely without affecting core wardrobe logic.

---

## 5. Technical Considerations & Performance

### 5.1. Skinned Mesh Culling & Bounds
* Standard Unity skinned meshes calculate frustum culling based on their `rootBone` and `localBounds`.
* When clothing is remapped, `rootBone` must be bound to the skeleton's root bone (e.g. `Hips` or `spine`), **not** the character's GameObject origin.
* If a mesh causes clipping or premature culling, `SkinnedMeshRenderer.updateWhenOffscreen` or copying `localBounds` from the host body mesh guarantees stable rendering.

### 5.2. Garbage Collection & Memory Management
* `SkinnedMeshRemapper` only runs once per item instantiation.
* Transform dictionaries are allocated locally during the remap routine and discarded immediately after setup.
* Unused skeleton nodes from clothing prefabs are systematically removed (`Destroy` in Play Mode, `DestroyImmediate` in Editor Mode) to avoid hierarchy bloat and overhead during animation updates.
