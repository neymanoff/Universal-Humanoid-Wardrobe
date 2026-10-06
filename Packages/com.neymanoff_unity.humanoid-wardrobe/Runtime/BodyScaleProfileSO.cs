using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// ScriptableObject defining a shared body scale profile or archetype (e.g. Dwarf, Orc, Giant, SlenderElf, Standard).
    /// Holds default prop scale multipliers, per-slot socket offset adjustments, and base body morph weights.
    /// Can be assigned to multiple characters sharing the same body type.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBodyScaleProfile", menuName = "Humanoid Wardrobe/Body Scale Profile")]
    public class BodyScaleProfileSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique profile identifier used for matching variants and overrides (e.g. 'Dwarf', 'Orc', 'Heavy', 'Standard').")]
        [SerializeField] private string profileId = "Standard";

        [Tooltip("Descriptive name or label for this body profile.")]
        [SerializeField] private string displayName = "Standard Body";

        [Header("Rigid Props Scaling")]
        [Tooltip("Global scale multiplier applied to rigid props (weapons, shields, helmets) for characters with this profile.")]
        [SerializeField] private float propScaleMultiplier = 1f;

        [Header("Archetype Socket Offsets")]
        [Tooltip("Default socket offset adjustments for characters sharing this profile.")]
        [SerializeField] private List<SocketOffsetOverride> socketOverrides = new();

        [Header("Archetype Body Morphs")]
        [Tooltip("Default blendshape weights associated with this body archetype (e.g. Shape_Dwarf: 100, Shape_Heavy: 80).")]
        [SerializeField] private List<BlendShapeWeightEntry> baseBodyMorphs = new();

        public string ProfileId => string.IsNullOrEmpty(profileId) ? name : profileId;
        public string DisplayName => displayName;
        public float PropScaleMultiplier => Mathf.Max(0.01f, propScaleMultiplier);
        public IReadOnlyList<SocketOffsetOverride> SocketOverrides => socketOverrides;
        public IReadOnlyList<BlendShapeWeightEntry> BaseBodyMorphs => baseBodyMorphs;

        /// <summary>
        /// Attempts to find a socket override configured for the specified slot.
        /// </summary>
        public bool TryGetSocketOverride(EquipmentSlot slot, out SocketOffsetOverride result)
        {
            if (socketOverrides != null)
            {
                for (int i = 0; i < socketOverrides.Count; i++)
                {
                    if (socketOverrides[i].slot == slot)
                    {
                        result = socketOverrides[i];
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Testing helper to configure profile properties in-memory without inspector serialization.
        /// </summary>
        public void ConfigureForTest(
            string id,
            float propScale,
            IEnumerable<SocketOffsetOverride> socketOffsets = null,
            IEnumerable<BlendShapeWeightEntry> morphs = null)
        {
            profileId = id;
            propScaleMultiplier = propScale;
            socketOverrides = socketOffsets != null ? new List<SocketOffsetOverride>(socketOffsets) : new List<SocketOffsetOverride>();
            baseBodyMorphs = morphs != null ? new List<BlendShapeWeightEntry>(morphs) : new List<BlendShapeWeightEntry>();
        }
#endif
    }
}
