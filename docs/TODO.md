# Universal Humanoid Wardrobe — Project Roadmap & Task Tracker

This document tracks all active development items, architectural milestones, and verification tasks required to bring the **Universal Humanoid Wardrobe** package to production readiness.

For repository setup and Git LFS recovery history, see [docs/DEVELOPMENT.md](DEVELOPMENT.md).

---

## 🚀 Active Milestones

| Phase | Milestone | Priority | Status |
| :--- | :--- | :---: | :---: |
| **Phase 0** | AI Agent Infrastructure & 3-Tier Anti-Obsolete Defense | 🔴 Critical | ✅ Completed |
| **Phase 1** | Immediate Core Architecture Refactoring | 🔴 Critical | ✅ Completed |
| **Phase 2** | Loadout Persistence & DTO Serialization | 🟡 High | ✅ Completed |
| **Phase 3** | Rig Robustness & Helper/Twist Bone Remapping | 🟡 High | ✅ Completed |
| **Phase 4** | Mesh Clipping & Body Part Masking | 🟡 High | ✅ Completed |
| **Phase 5** | UPM Samples Separation (`Samples~/Demo`) | 🟢 Medium | ✅ Completed |
| **Phase 6** | Comprehensive Automated NUnit Test Suite | 🔴 Critical | ✅ Completed |
| **Phase 7** | Asset Store Submission & Showcase Demo Scene | 🔵 Release | 🔄 Next |

---

## Phase 0: AI Agent Infrastructure & 3-Tier Defense (✅ Completed)

- [x] **0.1. Local Model Swarm Setup (Ollama & RTX 5080)**:
  - [x] Integrate `unity-coder:30b` (pre-conditioned `qwen3-coder:30b` for Unity 6 C# synthesis).
  - [x] Integrate `unity-thinker:32b` (pre-conditioned `deepseek-r1:32b` for architectural auditing).
  - [x] Install `nomic-embed-text` for semantic codebase vector indexing.
  - [x] Configure global MCP bridge in `~/.gemini/config/mcp_config.json` via `ollama-mcp`.
- [x] **0.2. 3-Tier Defense Against Obsolete APIs (CS0618)**:
  - [x] Tier 1: Modelfile system prompts with explicit prohibitions on legacy methods (`FindObjectOfType`, `WWW`, `UI.Text`, `RandomRange`).
  - [x] Tier 2: Create [docs/UNITY6_STANDARDS.md](UNITY6_STANDARDS.md) with replacement tables, zero-GC conventions, and bone remapping rules.
  - [x] Tier 3: Create [Directory.Build.props](../Directory.Build.props) with `<WarningsAsErrors>CS0618</WarningsAsErrors>`.
- [x] **0.3. Universal Onboarding Rules**:
  - [x] Create root [AGENTS.md](../AGENTS.md) for universal discovery by all AI IDEs and agents.
  - [x] Update [docs/TEAM_AGREEMENTS.md](TEAM_AGREEMENTS.md) to define collaboration protocols.

---

## Phase 1: Immediate Core Architecture Refactoring (✅ Completed)

The foundational architectural pillars established to guarantee game-agnostic behavior:

- [x] **1.1. Refactor Slot Model & Multi-Slot Occupancy**:
  - [x] Replace restrictive enum with declarative slot configuration on `WardrobeItemSO`:
    - `List<EquipmentSlot> allowedSlots` (e.g., `[MainHand, OffHand]` or `[LeftRing, RightRing]`).
    - `List<EquipmentSlot> additionalOccupiedSlots` (e.g., `[OffHand]` for 2H weapons).
  - [x] Introduce `EquippedItemInstance` internal record:
    - Holds `WardrobeItemSO`, spawned `GameObject`, `PrimarySlot`, and `IReadOnlyList<EquipmentSlot> OccupiedSlots`.
  - [x] Multi-slot dictionary mapping:
    - Point all occupied slots to the same `EquippedItemInstance`.
  - [x] Atomic multi-slot unequip:
    - Calling `Unequip` on any occupied slot cleanly frees all linked slots and destroys visual instance once.
- [x] **1.2. Stable `ItemId` Implementation**:
  - [x] Add `[SerializeField] private string itemId` to `WardrobeItemSO` with backward-compatible fallback to asset name.
  - [x] Decouple persistence from asset file names.
- [x] **1.3. Explicit `EquipResult` & Richer Events**:
  - [x] Replace `GameObject` null-return with explicit `EquipResult` struct (`Success`, `InvalidSlot`, `SlotOccupied`, `MissingBone`, `MissingPrefab`).
  - [x] Upgrade event signatures:
    - `event Action<EquipmentSlot, WardrobeItemSO, GameObject> OnItemEquipped;`
    - `event Action<EquipmentSlot, WardrobeItemSO> OnItemUnequipped;`
    - `event Action<WardrobeLoadout> OnLoadoutChanged;`
- [x] **1.4. Extract Equipment Rule Resolver**:
  - [x] Separate slot conflict detection and validation logic into pure domain helper `EquipmentRuleResolver`.

---

## Phase 2: Loadout Persistence & DTO Serialization (✅ Completed)

- [x] **2.1. `WardrobeLoadout` Data Transfer Object**:
  - [x] Serializable `EquippedSlotEntry` struct (`slot`, `itemId`).
  - [x] `WardrobeLoadout.ToJson()` and `WardrobeLoadout.FromJson()`.
- [x] **2.2. Manager Integration**:
  - [x] `WardrobeManager.GetCurrentLoadout()`.
  - [x] `WardrobeManager.ApplyLoadout(WardrobeLoadout loadout, Func<string, WardrobeItemSO> resolver)`.
  - [x] Batch loadout application with single `OnLoadoutChanged` event emission.

---

## Phase 3: Rig Robustness & Helper/Twist Bone Remapping (✅ Completed)

- [x] **3.1. `rootBone` Anchoring**:
  - [x] Set `clothingRenderer.rootBone` to skeleton's `Hips` bone (`HumanBodyBones.Hips`) instead of GameObject root.
- [x] **3.2. Twist & Helper Bone Safe Fallbacks**:
  - [x] Hierarchical ancestor fallback walking up parent transform chain when bones are missing from target skeleton.
  - [x] Normalized bone name resolution stripping standard DCC prefixes (`mixamorig:`, `DEF-`, `Bip01_`, `Bone_`).
  - [x] Structured diagnostic warnings logged when bones are safely re-routed.
  - [x] Complete elimination of missing/destroyed bone references in `clothingRenderer.bones`.
- [x] **3.3. Bounding Box & Frustum Culling**:
  - [x] Inherit and expand `localBounds` from base character mesh to eliminate camera frustum culling flicker.

---

## Phase 4: Mesh Clipping & Body Part Masking (✅ Completed)

- [x] **4.1. Granular `BodyPartMask` Enum**:
  - [x] Created `[System.Flags] public enum BodyPartMask` with granular zones: `Head`, `UpperTorso`, `LowerTorso`, `UpperArms`, `LowerArms`, `Hands`, `UpperLegs`, `LowerLegs`, `Feet`.
- [x] **4.2. `WardrobeItemSO` Anti-Clipping Metadata**:
  - [x] Added `hiddenBodyParts` to declare occluded anatomical regions.
  - [x] Added `shrinkBlendShapes` to declare target morph shape keys to deflate.
- [x] **4.3. `WardrobeManager` Body Hiding & Morph Runtime**:
  - [x] Added `ModularBodyPart` struct binding body mask flags to sub-mesh renderers.
  - [x] Added `modularBodyParts` and `morphTargets` serializable lists on `WardrobeManager`.
  - [x] Implemented `UpdateBodyMasksAndBlendshapes()` called automatically on equip, unequip, and loadout restore.
- [x] **4.4. 3D Artist Blender Workflow Guide**:
  - [x] Authored [docs/BLENDER_CHARACTER_SETUP.md](BLENDER_CHARACTER_SETUP.md) detailing both modular sub-mesh splitting and shrink shape key authoring in Blender 4.x.

---

## Phase 5: UPM Samples Separation (✅ Completed)

- [x] **5.1. Decouple Demo UI from Core Package**:
  - [x] Moved `Packages/.../Runtime/UI/` (`DemoInventoryUI`, `EquipmentSlotUI`, `WardrobeDemoAnimationController`, `CharacterRotator`) into `Samples~/Demo/Scripts/`.
  - [x] Created `Neymanoff.HumanoidWardrobe.Demo.asmdef` for isolated demo compilation with TextMeshPro and InputSystem bindings.
  - [x] Maintained local testbed copy under `Assets/Scripts/Demo/` with preserved GUIDs so `WardrobeDemoScene.unity` retains 100% component integrity.
  - [x] Registered official `samples` definition in `package.json` with metadata manifest `.sample.json`.
  - [x] Verified core runtime assembly compiles with zero UI dependencies, 0 warnings, and 0 errors.

---

## Phase 6: Comprehensive Automated NUnit Test Suite (✅ Completed)

EditMode and PlayMode unit tests covering:
- [x] **Rule & Slot Tests (`EquipmentRuleResolverTests.cs`)**:
  - [x] Single-slot equip/unequip validation.
  - [x] Disallowed slot rejection (`EquipResultStatus.InvalidSlot`).
  - [x] Missing prefab handling (`EquipResultStatus.MissingPrefab`).
  - [x] Two-handed weapon occupies `MainHand` and `OffHand`.
  - [x] Secondary slot unequip clears primary slot atomically.
  - [x] Conflicting item replacement behavior.
- [x] **Serialization Tests (`WardrobeLoadoutTests.cs`)**:
  - [x] `WardrobeLoadout` JSON serialization roundtrip fidelity across multiple slots.
  - [x] Empty loadout serialization.
  - [x] Corrupted / null JSON string handling without exceptions.
- [x] **Result Contract Tests (`EquipResultTests.cs`)**:
  - [x] Success and failure factory methods with accurate statuses and error messages.
- [x] **Anti-Clipping Bitmask Tests (`BodyPartMaskTests.cs`)**:
  - [x] Multi-zone bitwise flags combination and evaluation.
  - [x] Multi-item additive mask aggregation.
- [x] **Manager Integration Tests (`WardrobeManagerTests.cs`)**:
  - [x] Full equip, replace, unequip, and unequip-all lifecycle.
  - [x] Event emission verification (`OnItemEquipped`, `OnLoadoutChanged`).
  - [x] Batch rehydration via `ApplyLoadout`.
  - [x] Modular body sub-mesh hiding and restoring on equip/unequip.

---

## Phase 7: Asset Store Submission & Showcase Demo Scene (🔄 Next)

- [ ] Interactive showcase scene with character model, apparel switcher, weapon swapping, and loadout preset saving.
- [ ] XML API documentation across all public classes.
- [ ] Unity Package Validation tests passing with 0 warnings.
- [ ] Asset Store marketing materials (screenshots, banner, documentation links).
