# Universal Humanoid Wardrobe — Project Roadmap & Task Tracker

This document tracks all completed work, active development items, and upcoming milestones required to bring the **Universal Humanoid Wardrobe** package to release readiness on the Unity Asset Store.

---

## 🚀 Progress Overview

| Phase | Description | Status | Completion |
| :--- | :--- | :---: | :---: |
| **Phase 1** | Repository Recovery, Stabilization & Cleanup | ✅ Done | 100% |
| **Phase 2** | Core Engine Improvements & Rig Robustness | 🔄 In Progress | 40% |
| **Phase 3** | Persistence, Serialization & Game API | ⏳ Next | 0% |
| **Phase 4** | UPM Samples & Demo Decoupling | 📋 Planned | 0% |
| **Phase 5** | Automated Tests (EditMode & PlayMode) | 📋 Planned | 0% |
| **Phase 6** | Asset Store Submission & Packaging | 📋 Planned | 0% |

---

## Phase 1: Repository Recovery & Stabilization (✅ Done)

- [x] **Diagnose Git LFS 404 Issue**: Identified missing remote LFS binaries on GitHub remote.
- [x] **Restore Asset Meta Files**: Restored all deleted `.meta` files across models, textures, prefabs, and ScriptableObjects to preserve GUIDs.
- [x] **Clean Git Index**: Unstaged accidental mass-staged file deletions (`git reset HEAD`).
- [x] **Update `.gitignore`**:
  - [x] Ignore AI agent directories (`.gemini/`, `.antigravity/`).
  - [x] Ignore IDE user caches (`.vscode/`, `.idea/`, `*.DotSettings.user`, `*.user`).
  - [x] Ignore Unity Editor local profiler settings (`ProjectSettings/ProjectAuditorSettings.asset`).
  - [x] Ignore OS temporary junk (`Thumbs.db`, `desktop.ini`, `.DS_Store`).
- [x] **Fix Assembly Definition References**:
  - [x] Fixed `Neymanoff.HumanoidWardrobeardrobe` typo in `Neymanoffunity.HumanoidWardrobe.Tests.asmdef`.
  - [x] Fixed invalid assembly names in `Neymanoffunity.HumanoidWardrobe.Editor.Tests.asmdef`.
- [x] **Fix Editor Preview Double-Instantiation**:
  - [x] Eliminated duplicate prefab leak in `WardrobeManagerEditor.PreviewLoadoutInEditor()`.
  - [x] Added proper Unity Undo registration.
- [x] **Initial Documentation Suite**:
  - [x] Root repository `README.md` in English.
  - [x] Package `README.md` and `CHANGELOG.md` in English.
  - [x] Technical manual `Documentation/humanoid-wardrobe.md`.

---

## Phase 2: Core Improvements & Rig Robustness (🔄 In Progress)

- [ ] **3D Test Assets Preparation**:
  - [ ] Place complete binary FBX files and textures for Slavic Armor, Slavic Helmet, and Dummy into `Assets/3D_Models/`.
  - [ ] Verify materials and textures connect automatically with restored GUIDs.
- [ ] **`SkinnedMeshRemapper` Improvements**:
  - [ ] Fix `rootBone` assignment: bind to target skeleton's `Hips` bone instead of character root GameObject.
  - [ ] Implement Humanoid Avatar fallback bone matching (`Animator.GetBoneTransform`) when bone names differ between clothing and character rigs (e.g. Rigify vs Mixamo).
  - [ ] Add option to copy or expand `localBounds` from character body mesh to prevent premature frustum culling.
  - [ ] Add optional `updateWhenOffscreen` toggle for heavy animation setups.
- [ ] **`HumanoidAttachmentPoint` Enhancements**:
  - [ ] Add visual Gizmo in Scene view showing target bone socket, position offset, and forward orientation.
  - [ ] Add interactive socket selection dropdown in the Inspector.

---

## Phase 3: Persistence, Serialization & Game API (⏳ Next)

- [ ] **Serializable Loadout DTO**:
  - [ ] Create `WardrobeLoadout` class and `EquippedItemEntry` struct.
  - [ ] Implement `WardrobeManager.GetCurrentLoadout()`.
  - [ ] Implement `WardrobeManager.ApplyLoadout(WardrobeLoadout loadout)`.
  - [ ] Implement `ToJson()` and `FromJson()` methods for simple save game integration.
- [ ] **Inventory Decoupling & Events**:
  - [ ] Create `IWardrobeInventoryProvider` interface.
  - [ ] Add rich C# events: `OnItemEquipped(slot, itemSO, instance)`, `OnItemUnequipped(slot, itemSO)`, `OnLoadoutChanged(loadout)`.
  - [ ] Support query helpers: `bool IsSlotOccupied(EquipmentSlot slot)`.
- [ ] **Dynamic Slot Extensibility**:
  - [ ] Evaluate migration or extension for custom slots (e.g., Cloak, Belt, Mask, Quiver, Tail).

---

## Phase 4: UPM Samples & Demo Decoupling (📋 Planned)

- [ ] **Separate Runtime Core from Demo UI**:
  - [ ] Keep `Packages/com.neymanoff_unity.humanoid-wardrobe/Runtime/` strictly focused on core logic.
  - [ ] Move demo UI scripts (`DemoInventoryUI`, `EquipmentSlotUI`, `WardrobeDemoAnimationController`) into a dedicated `Samples~/Demo/` folder according to Unity Package standards.
- [ ] **Sample Import Configuration**:
  - [ ] Update `package.json` to register the `Demo` sample with description and preview scene.
  - [ ] Ensure package can be cleanly imported into an empty project without forcing sample UI assets into user folders.

---

## Phase 5: Automated Testing & Verification (📋 Planned)

- [ ] **EditMode Tests**:
  - [ ] Test `WardrobeItemSO.CanFitInSlot()` logic across all restrictions (`OneHanded`, `TwoHanded`, `AnyRing`).
  - [ ] Test `WardrobeLoadout` JSON serialization and deserialization roundtrip.
  - [ ] Test slot conflict resolution (equipping 2H weapon clears off-hand).
- [ ] **PlayMode Tests**:
  - [ ] Test instantiation and parenting of `HumanoidAttachmentPoint` to valid humanoid bones.
  - [ ] Test runtime bone remapping on mock humanoid rigs.
  - [ ] Test `UnequipAll()` cleans up instances without leaving dangling objects.

---

## Phase 6: Asset Store Release Preparation (📋 Planned)

- [ ] **Asset Store Guidelines Compliance**:
  - [ ] Pass Unity Package Validation tests (no warnings, valid dependencies).
  - [ ] Verify clean namespace conventions (`Neymanoff.HumanoidWardrobe`).
  - [ ] Include standard `Third Party Notices.md` and `LICENSE` (MIT).
- [ ] **Visuals & Presentation**:
  - [ ] Create Asset Store cover banner (1920x1080) and icon (512x512).
  - [ ] Record short GIF/video demonstrating hot-swapping clothing in Play Mode and Editor Preview.
- [ ] **Documentation Polish**:
  - [ ] Review all XML documentation comments across public APIs.
  - [ ] Finalize quick-start guide in `Documentation/humanoid-wardrobe.md`.
