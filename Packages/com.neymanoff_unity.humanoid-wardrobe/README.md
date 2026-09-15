# Universal Humanoid Wardrobe

A modular, lightweight, and high-performance Unity package for dynamic character equipment and wardrobe customization on Humanoid rigs.

Designed for standalone integration in any Unity project or RPG framework, supporting both skinned clothing/armor remapping and rigid bone attachments (weapons, helmets, shields, accessories).

---

## Features

- **Skinned Mesh Remapping**: Seamlessly remaps clothing and armor `SkinnedMeshRenderer` bones to the target Humanoid skeleton at runtime without modifying original assets.
- **Bone Sockets & Attachments**: Equips rigid objects (weapons, props, shields, jewelry) directly to standard Unity `HumanBodyBones` with customizable local offsets and auto-mirroring for left-hand/ring slots.
- **Slot Restriction Rules**: Built-in support for Two-Handed weapon slot conflicts (automatically unequipping off-hand items), One-Handed versatility, and ring swaps.
- **Decoupled Architecture**: Completely independent of any specific inventory or UI framework. Integrates via C# events and serializable loadout data.
- **Editor Previewing**: Preview default equipment loadouts directly in the Unity Editor scene view without entering Play Mode.
- **Ready for Unity 6**: Fully compatible with modern Unity versions and Universal Render Pipeline (URP).

---

## Installation

### Via Unity Package Manager (UPM Git URL)
1. In the Unity Editor, open **Window > Package Manager**.
2. Click the `+` button and select **Add package from git URL...**
3. Enter repository URL:
   ```text
   https://github.com/neymanoff/Universal-Humanoid-Wardrobe.git?path=Packages/com.neymanoff_unity.humanoid-wardrobe
   ```

### As Embedded Local Package
Copy the `com.neymanoff_unity.humanoid-wardrobe` folder directly into your project's `Packages/` directory.

---

## Quick Usage Guide

### 1. Preparing the Character
Add the `WardrobeManager` component to your humanoid character (the GameObject containing the `Animator` component):
```csharp
using Neymanoff.HumanoidWardrobe;

public class CharacterInitializer : MonoBehaviour
{
    [SerializeField] private WardrobeManager wardrobeManager;
    [SerializeField] private WardrobeItemSO starterHelmet;

    private void Start()
    {
        wardrobeManager.EquipItemSO(starterHelmet, EquipmentSlot.Head);
    }
}
```

### 2. Creating an Item (ScriptableObject)
1. In the Project window, right-click and select **Create > Humanoid Wardrobe > Wardrobe Item**.
2. Set the `Item Name`, `Target Slot`, and `Restriction`.
3. Assign the UI Icon and the 3D Prefab.

### 3. Creating an Equippable Prefab
- **For Clothing / Armor**: Add `SkinnedMeshRemapper` to the clothing prefab.
- **For Weapons / Helmets / Props**: Add `HumanoidAttachmentPoint` to configure bone offset and rotation.

---

## Documentation

For full API reference and technical details, see the [Package Documentation](Documentation/humanoid-wardrobe.md).

## License

Released under the MIT License.