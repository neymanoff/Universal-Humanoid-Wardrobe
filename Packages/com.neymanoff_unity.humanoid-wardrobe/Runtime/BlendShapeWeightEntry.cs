using System;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Represents a target weight for a named character blendshape.
    /// Used by BodyScaleProfileSO and CharacterBodyScale to define body archetype morphs.
    /// </summary>
    [Serializable]
    public struct BlendShapeWeightEntry
    {
        [Tooltip("Exact name of the morph target / blendshape on the body and clothing meshes.")]
        public string blendShapeName;

        [Range(0f, 100f)]
        [Tooltip("Target weight of the blendshape from 0 to 100.")]
        public float weight;

        public BlendShapeWeightEntry(string blendShapeName, float weight)
        {
            this.blendShapeName = blendShapeName;
            this.weight = Mathf.Clamp(weight, 0f, 100f);
        }
    }
}
