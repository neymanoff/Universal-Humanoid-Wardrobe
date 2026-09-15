# Universal Humanoid Wardrobe — Project Roadmap & Task Tracker

This document tracks all active development items, architectural milestones, and verification tasks required to bring the **Universal Humanoid Wardrobe** package to production readiness.

For repository setup and Git LFS recovery history, see [docs/DEVELOPMENT.md](DEVELOPMENT.md).

---

## 🚀 Active Milestones

| Phase | Milestone | Priority | Status |
| :--- | :--- | :---: | :---: |
| **Phase 1** | Immediate Core Architecture Refactoring | 🔴 Critical | 🔄 In Progress |
| **Phase 2** | Rig Robustness & Bone Remapping | 🟡 High | ⏳ Next |
| **Phase 3** | Loadout Persistence & DTO Serialization | 🟡 High | 📋 Planned |
| **Phase 4** | UPM Samples Separation | 🟢 Medium | 📋 Planned |
| **Phase 5** | Comprehensive Automated Test Suite | 🔴 Critical | 📋 Planned |
| **Phase 6** | Advanced Features (Rig Profiles & Body Masking) | 🟢 Medium | 📋 Planned |
| **Phase 7** | Asset Store Submission & Polish | 🔵 Release | 📋 Planned |

---

## Phase 1: Immediate Core Architecture Refactoring (🔄 In Progress)

The foundational architectural pillars to establish before expanding features:

- [ ] **1.1. Refactor Slot Model & Multi-Slot Occupancy**:
  - [ ] Replace restrictive `ItemSlotRestriction` enum with declarative slot configuration on `WardrobeItemSO`:
    - `List<EquipmentSlot> allowedSlots` (e.g., `[MainHand, OffHand]` or `[LeftRing, RightRing]`).
    - `List<EquipmentSlot> additionalOccupiedSlots` (e.g., `[OffHand]` for 2H weapons).
  - [ ] Introduce `EquippedItemInstance` internal record:
    - Holds `WardrobeItemSO`, spawned `GameObject`, `PrimarySlot`, and `IReadOnlyList<EquipmentSlot> OccupiedSlots`.
  - [ ] Multi-slot dictionary mapping:
    - Point all occupied slots to the same `EquippedItemInstance` (e.g. `_slotToInstance[MainHand]` and `_slotToInstance[OffHand]`).
  - [ ] Atomic multi-slot unequip:
    - Calling `Unequip` on any occupied slot unlinks all associated slots and destroys the visual GameObject once.
- [ ] **1.2. Stable `ItemId` Implementation**:
  - [ ] Add `[SerializeField] private string itemId` to `WardrobeItemSO`.
  - [ ] Ensure persistence and serialization rely strictly on `ItemId`, never asset filenames or `Resources.Load`.
- [ ] **1.3. Explicit `EquipResult` & Richer Events**:
  - [ ] Replace `GameObject` null-return with `EquipResult` struct:
    - `EquipResultStatus`: `Success`, `InvalidSlot`, `SlotOccupied`, `MissingPrefab`, `MissingBone`, `IncompatibleRig`.
  - [ ] Upgrade event signatures:
    - `event Action<EquipmentSlot, WardrobeItemSO, GameObject> OnItemEquipped;`
    - `event Action<EquipmentSlot, WardrobeItemSO> OnItemUnequipped;`
    - `event Action<WardrobeLoadout> OnLoadoutChanged;`
- [ ] **1.4. Extract Equipment Rule Resolver**:
  - [ ] Separate slot conflict detection and validation logic from `WardrobeManager` into an isolated `EquipmentRuleResolver` domain helper.

---

## Phase 2: Rig Robustness & Bone Remapping (⏳ Next)

- [ ] **2.1. `SkinnedMeshRemapper` Fixes**:
  - [ ] Set `clothingRenderer.rootBone` to the skeleton's root bone (`Hips` / `spine`) instead of the character GameObject root.
  - [ ] Add option to inherit or expand `localBounds` from the host character mesh to eliminate frustum culling flicker.
  - [ ] Implement Humanoid Avatar fallback bone resolution (`Animator.GetBoneTransform`) for standard body joints when bone names differ.
- [ ] **2.2. Twist & Helper Bone Handling**:
  - [ ] Document rig matching requirements in code and warnings.
  - [ ] Prepare architecture for bone alias mapping for non-standard DCC twist bones.
- [ ] **2.3. Test Models Integration**:
  - [ ] Verify imported binary FBX and texture assets against restored `.meta` GUIDs.

---

## Phase 3: Loadout Persistence & DTO Serialization (📋 Planned)

- [ ] **3.1. `WardrobeLoadout` Data Transfer Object**:
  - [ ] Serializable `EquippedSlotEntry` struct (`slot`, `itemId`).
  - [ ] `WardrobeLoadout.ToJson()` and `WardrobeLoadout.FromJson()`.
- [ ] **3.2. Manager Integration**:
  - [ ] `WardrobeManager.GetCurrentLoadout()`.
  - [ ] `WardrobeManager.ApplyLoadout(WardrobeLoadout loadout, Func<string, WardrobeItemSO> resolver)`.
  - [ ] Guarantee exactly one `OnLoadoutChanged` event per batch loadout application.

---

## Phase 4: UPM Samples Separation (📋 Planned)

- [ ] **4.1. Decouple Demo UI from Core Package**:
  - [ ] Move `Runtime/UI/` (`DemoInventoryUI`, `EquipmentSlotUI`, `WardrobeDemoAnimationController`) into `Samples~/Demo/`.
  - [ ] Register sample in `package.json` with sample metadata and preview scene.
  - [ ] Ensure core package compiles and operates cleanly without the demo UI assembly.

---

## Phase 5: Comprehensive Automated Test Suite (🔴 Critical)

Both EditMode and PlayMode tests covering core stability and edge cases:

- [ ] **Slot & Rule Tests**:
  - [ ] Can equip into any allowed slot (e.g. one-handed weapon in either hand).
  - [ ] Two-handed weapon occupies both `MainHand` and `OffHand`.
  - [ ] Unequipping multi-slot item from secondary slot (e.g. `OffHand`) cleanly clears both slots and destroys visual instance once.
  - [ ] Equip same item twice handling.
  - [ ] Slot conflict resolution with existing equipped items.
- [ ] **Serialization Tests**:
  - [ ] `WardrobeLoadout` JSON serialization roundtrip fidelity.
  - [ ] Rehydrating loadout with unknown `ItemId` gracefully reports failure.
  - [ ] Duplicate `ItemId` detection and validation.
- [ ] **Robustness & Edge Cases**:
  - [ ] Missing bone in character skeleton returns `EquipResultStatus.MissingBone`.
  - [ ] Invalid or null prefab returns `EquipResultStatus.MissingPrefab`.
  - [ ] Incompatible rig detection.
  - [ ] Rigs with differing bone naming conventions (Rigify vs Mixamo vs Synty).
- [ ] **Event & Lifecycle Tests**:
  - [ ] Event firing order verification (`OnItemUnequipped` -> `OnItemEquipped` -> `OnLoadoutChanged`).
  - [ ] Exactly one `OnLoadoutChanged` fired per multi-slot transaction.
  - [ ] Invalid `defaultLoadout` entries handled gracefully without exceptions.
  - [ ] In-Editor Preview -> Clear Preview -> Assert zero leaked or dangling GameObjects.

---

## Phase 6: Advanced Features (📋 Planned)

- [ ] **6.1. `WardrobeRigProfile`**:
  - [ ] ScriptableObject defining per-rig socket offsets (Orc, Elf, Dwarf, Human), bone aliases, and custom bounds.
- [ ] **6.2. Body Coverage & Mesh Clipping (`HideBodyParts`)**:
  - [ ] Add flags to `WardrobeItemSO` (e.g. `HideTorso`, `HideArms`, `HideLegs`) to hide host body sub-meshes and eliminate armor poke-through.

---

## Phase 7: Asset Store Submission & Polish (📋 Planned)

- [ ] Unity Package Validation tests (zero warnings, valid dependency manifests).
- [ ] XML API documentation on all public methods and types.
- [ ] Marketing assets: 1920x1080 banner, 512x512 icon, preview GIFs.
