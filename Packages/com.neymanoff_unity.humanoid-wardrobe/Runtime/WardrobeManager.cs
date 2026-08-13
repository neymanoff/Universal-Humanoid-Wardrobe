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
        [System.Serializable]
        public struct DefaultEquipment
        {
            public EquipmentSlot slot;
            public GameObject prefab;
        }
        
        [Header("Default Loadout")]
        [Tooltip("Items equipped automatically when the game starts.")]
        public List<DefaultEquipment> defaultLoadout = new();
        
        private Animator _animator;
        private readonly Dictionary<EquipmentSlot, GameObject> _equipmentItems = new ();
        private readonly Dictionary<EquipmentSlot, WardrobeItemSO> _equipmentItemData = new();
        
        public event Action<EquipmentSlot, GameObject> OnEquipmentChanged;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator.avatar == null || !_animator.isHuman)
            {
                Debug.LogWarning($"[Wardrobe Manager]: Animator on {gameObject.name} is not set up as Humanoid! Attachment system might fail.");
            }
        }

        private void Start()
        {
            foreach (var item in defaultLoadout)
            {
                if (item.prefab != null)
                {
                    Equip(item.slot, item.prefab);
                }
            }
        }

        ///<summary>
        /// Equips a WardrobeItemSO into a target slot with automatic rule checking
        /// (e.g. 2H weapons auto-unequipping OffHand, rings swapping, etc.)
        /// </summary>
        public GameObject EquipItemSO(WardrobeItemSO itemSO, EquipmentSlot requestedSlot)
        {
            if (itemSO == null || itemSO.ItemPrefab == null) return null;

            if (!itemSO.CanFitInSlot(requestedSlot))
            {
                Debug.LogWarning($"[WardrobeManager] Item '{itemSO.ItemPrefab.name}' cannot be equipped into slot {requestedSlot}!");
                return null;
            }
            
            if (itemSO.Restriction == ItemSlotRestriction.TwoHanded)
            {
                Unequip(EquipmentSlot.OffHand);
            }

            if (requestedSlot == EquipmentSlot.OffHand)
            {
                if (_equipmentItemData.TryGetValue(EquipmentSlot.MainHand, out var mainItem) 
                    && mainItem != null
                    && mainItem.Restriction == ItemSlotRestriction.TwoHanded)
                {
                    Unequip(EquipmentSlot.MainHand);
                }
            }
            
            GameObject instance = Equip(requestedSlot, itemSO.ItemPrefab);
            if (instance != null)
            {
                _equipmentItemData[requestedSlot] = itemSO;
            }
            return instance;
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
                HumanBodyBones targetBone = attachment.UseCustomBone 
                    ? attachment.TargetBone 
                    : GetDefaultBoneForSlot(slot);
                
                Transform boneTransform = _animator.GetBoneTransform(targetBone);
                if (boneTransform != null)
                {
                    spawnedInstance.transform.SetParent(boneTransform, false);
                    bool isLeftSlot = (slot == EquipmentSlot.OffHand || slot == EquipmentSlot.LeftRing);
                    attachment.ApplyOffsets(isLeftSlot);
                }
                else
                {
                    Debug.LogError($"[WardrobeManager] Bone {attachment.TargetBone} for slot {slot} not found on character {gameObject.name}!");
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
            if (_equipmentItems.TryGetValue(slot, out var item))
            {
                if (item != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(item);
                    }
                    else
                    {
                        DestroyImmediate(item);
                    }
                }
                _equipmentItems.Remove(slot);
            }
            _equipmentItemData.Remove(slot);
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

        /// <summary>
        /// Gets the WardrobeItemSO data for the item equipped in the specified slot.
        /// </summary>
        public WardrobeItemSO GetEquippedItemData(EquipmentSlot slot)
        {
            _equipmentItemData.TryGetValue(slot, out var data);
            return data;
        }
        
        public static HumanBodyBones GetDefaultBoneForSlot(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => HumanBodyBones.Head,
                EquipmentSlot.Neck => HumanBodyBones.Neck,
                EquipmentSlot.Chest => HumanBodyBones.Chest,
                EquipmentSlot.Shoulders => HumanBodyBones.Chest,
                EquipmentSlot.Back => HumanBodyBones.Chest,
                EquipmentSlot.MainHand => HumanBodyBones.RightHand,
                EquipmentSlot.OffHand => HumanBodyBones.LeftHand,
                EquipmentSlot.LeftRing => HumanBodyBones.LeftRingProximal,
                EquipmentSlot.RightRing => HumanBodyBones.RightRingProximal,
                _ => HumanBodyBones.Hips
            };
        }
    }
}