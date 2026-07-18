using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Supported equipment slots for wardrobe system
    /// </summary>
    public enum EquipmentSlot
    {
        Head,
        Shoulders,
        Chest,
        Hands,
        Legs,
        Feet,
        Neck,
        Back,
        MainHand,
        OffHand,
        LeftRing,
        RightRing,
    }
    
    /// <summary>
    /// Central manager placed on character to handle equipping,
    /// remapping, and unequipping items in different slots. 
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/WardrobeManager")]
    public class WardrobeManager : MonoBehaviour
    {
        private Animator _animator;

        private readonly Dictionary<EquipmentSlot, GameObject> _equipmentItems = new ();
        
        public event Action<EquipmentSlot, GameObject> OnEquipmentChanged;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator.avatar == null || !_animator.isHuman)
            {
                Debug.LogWarning($"[Wardrobe Manager]: Animator on {gameObject.name} is not set up as Humanoid! Attachment system might fail.");
            }
        }

        /// <summary>
        /// Instantiates a prefab and equips it to the specified slot.
        /// Automatically handles both skinned clothing and static attachments.
        /// </summary>
        /// <param name="slot">Target equipment slot.</param>
        /// <param name="prefab">The item prefab to spawn.</param>
        /// <returns>The spawned GameObject instance, or null.</returns>
        public GameObject Equip(EquipmentSlot slot, GameObject prefab)
        {
            Unequip(slot);

            if (prefab == null) return null;
            
            GameObject spawnedInstance = Instantiate(prefab, transform,  false);
            spawnedInstance.name = $"{prefab.name}_{slot}";

            if (spawnedInstance.TryGetComponent<SkinnedMeshRemapper>(out var remapper))
            {
                remapper.Remap(_animator.transform);
            }
            else if (spawnedInstance.TryGetComponent<HumanoidAttachmentPoint>(out var attachment))
            {
                Transform boneTransform = _animator.GetBoneTransform(attachment.TargetBone);
                if (boneTransform != null)
                {
                    spawnedInstance.transform.SetParent(boneTransform, false);
                    attachment.ApplyOffsets();
                }
                else
                {
                    Debug.LogError($"[WardrobeManager] Bone {attachment.TargetBone} not found on character {gameObject.name}!");
                    Destroy(spawnedInstance);
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"[WardrobeManager] Spawned prefab {prefab.name} doesn't have a Remapper or AttachmentPoint. Parented to root.");
            }
            
            _equipmentItems[slot] =  spawnedInstance;
            OnEquipmentChanged?.Invoke(slot, spawnedInstance);
            return spawnedInstance;
        }

        /// <summary>
        /// Destroys and removes the item currently equipped in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to clear.</param>
        public void Unequip(EquipmentSlot slot)
        {
            if (!_equipmentItems.TryGetValue(slot, out var item)) return;
            if (item != null)
            {
                Destroy(item);
            }
            _equipmentItems.Remove(slot);
            OnEquipmentChanged?.Invoke(slot, null);
        }

        /// <summary>
        /// Unequips all currently equipped items
        /// </summary>
        public void UnequipAll()
        {
            List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>(_equipmentItems.Keys);
            foreach (EquipmentSlot slot in equipmentSlots)
            {
                Unequip(slot);
            }
        }

        /// <summary>
        /// Gets the active equipped GameObject in the specified slot
        /// </summary>
        public GameObject GetEquippedItem(EquipmentSlot slot)
        {
            _equipmentItems.TryGetValue(slot, out var item);
            return item;
        }
    }
}