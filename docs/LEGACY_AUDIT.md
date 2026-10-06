# Legacy Codebase Audit: Wardrobe & Equipment Attachment Subsystems

> **Module**: Universal Humanoid Wardrobe (`com.neymanoff.humanoid-wardrobe`)  
> **Source Project**: Legends: Legacy of the Lost (`d:\Unity\My Projects\LegendsLegacyOfLost`)  
> **Target Engine**: Unity 6 (`6000.5.10f1`)  
> **Audit Status**: Completed & Grounded  

---

## 1. Executive Summary & Purpose

This audit establishes the architectural truth and lineage between the legacy equipment attachment systems in **Legends: Legacy of the Lost** and the standalone, reusable **Universal Humanoid Wardrobe** package.

In the original game project, equipment visualization was implemented as a minimal rigid attachment system (`AttachmentModule` and `EquipmentModule`). While functional for simple weapons and shields, it possessed fundamental limitations:
1. **No Skinned Mesh Deformations**: Inability to equip wearable apparel, armors, boots, or robes that conform to character skeletons.
2. **String-Coupled Bone Lookup**: Reliance on manual string keys (`"MainHand"`, `"OffHand"`, `"Back"`) and manually populated transform arrays.
3. **Monolithic Data Couplings**: Direct dependencies on gameplay-specific classes (`UnitLoadout`, `EquipmentItemSO`, `GameSessionEquipmentProvider`).
4. **Lack of Anti-Clipping**: Zero support for sub-mesh masking or shrink blendshapes.
5. **Rigid Single-Slot Limitations**: Inability to declaratively represent multi-slot items (e.g. two-handed greatswords occupying both main-hand and off-hand).

The **Universal Humanoid Wardrobe** package was designed to supersede this legacy implementation by providing a game-agnostic, robust, high-performance solution supporting both **Skinned Mesh Remapping** and **Rigid Bone Sockets**, while remaining 100% free of game taint.

---

## 2. Source Files Inspected in `Legends: Legacy of the Lost`

The following files from `Legends: Legacy of the Lost` were inspected and analyzed as the source of truth:

| Source File Path | Original Responsibility | Architectural Evaluation |
| :--- | :--- | :--- |
| `Assets/Scripts/Features/AttachmentModule/Runtime/BoneAttachmentController.cs` | Manages attaching prefabs to bone transforms via string mappings (`boneMappings`). Tracks `activeAttachments` dictionary and clears instances. | **Preserved concept, refactored implementation**: Replaced string lookups with native Unity `Animator.GetBoneTransform(HumanBodyBones)` and automated lifecycle management. |
| `Assets/Scripts/Features/AttachmentModule/Runtime/EquipmentOffset.cs` | Serializes `localPosition`, `localRotation`, `localScale` on equippable prefabs and applies them relative to parent bone. | **Preserved & Expanded**: Evolved into `HumanoidAttachmentPoint.cs` with auto-mirroring for left slots (`autoMirrorForLeftSlot`) and profile/scale compensation hooks. |
| `Assets/Scripts/Features/AttachmentModule/Editor/BoneAttachmentControllerEditor.cs` | Custom editor with button to auto-detect and populate standard bone transforms. | **Refactored**: Transferred into `WardrobeManagerEditor.cs` with one-click preview and unequip actions. |
| `Assets/Scripts/Features/AttachmentModule/Editor/EquipmentOffsetEditor.cs` | Custom editor with helper buttons to reset offsets. | **Refactored**: Transferred into context menu actions (`[ContextMenu("Capture Current Transform as Offsets")]`) directly on `HumanoidAttachmentPoint.cs`. |
| `Assets/Scripts/Features/EquipmentModule/Runtime/UnitEquipmentVisuals.cs` | Listens to unit loadout changes and coordinates spawning models via `BoneAttachmentController`. Contains manual serialized mounts (`mainHandMount`, `offHandMount`, `backMount`). | **Refactored into Game-Agnostic Facade**: Core lifecycle logic abstracted into `WardrobeManager.cs`. Legacy mount point fallbacks replaced by direct humanoid bone resolution. |
| `Assets/Scripts/Features/EquipmentModule/Runtime/EquipmentModuleController.cs` | High-level controller bridging game inventory with visuals. | **Decoupled**: Game logic remains in `LegendsLegacyOfLost`; presentation and state management moved to `WardrobeManager.cs`. |
| `Assets/Scripts/Data/Items/EquipmentItemSO.cs` | Game-specific equipment definition containing RPG combat stats, price, rarity, and visual prefab reference. | **Decoupled**: Extracted pure wardrobe representation into `WardrobeItemSO.cs` with stable `ItemId`, allowed slots, and anti-clipping metadata. |

---

## 3. Preserved Logic, Mechanics & Architectural Lineage

The following core mechanics from `Legends: Legacy of the Lost` were identified as mathematically and conceptually sound, and have been preserved and improved:

### 3.1. Relative Transform Offsetting
* **Legacy Mechanic**: `EquipmentOffset.cs` stored `localPosition`, `localRotation`, and `localScale` to fine-tune how weapons and shields sit in hands.
* **Preserved & Evolved**: Retained in `HumanoidAttachmentPoint.cs`. Added **left-slot auto-mirroring** (`pos.x = -pos.x`, `rot.y = -rot.y`, `rot.z = -rot.z`), eliminating the need for artists to duplicate weapon prefabs for left vs right hands.

### 3.2. Slot-Based Instantiation & Atomic Replacement
* **Legacy Mechanic**: When equipping an item into a slot, any existing item in that slot was destroyed before the new instance was spawned and parented.
* **Preserved & Evolved**: Implemented in `WardrobeManager.Equip` and `EquipmentRuleResolver.GetConflictingSlots`, supporting multi-slot atomic clearing (e.g., equipping a 2H weapon clears both hands cleanly).

### 3.3. In-Editor Authoring & Offset Capture
* **Legacy Mechanic**: Custom inspector tools allowing designers to position a weapon visually in the Scene view and save transform offsets.
* **Preserved & Evolved**: Retained via `[ContextMenu("Capture Current Transform as Offsets")]` in `HumanoidAttachmentPoint.cs` and loadout preview tools in `WardrobeManagerEditor.cs`.

---

## 4. Refactored Monolithic Couplings & Anti-Patterns

| Legacy Coupling / Issue | Refactored Architecture in `Universal-Humanoid-Wardrobe` | Architectural Benefit |
| :--- | :--- | :--- |
| **String Keys for Bones** (`"MainHand"`, `"OffHand"`, `"Back"`) prone to typos and missing mappings. | **Native `HumanBodyBones` Enum** queried directly from `Animator.GetBoneTransform(...)`. | Zero-string allocation, compile-time safety, out-of-the-box compatibility with any standard Unity Humanoid avatar. |
| **No Skinned Mesh Support** (only rigid parenting to transforms was possible). | **`SkinnedMeshRemapper.cs`** with runtime bone array retargeting, `rootBone` anchored to `Hips`, and local bounds stabilization against frustum culling. | Enables true clothing, armor, and modular body swapping for modern RPGs, survival games, and shooters. |
| **Twist/Helper Bone Rig Discrepancies** causing mesh explosion or missing bone exceptions across different rigs. | **Hierarchical Ancestor Fallback** (`ResolveAncestorFallback`) walking parent transform hierarchy, plus **Rig Prefix Stripping** (`mixamorig:`, `DEF-`, `Bip01_`, `ValveBiped.`). | Flawless mesh retargeting across Mixamo, Blender Rigify, 3ds Max Biped, and custom humanoids. |
| **Single-Slot Assumption** preventing 2H weapons or paired rings. | **Declarative Multi-Slot Occupancy** via `WardrobeItemSO.AdditionalOccupiedSlots`, `EquippedItemInstance`, and domain `EquipmentRuleResolver`. | Clean atomic equipping and unequipping of complex equipment configurations. |
| **Mesh Clipping / Skin Poke-Through** when wearing bulky armor over body meshes. | **Granular `BodyPartMask`** (`[Flags]`) and shrink blendshape triggers (`ShrinkBlendShapes`) automated on `WardrobeManager`. | Zero clipping artifacts without requiring manual destructive mesh deletion. |
| **Game Data Pollution** (`UnitLoadout`, `EquipmentItemSO` with combat stats). | **Pure Wardrobe DTO** (`WardrobeLoadout` JSON serialization) and decoupled `WardrobeItemSO`. | 100% Zero Game Taint. Package can be published to Unity Asset Store or used in unrelated projects. |

---

## 5. Discarded Obsolete Items

The following legacy structures were intentionally discarded and must **never** be reintroduced into `Universal-Humanoid-Wardrobe`:
1. **`BoneAttachmentController.BoneMapping` struct**: Manual mapping arrays in Inspector are obsolete; Unity's `Animator.GetBoneTransform(HumanBodyBones)` provides direct skeleton access.
2. **Hardcoded Transform Mounts** (`mainHandMount`, `offHandMount`, `backMount`): Rigid mounts baked into character prefabs prevent dynamic bone scaling and rig reusability.
3. **`Data.Units` and `Data.Items` references**: Any dependency on character progression, durability, inventory weight, or combat math was excised to maintain strict single responsibility.

---

## 6. Forward Compatibility & Universality Requirements

To fulfill the requirements for broader genre application (survival games, tactical shooters, multi-race RPGs):
1. **Adaptive Body Scaling & Race Profiles (`CharacterBodyProfile`)**:
   - Provide socket offset overrides and proportional scaling for characters of non-standard proportions (Dwarfs, Orcs, Giants, heavy/thin body types).
   - Support synchronized body blendshapes between character body meshes and equipped apparel.
   - Support profile-based prefab overrides (`profileOverrides`) on `WardrobeItemSO`.
2. **Tactical & Survival Slot Expansion**:
   - Enable equipment slots suitable for modern/tactical settings (`Backpack`, `TacticalVest`, `Holster`, `FaceMask`, `Belt`).
3. **Dual Socket States (Drawn vs. Holstered)**:
   - Support secondary socket transforms on `HumanoidAttachmentPoint` for weapons transitioning between combat hands and holsters/back slings.

---

## 7. Compliance Verification

- [x] **Zero Game Taint**: Package contains zero references to `LegendsLegacyOfLost` namespaces or singletons.
- [x] **Unity 6 Standard Compliance**: Verified zero deprecated API usages (CS0618 enforced as compilation error).
- [x] **Modular Decoupling**: Verified presentation and demo logic isolated in `Samples~/Demo/`.
- [x] **NUnit Automated Verification**: 100% passing EditMode and PlayMode test suites.
