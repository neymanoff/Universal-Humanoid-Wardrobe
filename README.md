# Universal Humanoid Wardrobe (Unity 6)

[![Unity 6](https://img.shields.io/badge/Unity-6000.5+-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-In%20Active%20Development-orange.svg)](#)

A modular, lightweight character customization and equipment wardrobe system for Unity.

Designed as an official **Unity Package** (UPM) for free distribution on the **Unity Asset Store** and open-source portfolio presentation.

---

## 📌 Key Capabilities

* **Dual Equipping Paradigms**:
  1. **Rigid Socket Attachment**: Weapons, shields, helmets, jewelry, and props parent directly to standard `HumanBodyBones` sockets with customizable local transform offsets and auto-mirroring for left-side / off-hand slots. Highly universal across all standard Humanoid avatars.
  2. **Skinned Mesh Remapping**: Wearable clothing and armor meshes dynamically remap their bone bindings to match the host character's active skeleton at runtime without duplicating bone hierarchies.
* **Gameplay-Agnostic & Decoupled**: The wardrobe core does not know about combat, player stats, inventory databases, or economy. It manages equipment visualization, socket transforms, and bone bindings.
* **Multi-Slot Occupancy & Conflict Resolution**: Support for items occupying multiple slots (e.g. Two-Handed greatswords occupying both `MainHand` and `OffHand` simultaneously with atomic unequip).
* **Deterministic Event Model & Status Results**: Explicit `EquipResult` return codes and typed events (`OnItemEquipped`, `OnItemUnequipped`, `OnLoadoutChanged`) for frictionless integration with any external game system.
* **In-Editor Previewing**: Inspect and author character loadouts directly in the Scene View without entering Play Mode.
* **Turnkey Paper-Doll Demo**: Includes an optional demonstration UI featuring equipment slots, inventory selection grid, turntable character rotation, and fitting pose toggles.

---

## ⚠️ Compatibility Contract

To avoid unrealistic assumptions, the package defines clear compatibility boundaries:

* **Rigid Items (Weapons, Props, Jewelry)**: **Universal**. Any character with a valid Unity `Humanoid` Avatar can equip rigid items via `HumanBodyBones`.
* **Skinned Items (Clothing, Armor, Robes)**: **Rig-Dependent**. While Humanoid retargets animations cleanly, skinned meshes require matching bind poses, rest poses, body proportions, and compatible bone hierarchies (including twist and helper bones).

---

## 📂 Package Structure

* `Packages/com.neymanoff_unity.humanoid-wardrobe/` — The Core UPM package:
  * `Runtime/` — Core wardrobe scripts (`WardrobeManager`, `SkinnedMeshRemapper`, `HumanoidAttachmentPoint`, `WardrobeItemSO`).
  * `Runtime/UI/` — Demo UI components (scheduled for extraction into `Samples~/Demo`).
  * `Editor/` — Custom Unity Editor inspectors and preview tooling.
  * `Documentation/` — Technical package reference manual.
  * `Tests/` — EditMode and PlayMode automated test suites.
* `Assets/` — Project sandbox and test environment.

---

## 📚 Documentation

* 📐 **[Architecture Specification](docs/ARCHITECTURE.md)**: System design, UML class diagrams, sequence diagrams (Mermaid), multi-slot mapping, and persistence models.
* 🛠️ **[Integration & User Guide](docs/INTEGRATION_GUIDE.md)**: Step-by-step developer tutorial covering character setup, item authoring, and game system integration.
* 📋 **[Roadmap & Task Tracker](docs/TODO.md)**: Comprehensive tracking of completed features and active milestones.
* 🔧 **[Internal Development & Git Guide](docs/DEVELOPMENT.md)**: Git LFS recovery procedures and contributor workflow.
* 📖 **[Package Manual](Packages/com.neymanoff_unity.humanoid-wardrobe/Documentation/humanoid-wardrobe.md)**: In-package reference manual.

---

## 🚀 Quick Start

1. Open the project in **Unity 6 (6000.5.10f1 or newer)**.
2. Open `Assets/Scenes/WardrobeDemoScene.unity`.
3. Press **Play**:
   * Click items in the bottom inventory grid to equip them.
   * Click occupied equipment slots on the paper-doll UI to unequip.
   * Drag the left mouse button across the character to rotate preview (Turntable).
   * Use **Play Fitting Pose (T-Pose)** and **Play Idle** to evaluate mesh deformation in static and animated states.

---

## 📄 License
Released under the [MIT License](LICENSE). Free for commercial and non-commercial Unity projects.
