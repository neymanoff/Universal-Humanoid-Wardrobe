# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-09-15

### Added
- Comprehensive package documentation and API reference manual in `Documentation/humanoid-wardrobe.md`.
- Full project documentation and Git LFS recovery guide in `README.md`.
- Detailed project audit and architectural roadmap in `docs/PROJECT_AUDIT_AND_ROADMAP.md`.

### Fixed
- Fixed typo in `Neymanoff.HumanoidWardrobe.Tests.asmdef` assembly reference (`Neymanoff.HumanoidWardrobeardrobe` -> `Neymanoff.HumanoidWardrobe`).
- Fixed invalid assembly references in `Neymanoff.HumanoidWardrobe.Editor.Tests.asmdef`.
- Restored missing `.meta` files across all asset categories after Git clone corruption.
- Cleaned up Git index state from accidental mass-staged deletions.

### Changed
- Standardized package metadata in `package.json` for Unity 6 compatibility.

## [0.1.0] - 2026-07-11

### Added
- Initial implementation of `WardrobeManager` with runtime equip and unequip capabilities.
- Added `SkinnedMeshRemapper` for runtime bone hierarchy remapping of clothing meshes.
- Added `HumanoidAttachmentPoint` for socket bone attachments with mirroring support.
- Added `WardrobeItemSO` ScriptableObject data model with slot restriction rules.
- Added demo paper-doll equipment UI and animation test controller.
