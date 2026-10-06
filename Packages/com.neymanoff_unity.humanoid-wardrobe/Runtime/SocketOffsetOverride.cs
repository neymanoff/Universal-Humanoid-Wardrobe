using System;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Overrides position, rotation, and scale for a specific equipment slot socket.
    /// Can be defined at the archetype profile level (BodyScaleProfileSO) or
    /// overridden on an individual character instance (CharacterBodyScale).
    /// </summary>
    [Serializable]
    public struct SocketOffsetOverride
    {
        [Tooltip("The equipment slot targeted by this offset override.")]
        public EquipmentSlot slot;

        [Tooltip("Additive position offset applied to the socket.")]
        public Vector3 positionOffset;

        [Tooltip("Additive Euler rotation offset applied to the socket.")]
        public Vector3 rotationOffset;

        [Tooltip("Multiplicative scale applied to the socket (default 1, 1, 1).")]
        public Vector3 scaleMultiplier;

        public SocketOffsetOverride(EquipmentSlot slot, Vector3 positionOffset, Vector3 rotationOffset, Vector3 scaleMultiplier)
        {
            this.slot = slot;
            this.positionOffset = positionOffset;
            this.rotationOffset = rotationOffset;
            this.scaleMultiplier = scaleMultiplier;
        }

        public static SocketOffsetOverride Default(EquipmentSlot slot) =>
            new(slot, Vector3.zero, Vector3.zero, Vector3.one);
    }
}
