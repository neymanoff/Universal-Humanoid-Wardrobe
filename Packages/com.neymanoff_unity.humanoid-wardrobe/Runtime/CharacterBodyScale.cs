using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Component placed on a character to manage body archetype scaling,
    /// socket offset compensation, and body blendshape weights.
    /// Supports both shared profiles (BodyScaleProfileSO) and granular one-off individual overrides.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/Character Body Scale")]
    public class CharacterBodyScale : MonoBehaviour
    {
        [Header("Shared Archetype Profile (Optional)")]
        [Tooltip("Shared profile defining default scaling and socket offsets for this body type/race (e.g. Dwarf, Orc, Standard).")]
        [SerializeField] private BodyScaleProfileSO baseProfile;

        [Header("Individual Instance Fine-Tuning")]
        [Tooltip("If enabled, overrides the base profile's prop scale multiplier for this specific character instance.")]
        [SerializeField] private bool overridePropScale = false;

        [Tooltip("Custom prop scale multiplier applied to rigid items on this specific character instance.")]
        [SerializeField] private float instancePropScaleMultiplier = 1f;

        [Tooltip("Individual per-slot socket offset overrides applied to this specific character instance.")]
        [SerializeField] private List<SocketOffsetOverride> instanceSocketOverrides = new();

        [Tooltip("Individual blendshape weight overrides applied to this specific character instance.")]
        [SerializeField] private List<BlendShapeWeightEntry> instanceMorphOverrides = new();

        public BodyScaleProfileSO BaseProfile
        {
            get => baseProfile;
            set => baseProfile = value;
        }

        public bool OverridePropScale
        {
            get => overridePropScale;
            set => overridePropScale = value;
        }

        public float InstancePropScaleMultiplier
        {
            get => instancePropScaleMultiplier;
            set => instancePropScaleMultiplier = value;
        }

        /// <summary>
        /// Effective profile identifier. Returns base profile's ID or 'Default' if unassigned.
        /// </summary>
        public string EffectiveProfileId
        {
            get
            {
                if (baseProfile != null && !string.IsNullOrEmpty(baseProfile.ProfileId))
                {
                    return baseProfile.ProfileId;
                }
                return "Default";
            }
        }

        /// <summary>
        /// Effective prop scale multiplier. Resolves with priority:
        /// 1. Instance override (if enabled)
        /// 2. Base profile multiplier (if assigned)
        /// 3. Default fallback (1.0)
        /// </summary>
        public float EffectivePropScale
        {
            get
            {
                if (overridePropScale)
                {
                    return Mathf.Max(0.01f, instancePropScaleMultiplier);
                }
                if (baseProfile != null)
                {
                    return baseProfile.PropScaleMultiplier;
                }
                return 1f;
            }
        }

        public IReadOnlyList<SocketOffsetOverride> InstanceSocketOverrides => instanceSocketOverrides;
        public IReadOnlyList<BlendShapeWeightEntry> InstanceMorphOverrides => instanceMorphOverrides;

        /// <summary>
        /// Attempts to retrieve a socket offset override using cascading priority:
        /// 1. Individual character instance override
        /// 2. Shared archetype profile override
        /// </summary>
        public bool TryGetSocketOverride(EquipmentSlot slot, out SocketOffsetOverride result)
        {
            // 1. Check individual instance overrides first
            if (instanceSocketOverrides != null)
            {
                for (int i = 0; i < instanceSocketOverrides.Count; i++)
                {
                    if (instanceSocketOverrides[i].slot == slot)
                    {
                        result = instanceSocketOverrides[i];
                        return true;
                    }
                }
            }

            // 2. Fallback to shared archetype profile
            if (baseProfile != null && baseProfile.TryGetSocketOverride(slot, out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Aggregates all effective blendshape morph weights combining base profile morphs
        /// with individual instance overrides.
        /// </summary>
        public Dictionary<string, float> GetEffectiveMorphWeights()
        {
            Dictionary<string, float> weights = new(StringComparer.OrdinalIgnoreCase);

            // 1. Populate base profile morphs
            if (baseProfile != null && baseProfile.BaseBodyMorphs != null)
            {
                for (int i = 0; i < baseProfile.BaseBodyMorphs.Count; i++)
                {
                    var entry = baseProfile.BaseBodyMorphs[i];
                    if (!string.IsNullOrEmpty(entry.blendShapeName))
                    {
                        weights[entry.blendShapeName] = entry.weight;
                    }
                }
            }

            // 2. Layer individual instance morph overrides
            if (instanceMorphOverrides != null)
            {
                for (int i = 0; i < instanceMorphOverrides.Count; i++)
                {
                    var entry = instanceMorphOverrides[i];
                    if (!string.IsNullOrEmpty(entry.blendShapeName))
                    {
                        weights[entry.blendShapeName] = entry.weight;
                    }
                }
            }

            return weights;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Testing helper to configure character body scale in-memory without inspector serialization.
        /// </summary>
        public void ConfigureForTest(
            BodyScaleProfileSO profile,
            bool overrideScale = false,
            float customScale = 1f,
            IEnumerable<SocketOffsetOverride> instanceOffsets = null,
            IEnumerable<BlendShapeWeightEntry> instanceMorphs = null)
        {
            baseProfile = profile;
            overridePropScale = overrideScale;
            instancePropScaleMultiplier = customScale;
            instanceSocketOverrides = instanceOffsets != null ? new List<SocketOffsetOverride>(instanceOffsets) : new List<SocketOffsetOverride>();
            instanceMorphOverrides = instanceMorphs != null ? new List<BlendShapeWeightEntry>(instanceMorphs) : new List<BlendShapeWeightEntry>();
        }
#endif
    }
}
