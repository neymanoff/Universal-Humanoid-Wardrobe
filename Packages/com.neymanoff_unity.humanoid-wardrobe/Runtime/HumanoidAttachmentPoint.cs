using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Tuned transform offsets for a specific body profile on this item prefab.
    /// </summary>
    [Serializable]
    public struct ProfileTransformOverride
    {
        [Tooltip("Target body profile identifier (e.g. 'Dwarf', 'Orc', 'Heavy', 'Female').")]
        public string profileId;

        [Tooltip("Local position offset tuned specifically for this profile.")]
        public Vector3 localPosition;

        [Tooltip("Local rotation offset tuned specifically for this profile (Euler angles).")]
        public Vector3 localRotation;

        [Tooltip("Local scale tuned specifically for this profile.")]
        public Vector3 localScale;

        public ProfileTransformOverride(string profileId, Vector3 localPosition, Vector3 localRotation, Vector3 localScale)
        {
            this.profileId = profileId;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.localScale = localScale;
        }
    }

    /// <summary>
    /// Component attached to an equipment prefab (like a weapon or shield)
    /// to define which humanoid bone it attaches to and its local offsets.
    /// Supports adaptive body scaling and per-profile fine tuning.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/Humanoid Attachment Point")]
    public class HumanoidAttachmentPoint : MonoBehaviour
    {
        [Header("Bone Selection")]
        [Tooltip("If true, ignores the slot's default bone and uses TargetBone below.")]
        [SerializeField] private bool useCustomBone = false;

        [Tooltip("Custom target bone (only used if UseCustomBone is enabled).")]
        [SerializeField] private HumanBodyBones targetBone = HumanBodyBones.RightHand;

        [Header("Default Offsets")]
        [Tooltip("Local position offset relative to the bone.")]
        [SerializeField] private Vector3 localPosition = Vector3.zero;

        [Tooltip("Local rotation offset relative to the bone (Euler angles).")]
        [SerializeField] private Vector3 localRotation = Vector3.zero;

        [Tooltip("Local scale override (usually 1, 1, 1).")]
        [SerializeField] private Vector3 localScale = Vector3.one;

        [Header("Mirroring Options")]
        [Tooltip("Automatically mirror Position X and Rotation Y/Z when equipped in OffHand / Left slots.")]
        [SerializeField] private bool autoMirrorForLeftSlot = true;

        [Header("Profile Specific Overrides")]
        [Tooltip("Optional transform overrides tuned specifically for different body profiles (e.g. Dwarf, Orc).")]
        [SerializeField] private List<ProfileTransformOverride> profileOverrides = new();

        [Header("Holstered State Options")]
        [Tooltip("If enabled, allows this item to transition between in-hand (Drawn) and stowed (Holstered) states.")]
        [SerializeField] private bool supportsHolsteredState = false;

        [Tooltip("Target humanoid bone when holstered (e.g. Hips for a sidearm/sword, Chest for a back rifle/shield).")]
        [SerializeField] private HumanBodyBones holsteredBone = HumanBodyBones.Hips;

        [Tooltip("Local position offset relative to the holstered bone.")]
        [SerializeField] private Vector3 holsteredLocalPosition = Vector3.zero;

        [Tooltip("Local rotation offset relative to the holstered bone (Euler angles).")]
        [SerializeField] private Vector3 holsteredLocalRotation = Vector3.zero;

        [Tooltip("Local scale override when holstered (usually 1, 1, 1).")]
        [SerializeField] private Vector3 holsteredLocalScale = Vector3.one;

        [Tooltip("Optional transform overrides tuned specifically for holstered state across different body profiles.")]
        [SerializeField] private List<ProfileTransformOverride> holsteredProfileOverrides = new();

        public bool UseCustomBone => useCustomBone;
        public HumanBodyBones TargetBone => targetBone;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalRotation => localRotation;
        public Vector3 LocalScale => localScale;
        public bool AutoMirrorForLeftSlot => autoMirrorForLeftSlot;
        public IReadOnlyList<ProfileTransformOverride> ProfileOverrides => profileOverrides;

        public SocketAttachmentState CurrentSocketState { get; private set; } = SocketAttachmentState.Drawn;
        public bool SupportsHolsteredState => supportsHolsteredState;
        public HumanBodyBones HolsteredBone => holsteredBone;
        public Vector3 HolsteredLocalPosition => holsteredLocalPosition;
        public Vector3 HolsteredLocalRotation => holsteredLocalRotation;
        public Vector3 HolsteredLocalScale => holsteredLocalScale;
        public IReadOnlyList<ProfileTransformOverride> HolsteredProfileOverrides => holsteredProfileOverrides;

        /// <summary>
        /// Applies local offsets, optionally factoring in character body scale profile and left-side mirroring.
        /// </summary>
        /// <param name="isLeftSlot">True if equipped in a left-side slot (OffHand, LeftRing).</param>
        /// <param name="bodyScale">Optional character body scale component defining archetype and instance overrides.</param>
        /// <param name="slot">The equipment slot where this item is being attached.</param>
        public void ApplyOffsets(bool isLeftSlot = false, CharacterBodyScale bodyScale = null, EquipmentSlot slot = EquipmentSlot.MainHand)
        {
            Vector3 pos = localPosition;
            Vector3 rot = localRotation;
            Vector3 scale = localScale;

            // 1. Check if this attachment point has a bespoke override for the character's profile
            string profileId = bodyScale != null ? bodyScale.EffectiveProfileId : null;
            if (!string.IsNullOrEmpty(profileId) && profileOverrides != null)
            {
                for (int i = 0; i < profileOverrides.Count; i++)
                {
                    if (string.Equals(profileOverrides[i].profileId, profileId, StringComparison.OrdinalIgnoreCase))
                    {
                        pos = profileOverrides[i].localPosition;
                        rot = profileOverrides[i].localRotation;
                        scale = profileOverrides[i].localScale;
                        break;
                    }
                }
            }

            // 2. Apply global prop scale multiplier and slot adjustments from CharacterBodyScale
            if (bodyScale != null)
            {
                scale *= bodyScale.EffectivePropScale;

                // 3. Apply slot-specific offset adjustments (from character instance or profile)
                if (bodyScale.TryGetSocketOverride(slot, out var socketOverride))
                {
                    pos += socketOverride.positionOffset;
                    rot += socketOverride.rotationOffset;
                    scale = Vector3.Scale(scale, socketOverride.scaleMultiplier);
                }
            }

            // 4. Apply left-side mirroring
            if (isLeftSlot && autoMirrorForLeftSlot)
            {
                pos.x = -pos.x;
                rot.y = -rot.y;
                rot.z = -rot.z;
            }

            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(rot);
            transform.localScale = scale;
        }

        /// <summary>
        /// Transitions the item between Drawn (in-hand active) and Holstered (stowed) states,
        /// re-parenting to the target bone and applying the appropriate local offsets.
        /// </summary>
        public bool SetSocketState(
            SocketAttachmentState state,
            Animator animator,
            CharacterBodyScale bodyScale = null,
            EquipmentSlot slot = EquipmentSlot.MainHand,
            bool isLeftSlot = false)
        {
            if (state == SocketAttachmentState.Holstered && !supportsHolsteredState)
            {
                return false;
            }

            if (animator == null)
            {
                return false;
            }

            if (state == SocketAttachmentState.Holstered)
            {
                Transform targetBoneTransform = null;
                if (animator.isHuman && animator.avatar != null)
                {
                    targetBoneTransform = animator.GetBoneTransform(holsteredBone);
                }

                if (targetBoneTransform == null)
                {
                    targetBoneTransform = ResolveBoneFallback(animator.transform, holsteredBone);
                }

                if (targetBoneTransform == null)
                {
                    Debug.LogWarning($"[HumanoidAttachmentPoint] Holstered bone {holsteredBone} not found on {animator.gameObject.name}!");
                    return false;
                }

                transform.SetParent(targetBoneTransform, false);
                ApplyHolsteredOffsets(bodyScale);
                CurrentSocketState = SocketAttachmentState.Holstered;
                return true;
            }
            else
            {
                HumanBodyBones activeBone = useCustomBone ? targetBone : WardrobeManager.GetDefaultBoneForSlot(slot);
                Transform targetBoneTransform = null;
                if (animator.isHuman && animator.avatar != null)
                {
                    targetBoneTransform = animator.GetBoneTransform(activeBone);
                }

                if (targetBoneTransform == null)
                {
                    targetBoneTransform = ResolveBoneFallback(animator.transform, activeBone);
                }

                if (targetBoneTransform == null)
                {
                    Debug.LogWarning($"[HumanoidAttachmentPoint] Active bone {activeBone} not found on {animator.gameObject.name}!");
                    return false;
                }

                transform.SetParent(targetBoneTransform, false);
                ApplyOffsets(isLeftSlot, bodyScale, slot);
                CurrentSocketState = SocketAttachmentState.Drawn;
                return true;
            }
        }

        /// <summary>
        /// Attempts to find a matching bone transform in the character hierarchy when Animator.GetBoneTransform is null.
        /// Useful for greybox prototyping rigs and testing environments without a full Mecanim Humanoid Avatar.
        /// </summary>
        public static Transform ResolveBoneFallback(Transform root, HumanBodyBones bone)
        {
            if (root == null) return null;
            string targetName = bone.ToString();
            return FindBoneRecursive(root, targetName);
        }

        private static Transform FindBoneRecursive(Transform current, string targetName)
        {
            if (string.Equals(current.name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            string strippedName = current.name;
            int colonIndex = strippedName.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < strippedName.Length - 1)
            {
                strippedName = strippedName.Substring(colonIndex + 1);
                if (string.Equals(strippedName, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindBoneRecursive(current.GetChild(i), targetName);
                if (found != null) return found;
            }

            return null;
        }

        private void ApplyHolsteredOffsets(CharacterBodyScale bodyScale)
        {
            Vector3 pos = holsteredLocalPosition;
            Vector3 rot = holsteredLocalRotation;
            Vector3 scale = holsteredLocalScale;

            string profileId = bodyScale != null ? bodyScale.EffectiveProfileId : null;
            if (!string.IsNullOrEmpty(profileId) && holsteredProfileOverrides != null)
            {
                for (int i = 0; i < holsteredProfileOverrides.Count; i++)
                {
                    if (string.Equals(holsteredProfileOverrides[i].profileId, profileId, StringComparison.OrdinalIgnoreCase))
                    {
                        pos = holsteredProfileOverrides[i].localPosition;
                        rot = holsteredProfileOverrides[i].localRotation;
                        scale = holsteredProfileOverrides[i].localScale;
                        break;
                    }
                }
            }

            if (bodyScale != null)
            {
                scale *= bodyScale.EffectivePropScale;
            }

            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(rot);
            transform.localScale = scale;
        }

        [ContextMenu("Capture Current Transform as Default Offsets")]
        private void CaptureCurrentTransform()
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation.eulerAngles;
            localScale = transform.localScale;
            Debug.Log($"[HumanoidAttachmentPoint] Captured default offsets for {gameObject.name}");
        }

        [ContextMenu("Capture Current Transform as Holstered Offsets")]
        private void CaptureHolsteredTransform()
        {
            holsteredLocalPosition = transform.localPosition;
            holsteredLocalRotation = transform.localRotation.eulerAngles;
            holsteredLocalScale = transform.localScale;
            Debug.Log($"[HumanoidAttachmentPoint] Captured holstered offsets for {gameObject.name}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Testing helper to configure attachment point properties in-memory.
        /// </summary>
        public void ConfigureForTest(
            Vector3 pos,
            Vector3 rot,
            Vector3 sc,
            bool autoMirror = true,
            IEnumerable<ProfileTransformOverride> overrides = null)
        {
            localPosition = pos;
            localRotation = rot;
            localScale = sc;
            autoMirrorForLeftSlot = autoMirror;
            profileOverrides = overrides != null ? new List<ProfileTransformOverride>(overrides) : new List<ProfileTransformOverride>();
        }

        /// <summary>
        /// Testing helper to configure holstered options in-memory.
        /// </summary>
        public void ConfigureHolsteredForTest(
            bool supports,
            HumanBodyBones bone,
            Vector3 pos,
            Vector3 rot,
            Vector3 scale,
            IEnumerable<ProfileTransformOverride> profileOverrides = null)
        {
            supportsHolsteredState = supports;
            holsteredBone = bone;
            holsteredLocalPosition = pos;
            holsteredLocalRotation = rot;
            holsteredLocalScale = scale;
            holsteredProfileOverrides = profileOverrides != null ? new List<ProfileTransformOverride>(profileOverrides) : new List<ProfileTransformOverride>();
        }
#endif
    }
}
