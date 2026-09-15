# Universal Humanoid Wardrobe (Unity 6)

[![Unity 6](https://img.shields.io/badge/Unity-6000.5+-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-In%20Active%20Development-orange.svg)](#)

A modular, lightweight, and high-performance dynamic character customization and equipment wardrobe system for Unity characters rigged with the standard **Humanoid** avatar.

Designed as an official **Unity Package** intended for free release on the **Unity Asset Store** and open-source portfolio presentation.

---

## 📌 Key Capabilities

* **Dual Equipping Modes**:
  1. **Skinned Mesh Remapping**: For deformable clothing, armor, pants, and boots. Automatically binds clothing renderer bones to the target character skeleton at runtime to faithfully follow all character animations.
  2. **Humanoid Bone Attachment**: For rigid items (weapons in hand, shields, helmets, rings, necklaces). Parents directly to standard `HumanBodyBones` with customizable local transform offsets and automatic mirroring for off-hand / left-side slots.
* **Game & Inventory Decoupled**: Operates independently of any specific inventory model, weight system, or stat calculation framework.
* **Slot Restriction Rules**: Built-in support for Two-Handed weapons (automatically unequipping off-hand items), One-Handed versatility, and ring swaps.
* **In-Editor Previewing**: Inspect default character loadouts in the Scene View without entering Play Mode.
* **Turnkey Paper-Doll Demo**: Includes an interactive demo UI featuring paper-doll equipment slots, inventory grid, character turntable rotation, and fitting pose controls.

---

## 📂 Project & Package Structure

* `Packages/com.neymanoff_unity.humanoid-wardrobe/` — The Core UPM package:
  * `Runtime/` — Core wardrobe scripts (`WardrobeManager`, `SkinnedMeshRemapper`, `HumanoidAttachmentPoint`, `WardrobeItemSO`).
  * `Runtime/UI/` — Demo inventory UI and paper-doll slot components.
  * `Editor/` — Custom Unity Editor inspectors and preview utilities.
  * `Documentation/` — Package manual and reference docs.
  * `Tests/` — EditMode and PlayMode test suites.
* `Assets/` — Development test environment:
  * `3D_Models/` — Test models (Humanoid Dummy, Slavic helmet, chest armor, shields, weapons).
  * `ScriptableObjects/` — Equippable wardrobe item database assets.
  * `Scenes/WardrobeDemoScene.unity` — Interactive demonstration scene.

---

## 📚 Documentation Index

For in-depth guides and technical specifications, explore the project documentation:

* 📐 **[Architecture Specification](docs/ARCHITECTURE.md)**: Full system architecture, UML Class diagrams, sequence diagrams (Mermaid), persistence models, and decoupling patterns.
* 🛠️ **[Integration & User Guide](docs/INTEGRATION_GUIDE.md)**: Step-by-step developer tutorial covering character setup, item creation, inventory integration, and save/load state.
* 📋 **[Roadmap & Task Tracker](docs/TODO.md)**: Comprehensive tracking of completed fixes, active tasks, and milestones toward Asset Store publication.
* 📖 **[Package Technical Manual](Packages/com.neymanoff_unity.humanoid-wardrobe/Documentation/humanoid-wardrobe.md)**: UPM package reference manual.

---

## 🛠️ Git LFS & Asset Recovery Notes

If 3D models or textures appear missing after cloning from GitHub:

1. **Root Cause**: `.fbx`, `.png`, and `.ttf` files are tracked via **Git LFS**. If binary objects were not uploaded to GitHub LFS storage (404 error during clone), Git places 130-byte text pointer files on disk.
2. **Restoring Meta Files**:
   All `.meta` files have been restored to preserve model GUIDs and humanoid avatar mappings:
   ```bash
   git checkout -- "*.meta"
   ```
3. **Placing Binary Assets**:
   Copy your original FBX and PNG files into the respective `Assets/3D_Models/...` directories. Unity will instantly re-bind all materials, prefabs, and scene instances without broken references.

---

## 🚀 Quick Start

1. Open the project in **Unity 6 (6000.5.10f1 or newer)**.
2. Open `Assets/Scenes/WardrobeDemoScene.unity`.
3. Press **Play**:
   * Click items in the bottom inventory grid to equip them.
   * Click occupied equipment slots on the paper-doll UI to unequip.
   * Click and drag the left mouse button across the character to rotate preview (Turntable).
   * Use **Play Fitting Pose (T-Pose)** and **Play Idle** to evaluate mesh deformation across static and animated states.

---

## 📄 License
Released under the [MIT License](LICENSE). Free for commercial and non-commercial Unity projects.
