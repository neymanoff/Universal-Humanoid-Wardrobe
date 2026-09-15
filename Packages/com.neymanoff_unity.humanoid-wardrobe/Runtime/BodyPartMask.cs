using System;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Granular bitmask flags representing anatomical regions of a Humanoid character.
    /// Used by WardrobeItemSO to declare which body regions are concealed by apparel,
    /// and by WardrobeManager to hide occluded body geometry or apply shrink blendshapes
    /// to prevent mesh clipping.
    /// </summary>
    [Flags]
    public enum BodyPartMask
    {
        None = 0,
        Head = 1 << 0,
        UpperTorso = 1 << 1,  // Chest, clavicles, shoulder blades
        LowerTorso = 1 << 2,  // Abdomen, stomach, waist, lower back
        UpperArms = 1 << 3,   // Shoulders, biceps, triceps
        LowerArms = 1 << 4,   // Forearms
        Hands = 1 << 5,       // Wrists, palms, fingers
        UpperLegs = 1 << 6,   // Hips, thighs, buttocks
        LowerLegs = 1 << 7,   // Knees, shins, calves
        Feet = 1 << 8         // Ankles, feet, toes
    }
}
