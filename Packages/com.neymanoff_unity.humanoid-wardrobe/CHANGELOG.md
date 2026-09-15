# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-09-15

### Added
- Multi-slot occupancy architecture (`allowedSlots`, `additionalOccupiedSlots`, `EquippedItemInstance`) supporting two-handed weapons and cross-slot dual-wielding.
- Explicit equipment status contracts via `EquipResult` and `EquipResultStatus` enum.
- Pure domain helper `EquipmentRuleResolver` decoupling rule evaluation from visual instantiation.
- Persistent loadout DTO (`WardrobeLoadout`) with JSON serialization and atomic batch rehydration.
- Robust twist/helper bone handling with hierarchical ancestor fallback and normalized prefix resolution in `SkinnedMeshRemapper`.
- Frustum culling bounds stabilization inheriting base character bounding boxes to prevent flicker.
- Hybrid anti-clipping system: granular anatomical `BodyPartMask` flags and automated `shrinkBlendShapes` morph evaluation in `WardrobeManager`.
- Step-by-step 3D artist Blender 4.x authoring guide for modular sub-meshes and shrink shape keys in `docs/BLENDER_CHARACTER_SETUP.md`.
- Official UPM sample structure (`Samples~/Demo/`) with dedicated `Neymanoff.HumanoidWardrobe.Demo.asmdef` and `.sample.json` manifest.
- Comprehensive automated NUnit test suite under `Tests/Runtime/` covering domain rules, serialization fidelity, contracts, bitmasks, and manager lifecycle.
- Interactive demo UI enhancement with live JSON preset save/load hotkeys (`F5` save, `F9` load, `C` clear).

### Fixed
- Fixed destroyed bone references when clothing contains twist bones absent from the humanoid skeleton.
- Fixed frustum culling mesh disappearing when camera moves away from origin.
- Fixed assembly reference and meta file corruptions across tests and samples.

### Changed
- Refactored `WardrobeManager` to be 100% gameplay- and UI-agnostic with zero TextMeshPro or UI dependencies in the core runtime assembly.
- Standardized package metadata in `package.json` with keywords and sample definitions.

## [0.1.0] - 2026-07-11

### Added
- Initial implementation of `WardrobeManager` with runtime equip and unequip capabilities.
- Added `SkinnedMeshRemapper` for runtime bone hierarchy remapping of clothing meshes.
- Added `HumanoidAttachmentPoint` for socket bone attachments with mirroring support.
- Added `WardrobeItemSO` ScriptableObject data model with slot restriction rules.
- Added demo paper-doll equipment UI and animation test controller.
