# Universal Humanoid Wardrobe - Project Rules & Environment Memory

## 1. Unity Environment & Unity CLI
- **Unity Engine**: Unity 6 (6000.x / 6.5 LTS) with URP (Universal Render Pipeline) and New Input System.
- **Unity CLI**: `unity` CLI (version 1.0.0-beta) is installed globally.
  - The AI assistant may run `unity package`, `unity test`, and `unity build` CLI commands via `run_command` for background validation and UPM management.

## 2. Project Architecture & Package Structure
- **Package ID**: `com.neymanoff_unity.humanoid-wardrobe`
- **Location**: `Packages/com.neymanoff_unity.humanoid-wardrobe/`
- **Assemblies**: 
  - Runtime: `Neymanoff.HumanoidWardrobe.asmdef`
  - Editor: `Neymanoff.HumanoidWardrobe.Editor.asmdef`
- **Core Runtime Scripts**:
  - `HumanoidAttachmentPoint.cs`: Manages static props, weapons, shields, and headgear parented to humanoid bones with local offsets and auto left-side mirroring.
  - `SkinnedMeshRemapper.cs`: Remaps soft clothing, armor, and footwear `SkinnedMeshRenderer.bones` and `rootBone` to the character skeleton.
  - `WardrobeItemSO.cs`: ScriptableObject holding item metadata, icon, prefab reference, target slot, and `ItemSlotRestriction` validation.
  - `WardrobeManager.cs`: Central character component managing equipment dictionaries (`_equipmentItems`, `_equipmentItemData`), 2H weapon auto-unequip rules, and event notifications (`OnEquipmentChanged`).
- **Runtime UI & Demo Controls**:
  - `EquipmentSlotUI.cs`: Paperdoll equipment slot controller supporting default silhouettes, item icons, and Diablo 2 style 2H blocking tints.
  - `DemoInventoryUI.cs`: Manages inventory grid buttons and auto-hides equipped items.
  - `WardrobeDemoAnimationController.cs`: Controls `Animator` playback speed, state switching (`PlayState`), and static A-Pose fitting mode.
  - `CharacterRotator.cs`: Mouse-drag character rotation controller using New Input System.

## 3. Development Guidelines & Mentorship Role
- **Mentor Role**: Explain architectural choices, line-by-line rationale, and alternatives clearly.
- **Strong Typing**: Always use enums (`EquipmentSlot`, `ItemSlotRestriction`) instead of string searches.
- **Modern APIs**: Avoid deprecated Unity methods (e.g. use `FindFirstObjectByType` or Inspector serialization instead of legacy `FindObjectOfType`, use New Input System `Mouse.current` instead of legacy `Input.GetAxis`).
