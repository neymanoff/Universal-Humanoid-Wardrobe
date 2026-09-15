using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Supported equipment slots for the wardrobe system.
    /// </summary>
    public enum EquipmentSlot
    {
        Head = 0,
        Shoulders = 1,
        Chest = 2,
        Hands = 3,
        Legs = 4,
        Feet = 5,
        Neck = 6,
        Back = 7,
        MainHand = 8,
        OffHand = 9,
        LeftRing = 10,
        RightRing = 11,
    }

    /// <summary>
    /// Central manager placed on a Humanoid character to handle equipping,
    /// remapping, socket attachment, and equipment occupancy state.
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

        [System.Serializable]
        public struct ModularBodyPart
        {
            [Tooltip("Anatomical body region represented by this sub-mesh renderer.")]
            public BodyPartMask part;

            [Tooltip("The Renderer (SkinnedMeshRenderer or MeshRenderer) that will be enabled/disabled.")]
            public Renderer renderer;
        }

        [Header("Default Loadout")]
        [Tooltip("Items equipped automatically when the game starts.")]
        public List<DefaultEquipment> defaultLoadout = new();

        [Header("Body Masking & Anti-Clipping")]
        [Tooltip("Optional modular sub-meshes of the base character body that will be automatically hidden when covered by apparel.")]
        [SerializeField] private List<ModularBodyPart> modularBodyParts = new();

        [Tooltip("Optional SkinnedMeshRenderers on the base character containing shrink/morph blendshapes (e.g. Shrink_Chest, Shrink_Torso).")]
        [SerializeField] private List<SkinnedMeshRenderer> morphTargets = new();

        private Animator _animator;
        private readonly Dictionary<EquipmentSlot, EquippedItemInstance> _slotToInstance = new();
        private readonly List<EquippedItemInstance> _equippedInstances = new();

        // Modern typed events
        public event Action<EquipmentSlot, WardrobeItemSO, GameObject> OnItemEquipped;
        public event Action<EquipmentSlot, WardrobeItemSO> OnItemUnequipped;
        public event Action<WardrobeLoadout> OnLoadoutChanged;

        // Legacy compatibility event
        public event Action<EquipmentSlot, GameObject> OnEquipmentChanged;

        public Animator CharacterAnimator => _animator;
        public IReadOnlyList<EquippedItemInstance> EquippedInstances => _equippedInstances;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator == null || _animator.avatar == null || !_animator.isHuman)
            {
                Debug.LogWarning(
                    $"[WardrobeManager]: Animator on {gameObject.name} is not set up as Humanoid! Attachment system might fail.");
            }
        }

        private void Start()
        {
            foreach (var item in defaultLoadout)
            {
                if (item.prefab != null)
                {
                    EquipPrefab(item.slot, item.prefab);
                }
            }

            UpdateBodyMasksAndBlendshapes();
        }

        /// <summary>
        /// Equips a WardrobeItemSO into a target slot with rule validation and multi-slot occupancy.
        /// </summary>
        public EquipResult Equip(WardrobeItemSO itemSO, EquipmentSlot requestedSlot)
        {
            EquipResultStatus status = EquipmentRuleResolver.ValidateEquipRequest(itemSO, requestedSlot);
            if (status != EquipResultStatus.Success)
            {
                return EquipResult.Failed(status, $"Cannot equip {itemSO?.ItemName ?? "null"} into {requestedSlot} ({status}).");
            }

            // Resolve and clear conflicting slots (e.g. 2H weapon clears OffHand)
            List<EquipmentSlot> conflictingSlots = EquipmentRuleResolver.GetConflictingSlots(itemSO, requestedSlot, _slotToInstance);
            for (int i = 0; i < conflictingSlots.Count; i++)
            {
                UnequipInternal(conflictingSlots[i], suppressLoadoutEvent: true);
            }

            // Instantiate visual prefab
            GameObject spawnedInstance = Instantiate(itemSO.ItemPrefab, transform, false);
            spawnedInstance.name = $"{itemSO.ItemPrefab.name}_{requestedSlot}";

            // Bind to skeleton
            bool bindSuccess = BindSpawnedInstance(spawnedInstance, requestedSlot);
            if (!bindSuccess)
            {
                DestroyObject(spawnedInstance);
                return EquipResult.Failed(EquipResultStatus.MissingBone, $"Failed to bind {itemSO.ItemName} to humanoid skeleton.");
            }

            // Register multi-slot occupancy
            List<EquipmentSlot> occupiedSlots = itemSO.GetOccupiedSlots(requestedSlot);
            EquippedItemInstance itemInstance = new(itemSO, spawnedInstance, requestedSlot, occupiedSlots);

            for (int i = 0; i < occupiedSlots.Count; i++)
            {
                _slotToInstance[occupiedSlots[i]] = itemInstance;
            }
            _equippedInstances.Add(itemInstance);

            UpdateBodyMasksAndBlendshapes();

            // Fire events
            OnItemEquipped?.Invoke(requestedSlot, itemSO, spawnedInstance);
            OnEquipmentChanged?.Invoke(requestedSlot, spawnedInstance);
            OnLoadoutChanged?.Invoke(GetCurrentLoadout());

            return EquipResult.Succeeded(itemInstance);
        }

        /// <summary>
        /// Legacy overload for equipping a WardrobeItemSO.
        /// </summary>
        public GameObject EquipItemSO(WardrobeItemSO itemSO, EquipmentSlot requestedSlot)
        {
            EquipResult result = Equip(itemSO, requestedSlot);
            return result.Instance?.InstanceObject;
        }

        /// <summary>
        /// Equips a raw prefab without SO metadata.
        /// </summary>
        public GameObject Equip(EquipmentSlot slot, GameObject prefab)
        {
            return EquipPrefab(slot, prefab);
        }

        private GameObject EquipPrefab(EquipmentSlot slot, GameObject prefab)
        {
            Unequip(slot);
            if (prefab == null) return null;

            GameObject spawnedInstance = Instantiate(prefab, transform, false);
            spawnedInstance.name = $"{prefab.name}_{slot}";

            if (!BindSpawnedInstance(spawnedInstance, slot))
            {
                DestroyObject(spawnedInstance);
                return null;
            }

            EquippedItemInstance instance = new(null, spawnedInstance, slot, new[] { slot });
            _slotToInstance[slot] = instance;
            _equippedInstances.Add(instance);

            OnEquipmentChanged?.Invoke(slot, spawnedInstance);
            OnLoadoutChanged?.Invoke(GetCurrentLoadout());

            return spawnedInstance;
        }

        private bool BindSpawnedInstance(GameObject spawnedInstance, EquipmentSlot slot)
        {
            if (spawnedInstance.TryGetComponent<SkinnedMeshRemapper>(out var remapper))
            {
                remapper.Remap(_animator.transform);
                return true;
            }

            if (spawnedInstance.TryGetComponent<HumanoidAttachmentPoint>(out var attachment))
            {
                HumanBodyBones targetBone = attachment.UseCustomBone
                    ? attachment.TargetBone
                    : GetDefaultBoneForSlot(slot);

                Transform boneTransform = _animator != null ? _animator.GetBoneTransform(targetBone) : null;
                if (boneTransform != null)
                {
                    spawnedInstance.transform.SetParent(boneTransform, false);
                    bool isLeftSlot = (slot == EquipmentSlot.OffHand || slot == EquipmentSlot.LeftRing);
                    attachment.ApplyOffsets(isLeftSlot);
                    return true;
                }

                Debug.LogError($"[WardrobeManager] Bone {targetBone} for slot {slot} not found on {gameObject.name}!");
                return false;
            }

            Debug.LogWarning($"[WardrobeManager] Spawned prefab {spawnedInstance.name} has no Remapper or AttachmentPoint. Parented to root.");
            return true;
        }

        /// <summary>
        /// Unequips and destroys the item occupying the specified slot.
        /// If the item occupies multiple slots, all slots are cleared atomically.
        /// </summary>
        public bool Unequip(EquipmentSlot slot)
        {
            return UnequipInternal(slot, suppressLoadoutEvent: false);
        }

        private bool UnequipInternal(EquipmentSlot slot, bool suppressLoadoutEvent)
        {
            if (!_slotToInstance.TryGetValue(slot, out var instance) || instance == null)
            {
                return false;
            }

            WardrobeItemSO itemData = instance.ItemData;
            EquipmentSlot primarySlot = instance.PrimarySlot;
            IReadOnlyList<EquipmentSlot> occupied = instance.OccupiedSlots;

            // Remove all slot mappings for this instance
            for (int i = 0; i < occupied.Count; i++)
            {
                _slotToInstance.Remove(occupied[i]);
            }
            _equippedInstances.Remove(instance);

            // Destroy visual GameObject once
            if (instance.InstanceObject != null)
            {
                DestroyObject(instance.InstanceObject);
            }

            // Fire events
            OnItemUnequipped?.Invoke(primarySlot, itemData);

            for (int i = 0; i < occupied.Count; i++)
            {
                OnEquipmentChanged?.Invoke(occupied[i], null);
            }

            if (!suppressLoadoutEvent)
            {
                UpdateBodyMasksAndBlendshapes();
                OnLoadoutChanged?.Invoke(GetCurrentLoadout());
            }

            return true;
        }

        /// <summary>
        /// Unequips all currently equipped items.
        /// </summary>
        public void UnequipAll()
        {
            List<EquippedItemInstance> instances = new(_equippedInstances);
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                UnequipInternal(instances[i].PrimarySlot, suppressLoadoutEvent: true);
            }

            UpdateBodyMasksAndBlendshapes();
            OnLoadoutChanged?.Invoke(GetCurrentLoadout());
        }

        public bool IsSlotOccupied(EquipmentSlot slot)
        {
            return _slotToInstance.TryGetValue(slot, out var inst) && inst != null;
        }

        public GameObject GetEquippedItem(EquipmentSlot slot)
        {
            _slotToInstance.TryGetValue(slot, out var instance);
            return instance?.InstanceObject;
        }

        public WardrobeItemSO GetEquippedItemData(EquipmentSlot slot)
        {
            _slotToInstance.TryGetValue(slot, out var instance);
            return instance?.ItemData;
        }

        public EquippedItemInstance GetEquippedInstance(EquipmentSlot slot)
        {
            _slotToInstance.TryGetValue(slot, out var instance);
            return instance;
        }

        /// <summary>
        /// Captures the current equipped items as a serializable WardrobeLoadout snapshot.
        /// </summary>
        public WardrobeLoadout GetCurrentLoadout()
        {
            List<EquippedSlotEntry> entries = new();
            for (int i = 0; i < _equippedInstances.Count; i++)
            {
                EquippedItemInstance inst = _equippedInstances[i];
                if (inst.ItemData != null)
                {
                    entries.Add(new EquippedSlotEntry(inst.PrimarySlot, inst.ItemData.ItemId));
                }
            }
            return new WardrobeLoadout(entries);
        }

        /// <summary>
        /// Restores an equipment loadout using a resolver function to retrieve WardrobeItemSO by ItemId.
        /// </summary>
        public void ApplyLoadout(WardrobeLoadout loadout, Func<string, WardrobeItemSO> itemResolver)
        {
            if (loadout == null || itemResolver == null) return;

            UnequipAll();

            for (int i = 0; i < loadout.entries.Count; i++)
            {
                EquippedSlotEntry entry = loadout.entries[i];
                WardrobeItemSO item = itemResolver(entry.itemId);
                if (item != null)
                {
                    Equip(item, entry.slot);
                }
                else
                {
                    Debug.LogWarning($"[WardrobeManager] Could not resolve item with ID '{entry.itemId}' for slot {entry.slot}.");
                }
            }
        }

        private void DestroyObject(GameObject obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
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

        public IReadOnlyList<ModularBodyPart> ModularBodyParts => modularBodyParts;
        public IReadOnlyList<SkinnedMeshRenderer> MorphTargets => morphTargets;

        /// <summary>
        /// Re-evaluates all currently equipped items and updates modular body part visibility
        /// and character shrink blendshapes to eliminate mesh clipping.
        /// </summary>
        public void UpdateBodyMasksAndBlendshapes()
        {
            // 1. Aggregate active masks and active blendshape names across all equipped items
            BodyPartMask combinedHiddenMask = BodyPartMask.None;
            HashSet<string> activeShrinkShapes = null;

            for (int i = 0; i < _equippedInstances.Count; i++)
            {
                WardrobeItemSO itemSO = _equippedInstances[i].ItemData;
                if (itemSO == null) continue;

                combinedHiddenMask |= itemSO.HiddenBodyParts;

                if (itemSO.ShrinkBlendShapes != null && itemSO.ShrinkBlendShapes.Count > 0)
                {
                    activeShrinkShapes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int s = 0; s < itemSO.ShrinkBlendShapes.Count; s++)
                    {
                        string shapeName = itemSO.ShrinkBlendShapes[s];
                        if (!string.IsNullOrEmpty(shapeName))
                        {
                            activeShrinkShapes.Add(shapeName);
                        }
                    }
                }
            }

            // 2. Toggle modular body parts visibility
            for (int i = 0; i < modularBodyParts.Count; i++)
            {
                ModularBodyPart bodyPart = modularBodyParts[i];
                if (bodyPart.renderer == null) continue;

                bool shouldHide = (combinedHiddenMask & bodyPart.part) != 0;
                bodyPart.renderer.enabled = !shouldHide;
            }

            // 3. Update shrink blendshapes on configured morph target renderers
            for (int i = 0; i < morphTargets.Count; i++)
            {
                SkinnedMeshRenderer morphTarget = morphTargets[i];
                if (morphTarget == null || morphTarget.sharedMesh == null) continue;

                Mesh mesh = morphTarget.sharedMesh;
                int blendShapeCount = mesh.blendShapeCount;

                for (int b = 0; b < blendShapeCount; b++)
                {
                    string shapeName = mesh.GetBlendShapeName(b);
                    if (activeShrinkShapes != null && activeShrinkShapes.Contains(shapeName))
                    {
                        morphTarget.SetBlendShapeWeight(b, 100f);
                    }
                    else if (shapeName.StartsWith("Shrink_", StringComparison.OrdinalIgnoreCase))
                    {
                        morphTarget.SetBlendShapeWeight(b, 0f);
                    }
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Testing helper to configure modular body parts in-memory.
        /// </summary>
        public void ConfigureModularPartsForTest(IEnumerable<ModularBodyPart> parts)
        {
            modularBodyParts = parts != null ? new List<ModularBodyPart>(parts) : new List<ModularBodyPart>();
        }
#endif
    }
}