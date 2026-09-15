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
| **Phase 4** | Mesh Clipping & Body Part Masking | 🟡 High | 🔄 Next |
| **Phase 5** | UPM Samples Separation (`Samples~/Demo`) | 🟢 Medium | 📋 Planned |
| **Phase 6** | Comprehensive Automated NUnit Test Suite | 🔴 Critical | 📋 Planned |
| **Phase 7** | Asset Store Submission & Showcase Demo Scene | 🔵 Release | 📋 Planned |

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

## Phase 4: Mesh Clipping & Body Part Masking (🔄 Next)

- [ ] **4.1. Body Mask Flags on `WardrobeItemSO`**:
  - [ ] Add `[System.Flags] public enum BodyPartMask { None, Head, Torso, Arms, Hands, Legs, Feet }`.
  - [ ] Allow items (e.g. plate cuirass, closed boots) to declare which body regions they conceal.
- [ ] **4.2. Target Character Body Mesh Hiding**:
  - [ ] Support modular character setups: deactivate sub-meshes or set blendshapes to shrink occluded geometry, completely eliminating poke-through clipping.

---

## Phase 5: UPM Samples Separation (📋 Planned)

- [ ] **5.1. Decouple Demo UI from Core Package**:
  - [ ] Move `Packages/.../Runtime/UI/` (`DemoInventoryUI`, `EquipmentSlotUI`, `WardrobeDemoAnimationController`) into `Samples~/Demo/`.
  - [ ] Register sample in `package.json` with sample metadata and preview scene.
  - [ ] Ensure core package compiles with zero dependencies on the demo UI.

---

## Phase 6: Comprehensive Automated NUnit Test Suite (🔴 Critical)

EditMode and PlayMode unit tests covering:
- [ ] **Rule & Slot Tests**:
  - [ ] Single-slot equip/unequip.
  - [ ] Two-handed weapon occupies `MainHand` and `OffHand`.
  - [ ] Secondary slot unequip clears primary slot atomically.
  - [ ] Conflicting item replacement behavior.
- [ ] **Serialization Tests**:
  - [ ] `WardrobeLoadout` JSON serialization roundtrip fidelity.
  - [ ] Missing item resolution handling.
- [ ] **Edge Cases**:
  - [ ] Missing bone in avatar returns `EquipResultStatus.MissingBone`.
  - [ ] Null/destroyed prefab handling.

---

## Phase 7: Asset Store Submission & Showcase Demo Scene (🔵 Release)

- [ ] Interactive showcase scene with character model, apparel switcher, weapon swapping, and loadout preset saving.
- [ ] XML API documentation across all public classes.
- [ ] Unity Package Validation tests passing with 0 warnings.
- [ ] Asset Store marketing materials (screenshots, banner, documentation links).
