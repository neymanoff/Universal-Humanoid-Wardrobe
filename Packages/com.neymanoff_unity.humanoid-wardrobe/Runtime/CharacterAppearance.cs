using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Optional component providing independent Hairstyle, Beard, and per-instance Color customization,
    /// plus declarative multi-source visibility suppression.
    /// Can operate standalone on any humanoid <see cref="Animator"/> or alongside <see cref="WardrobeManager"/>
    /// and <see cref="CharacterBodyScale"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/Character Appearance")]
    public class CharacterAppearance : MonoBehaviour, ICharacterAppearance
    {
        /// <summary>
        /// Internal restriction source key used for automatic visibility suppression from equipped <see cref="WardrobeItemSO"/> items.
        /// </summary>
        public const string EquipmentRestrictionSourceId = "__WardrobeEquipment__";

        [Header("Catalog & Character References")]
        [Tooltip("Catalog containing available hairstyle options, beard options, and color presets.")]
        [SerializeField]
        private CosmeticCatalogSO catalog;

        [Tooltip("Target character Animator. Auto-wired from the same GameObject if left empty.")]
        [SerializeField]
        private Animator targetAnimator;

        [Tooltip("Optional WardrobeManager on the same character. When present, equipped items with HiddenCosmetics automatically suppress hair/beard visibility.")]
        [SerializeField]
        private WardrobeManager wardrobeManager;

        [Tooltip("Optional CharacterBodyScale on the same character for adaptive profile prefab overrides, socket offsets, and morph synchronization.")]
        [SerializeField]
        private CharacterBodyScale bodyScale;

        [Header("Default Appearance (Optional)")]
        [Tooltip("Initial hairstyle applied on Start if no appearance has been set programmatically.")]
        [SerializeField]
        private CosmeticOptionSO defaultHairOption;

        [Tooltip("Initial beard applied on Start if no appearance has been set programmatically.")]
        [SerializeField]
        private CosmeticOptionSO defaultBeardOption;

        [Header("Color Configuration")]
        [Tooltip("When enabled, changing hair color also updates beard color (and vice versa).")]
        [SerializeField]
        private bool linkHairAndBeardColor = true;

        [Tooltip("Current hair tint color applied via per-instance MaterialPropertyBlock.")]
        [SerializeField]
        private Color hairColor = new(0.28f, 0.18f, 0.10f, 1f);

        [Tooltip("Current beard tint color applied via per-instance MaterialPropertyBlock.")]
        [SerializeField]
        private Color beardColor = new(0.28f, 0.18f, 0.10f, 1f);

        [Tooltip("Fallback shader color property names used when a CosmeticOptionSO does not specify custom properties.")]
        [SerializeField]
        private List<string> fallbackColorProperties = new() { "_BaseColor", "_Color" };

        private CosmeticOptionSO _currentHairOption;
        private CosmeticOptionSO _currentBeardOption;
        private GameObject _currentHairInstance;
        private GameObject _currentBeardInstance;

        private readonly Dictionary<string, CosmeticVisibilityMask> _visibilityRestrictions =
            new(StringComparer.Ordinal);

        private MaterialPropertyBlock _propertyBlock;
        private bool _initializedReferences;
        private bool _subscribedToWardrobe;
        private bool _appearanceModifiedBeforeStart;

        /// <summary>
        /// Assigned cosmetic option catalog.
        /// </summary>
        public CosmeticCatalogSO Catalog
        {
            get => catalog;
            set => catalog = value;
        }

        /// <inheritdoc />
        public string CurrentHairId => _currentHairOption != null ? _currentHairOption.OptionId : string.Empty;

        /// <inheritdoc />
        public string CurrentBeardId => _currentBeardOption != null ? _currentBeardOption.OptionId : string.Empty;

        /// <inheritdoc />
        public CosmeticOptionSO CurrentHairOption => _currentHairOption;

        /// <inheritdoc />
        public CosmeticOptionSO CurrentBeardOption => _currentBeardOption;

        /// <inheritdoc />
        public GameObject CurrentHairInstance => _currentHairInstance;

        /// <inheritdoc />
        public GameObject CurrentBeardInstance => _currentBeardInstance;

        /// <inheritdoc />
        public Color HairColor => hairColor;

        /// <inheritdoc />
        public Color BeardColor => beardColor;

        /// <inheritdoc />
        public bool LinkHairAndBeardColor
        {
            get => linkHairAndBeardColor;
            set
            {
                if (linkHairAndBeardColor == value)
                {
                    return;
                }

                linkHairAndBeardColor = value;
                if (linkHairAndBeardColor)
                {
                    beardColor = hairColor;
                    ApplyColorToInstance(_currentBeardInstance, _currentBeardOption, beardColor);
                }

                OnColorsChanged?.Invoke(hairColor, beardColor, linkHairAndBeardColor);
                OnAppearanceChanged?.Invoke(GetAppearanceData());
            }
        }

        /// <inheritdoc />
        public bool IsHairVisible => (ActiveVisibilityRestrictions & CosmeticVisibilityMask.Hair) == 0;

        /// <inheritdoc />
        public bool IsBeardVisible => (ActiveVisibilityRestrictions & CosmeticVisibilityMask.Beard) == 0;

        /// <inheritdoc />
        public CosmeticVisibilityMask ActiveVisibilityRestrictions { get; private set; } = CosmeticVisibilityMask.None;

        /// <inheritdoc />
        public event Action<CosmeticCategory, CosmeticOptionSO, GameObject> OnCosmeticChanged;

        /// <inheritdoc />
        public event Action<Color, Color, bool> OnColorsChanged;

        /// <inheritdoc />
        public event Action<CosmeticVisibilityMask> OnVisibilityRestrictionsChanged;

        /// <inheritdoc />
        public event Action<CharacterAppearanceData> OnAppearanceChanged;

        private void Reset()
        {
            AutoWireReferences();
        }

        private void OnValidate()
        {
            AutoWireReferences();
            if (linkHairAndBeardColor)
            {
                beardColor = hairColor;
            }

            if (fallbackColorProperties == null || fallbackColorProperties.Count == 0)
            {
                fallbackColorProperties = new List<string> { "_BaseColor", "_Color" };
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            SubscribeToWardrobe();
            SyncEquipmentVisibilityRestrictions();
        }

        private void Start()
        {
            EnsureInitialized();
            SyncEquipmentVisibilityRestrictions();

            if (!_appearanceModifiedBeforeStart)
            {
                if (defaultHairOption != null && _currentHairOption == null)
                {
                    SetHairInternal(defaultHairOption, suppressAppearanceEvent: true);
                }

                if (defaultBeardOption != null && _currentBeardOption == null)
                {
                    SetBeardInternal(defaultBeardOption, suppressAppearanceEvent: true);
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromWardrobe();
        }

        private void OnDestroy()
        {
            UnsubscribeFromWardrobe();
        }

        /// <summary>
        /// Explicitly binds or swaps the <see cref="WardrobeManager"/> reference at runtime
        /// and synchronizes equipment-driven visibility restrictions.
        /// </summary>
        public void SetWardrobeManager(WardrobeManager manager)
        {
            if (wardrobeManager == manager)
            {
                return;
            }

            UnsubscribeFromWardrobe();
            wardrobeManager = manager;
            SubscribeToWardrobe();
            SyncEquipmentVisibilityRestrictions();
        }

        /// <summary>
        /// Explicitly binds or swaps the <see cref="CharacterBodyScale"/> reference at runtime
        /// and updates profile prefab variants, socket offsets, and morph weights on active cosmetics.
        /// </summary>
        public void SetBodyScale(CharacterBodyScale scaleComponent)
        {
            bodyScale = scaleComponent;
            RefreshBodyProfileAdaptation();
        }

        /// <summary>
        /// Re-evaluates active hair and beard instances against the current <see cref="CharacterBodyScale"/> profile
        /// (re-spawning profile prefab overrides if needed, updating socket offsets, and syncing morphs).
        /// </summary>
        public void RefreshBodyProfileAdaptation()
        {
            EnsureInitialized();

            if (_currentHairOption != null)
            {
                SetHairInternal(_currentHairOption, suppressAppearanceEvent: true);
            }

            if (_currentBeardOption != null)
            {
                SetBeardInternal(_currentBeardOption, suppressAppearanceEvent: true);
            }
        }

        /// <inheritdoc />
        public bool SetHair(string optionId)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            if (string.IsNullOrEmpty(optionId))
            {
                ClearHair();
                return true;
            }

            if (catalog == null)
            {
                Debug.LogWarning($"[CharacterAppearance] Cannot resolve hair option '{optionId}' on {gameObject.name}: Catalog is not assigned.");
                return false;
            }

            CosmeticOptionSO option = catalog.FindHairOption(optionId);
            if (option == null)
            {
                Debug.LogWarning($"[CharacterAppearance] Hair option '{optionId}' was not found in catalog '{catalog.name}'.");
                return false;
            }

            return SetHair(option);
        }

        /// <inheritdoc />
        public bool SetHair(CosmeticOptionSO option)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;
            return SetHairInternal(option, suppressAppearanceEvent: false);
        }

        /// <inheritdoc />
        public void ClearHair()
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;
            SetHairInternal(null, suppressAppearanceEvent: false);
        }

        /// <inheritdoc />
        public bool SetBeard(string optionId)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            if (string.IsNullOrEmpty(optionId))
            {
                ClearBeard();
                return true;
            }

            if (catalog == null)
            {
                Debug.LogWarning($"[CharacterAppearance] Cannot resolve beard option '{optionId}' on {gameObject.name}: Catalog is not assigned.");
                return false;
            }

            CosmeticOptionSO option = catalog.FindBeardOption(optionId);
            if (option == null)
            {
                Debug.LogWarning($"[CharacterAppearance] Beard option '{optionId}' was not found in catalog '{catalog.name}'.");
                return false;
            }

            return SetBeard(option);
        }

        /// <inheritdoc />
        public bool SetBeard(CosmeticOptionSO option)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;
            return SetBeardInternal(option, suppressAppearanceEvent: false);
        }

        /// <inheritdoc />
        public void ClearBeard()
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;
            SetBeardInternal(null, suppressAppearanceEvent: false);
        }

        /// <inheritdoc />
        public void ClearAllCosmetics()
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            SetHairInternal(null, suppressAppearanceEvent: true);
            SetBeardInternal(null, suppressAppearanceEvent: true);
            OnAppearanceChanged?.Invoke(GetAppearanceData());
        }

        /// <inheritdoc />
        public void SetHairColor(Color color)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            hairColor = color;
            ApplyColorToInstance(_currentHairInstance, _currentHairOption, hairColor);

            if (linkHairAndBeardColor)
            {
                beardColor = color;
                ApplyColorToInstance(_currentBeardInstance, _currentBeardOption, beardColor);
            }

            OnColorsChanged?.Invoke(hairColor, beardColor, linkHairAndBeardColor);
            OnAppearanceChanged?.Invoke(GetAppearanceData());
        }

        /// <inheritdoc />
        public void SetBeardColor(Color color)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            beardColor = color;
            ApplyColorToInstance(_currentBeardInstance, _currentBeardOption, beardColor);

            if (linkHairAndBeardColor)
            {
                hairColor = color;
                ApplyColorToInstance(_currentHairInstance, _currentHairOption, hairColor);
            }

            OnColorsChanged?.Invoke(hairColor, beardColor, linkHairAndBeardColor);
            OnAppearanceChanged?.Invoke(GetAppearanceData());
        }

        /// <inheritdoc />
        public void SetColors(Color newHairColor, Color newBeardColor, bool linkColors = false)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            linkHairAndBeardColor = linkColors;
            hairColor = newHairColor;
            beardColor = linkColors ? newHairColor : newBeardColor;

            ApplyColorToInstance(_currentHairInstance, _currentHairOption, hairColor);
            ApplyColorToInstance(_currentBeardInstance, _currentBeardOption, beardColor);

            OnColorsChanged?.Invoke(hairColor, beardColor, linkHairAndBeardColor);
            OnAppearanceChanged?.Invoke(GetAppearanceData());
        }

        /// <inheritdoc />
        public void SetVisibilityRestriction(string sourceId, CosmeticVisibilityMask hiddenMask)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                Debug.LogWarning("[CharacterAppearance] Cannot set visibility restriction with a null or empty sourceId.");
                return;
            }

            if (hiddenMask == CosmeticVisibilityMask.None)
            {
                if (_visibilityRestrictions.Remove(sourceId))
                {
                    RecalculateVisibility();
                }
                return;
            }

            _visibilityRestrictions[sourceId] = hiddenMask;
            RecalculateVisibility();
        }

        /// <inheritdoc />
        public bool RemoveVisibilityRestriction(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                return false;
            }

            bool removed = _visibilityRestrictions.Remove(sourceId);
            if (removed)
            {
                RecalculateVisibility();
            }

            return removed;
        }

        /// <inheritdoc />
        public void ClearExternalVisibilityRestrictions()
        {
            if (_visibilityRestrictions.Count == 0)
            {
                return;
            }

            bool hadEquipmentRestriction = _visibilityRestrictions.TryGetValue(
                EquipmentRestrictionSourceId,
                out CosmeticVisibilityMask equipmentMask);

            _visibilityRestrictions.Clear();

            if (hadEquipmentRestriction && equipmentMask != CosmeticVisibilityMask.None)
            {
                _visibilityRestrictions[EquipmentRestrictionSourceId] = equipmentMask;
            }

            RecalculateVisibility();
        }

        /// <inheritdoc />
        public CharacterAppearanceData GetAppearanceData()
        {
            return new CharacterAppearanceData(
                CurrentHairId,
                CurrentBeardId,
                hairColor,
                beardColor,
                linkHairAndBeardColor);
        }

        /// <inheritdoc />
        public AppearanceApplyResult ApplyAppearanceData(
            CharacterAppearanceData data,
            Func<string, CosmeticOptionSO> customResolver = null)
        {
            EnsureInitialized();
            _appearanceModifiedBeforeStart = true;

            if (data == null)
            {
                SetHairInternal(null, suppressAppearanceEvent: true);
                SetBeardInternal(null, suppressAppearanceEvent: true);
                OnAppearanceChanged?.Invoke(GetAppearanceData());
                return new AppearanceApplyResult(false, string.Empty, false, string.Empty);
            }

            // 1. Restore colors and linking first so newly spawned meshes immediately receive the saved tint
            linkHairAndBeardColor = data.linkColors;
            hairColor = data.hairColor;
            beardColor = data.linkColors ? data.hairColor : data.beardColor;

            bool hairMissing = false;
            string missingHairId = string.Empty;
            bool beardMissing = false;
            string missingBeardId = string.Empty;

            // 2. Resolve Hair option
            if (string.IsNullOrEmpty(data.hairOptionId))
            {
                SetHairInternal(null, suppressAppearanceEvent: true);
            }
            else
            {
                CosmeticOptionSO resolvedHair = ResolveOption(data.hairOptionId, CosmeticCategory.Hair, customResolver);
                if (resolvedHair != null && resolvedHair.Category == CosmeticCategory.Hair)
                {
                    SetHairInternal(resolvedHair, suppressAppearanceEvent: true);
                }
                else
                {
                    hairMissing = true;
                    missingHairId = data.hairOptionId;
                    Debug.LogWarning(
                        $"[CharacterAppearance] Saved hair option '{data.hairOptionId}' could not be resolved on {gameObject.name}. Falling back to 'None'.");
                    SetHairInternal(null, suppressAppearanceEvent: true);
                }
            }

            // 3. Resolve Beard option
            if (string.IsNullOrEmpty(data.beardOptionId))
            {
                SetBeardInternal(null, suppressAppearanceEvent: true);
            }
            else
            {
                CosmeticOptionSO resolvedBeard = ResolveOption(data.beardOptionId, CosmeticCategory.Beard, customResolver);
                if (resolvedBeard != null && resolvedBeard.Category == CosmeticCategory.Beard)
                {
                    SetBeardInternal(resolvedBeard, suppressAppearanceEvent: true);
                }
                else
                {
                    beardMissing = true;
                    missingBeardId = data.beardOptionId;
                    Debug.LogWarning(
                        $"[CharacterAppearance] Saved beard option '{data.beardOptionId}' could not be resolved on {gameObject.name}. Falling back to 'None'.");
                    SetBeardInternal(null, suppressAppearanceEvent: true);
                }
            }

            // 4. Ensure colors are applied even if the same option remained active
            ApplyColorToInstance(_currentHairInstance, _currentHairOption, hairColor);
            ApplyColorToInstance(_currentBeardInstance, _currentBeardOption, beardColor);

            OnColorsChanged?.Invoke(hairColor, beardColor, linkHairAndBeardColor);
            OnAppearanceChanged?.Invoke(GetAppearanceData());

            return new AppearanceApplyResult(hairMissing, missingHairId, beardMissing, missingBeardId);
        }

        /// <summary>
        /// Reads back the per-instance color stored in a <see cref="Renderer"/>'s <see cref="MaterialPropertyBlock"/>
        /// for verification or adapter inspection.
        /// </summary>
        public static bool TryGetAppliedRendererColor(
            Renderer renderer,
            out Color appliedColor,
            string shaderProperty = "_BaseColor",
            int materialIndex = -1)
        {
            appliedColor = Color.clear;
            if (renderer == null || string.IsNullOrEmpty(shaderProperty))
            {
                return false;
            }

            MaterialPropertyBlock block = new();
            if (materialIndex >= 0)
            {
                renderer.GetPropertyBlock(block, materialIndex);
            }
            else
            {
                renderer.GetPropertyBlock(block);
            }

            if (block.isEmpty)
            {
                return false;
            }

            appliedColor = block.GetColor(shaderProperty);
            return true;
        }

        private CosmeticOptionSO ResolveOption(
            string optionId,
            CosmeticCategory category,
            Func<string, CosmeticOptionSO> customResolver)
        {
            if (customResolver != null)
            {
                CosmeticOptionSO resolved = customResolver(optionId);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            if (catalog != null)
            {
                return catalog.FindOption(optionId, category);
            }

            return null;
        }

        private bool SetHairInternal(CosmeticOptionSO option, bool suppressAppearanceEvent)
        {
            if (option != null && option.Category != CosmeticCategory.Hair)
            {
                Debug.LogWarning(
                    $"[CharacterAppearance] Cannot assign option '{option.OptionId}' ({option.Category}) as Hair on {gameObject.name}.");
                return false;
            }

            if (_currentHairInstance != null)
            {
                DestroyInstance(_currentHairInstance);
                _currentHairInstance = null;
            }

            _currentHairOption = option;

            if (option != null)
            {
                _currentHairInstance = SpawnAndBindCosmetic(option);
                if (_currentHairInstance != null)
                {
                    ApplyColorToInstance(_currentHairInstance, option, hairColor);
                    _currentHairInstance.SetActive(IsHairVisible);
                }
            }

            OnCosmeticChanged?.Invoke(CosmeticCategory.Hair, _currentHairOption, _currentHairInstance);
            if (!suppressAppearanceEvent)
            {
                OnAppearanceChanged?.Invoke(GetAppearanceData());
            }

            return true;
        }

        private bool SetBeardInternal(CosmeticOptionSO option, bool suppressAppearanceEvent)
        {
            if (option != null && option.Category != CosmeticCategory.Beard)
            {
                Debug.LogWarning(
                    $"[CharacterAppearance] Cannot assign option '{option.OptionId}' ({option.Category}) as Beard on {gameObject.name}.");
                return false;
            }

            if (_currentBeardInstance != null)
            {
                DestroyInstance(_currentBeardInstance);
                _currentBeardInstance = null;
            }

            _currentBeardOption = option;

            if (option != null)
            {
                _currentBeardInstance = SpawnAndBindCosmetic(option);
                if (_currentBeardInstance != null)
                {
                    ApplyColorToInstance(_currentBeardInstance, option, beardColor);
                    _currentBeardInstance.SetActive(IsBeardVisible);
                }
            }

            OnCosmeticChanged?.Invoke(CosmeticCategory.Beard, _currentBeardOption, _currentBeardInstance);
            if (!suppressAppearanceEvent)
            {
                OnAppearanceChanged?.Invoke(GetAppearanceData());
            }

            return true;
        }

        private GameObject SpawnAndBindCosmetic(CosmeticOptionSO option)
        {
            string profileId = bodyScale != null ? bodyScale.EffectiveProfileId : null;
            GameObject prefabToSpawn = option.GetPrefabForProfile(profileId);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[CharacterAppearance] CosmeticOptionSO '{option.OptionId}' has no prefab assigned.");
                return null;
            }

            GameObject spawned = Instantiate(prefabToSpawn, transform, false);
            spawned.name = $"{prefabToSpawn.name}_{option.Category}";

            BindCosmeticToSkeleton(spawned, option);
            return spawned;
        }

        private void BindCosmeticToSkeleton(GameObject spawnedInstance, CosmeticOptionSO option)
        {
            Transform rigRoot = targetAnimator != null ? targetAnimator.transform : transform;

            // 1. Deforming SkinnedMeshRenderer rigged to skeleton
            if (spawnedInstance.TryGetComponent<SkinnedMeshRemapper>(out var remapper))
            {
                remapper.Remap(rigRoot);
                SynchronizeMorphsOnInstance(spawnedInstance);
                return;
            }

            // 2. Socket attachment via HumanoidAttachmentPoint
            if (spawnedInstance.TryGetComponent<HumanoidAttachmentPoint>(out var attachment))
            {
                HumanBodyBones targetBone = attachment.UseCustomBone
                    ? attachment.TargetBone
                    : option.DefaultAttachmentBone;

                Transform boneTransform = ResolveBoneTransform(targetBone);
                if (boneTransform != null)
                {
                    spawnedInstance.transform.SetParent(boneTransform, false);
                    attachment.ApplyOffsets(false, bodyScale, EquipmentSlot.Head);
                }
                else
                {
                    spawnedInstance.transform.SetParent(rigRoot, false);
                    attachment.ApplyOffsets(false, bodyScale, EquipmentSlot.Head);
                }

                return;
            }

            // 3. Direct fallback to DefaultAttachmentBone (or character root if rig has no humanoid bones)
            Transform fallbackBone = ResolveBoneTransform(option.DefaultAttachmentBone);
            if (fallbackBone != null)
            {
                spawnedInstance.transform.SetParent(fallbackBone, false);
            }
            else
            {
                spawnedInstance.transform.SetParent(rigRoot, false);
            }
        }

        private Transform ResolveBoneTransform(HumanBodyBones bone)
        {
            if (targetAnimator != null && targetAnimator.isHuman && targetAnimator.avatar != null)
            {
                Transform humanBone = targetAnimator.GetBoneTransform(bone);
                if (humanBone != null)
                {
                    return humanBone;
                }
            }

            Transform searchRoot = targetAnimator != null ? targetAnimator.transform : transform;
            return HumanoidAttachmentPoint.ResolveBoneFallback(searchRoot, bone);
        }

        private void SynchronizeMorphsOnInstance(GameObject instance)
        {
            if (wardrobeManager != null)
            {
                wardrobeManager.SynchronizeBodyMorphs(instance);
                return;
            }

            if (bodyScale == null || instance == null)
            {
                return;
            }

            Dictionary<string, float> effectiveWeights = bodyScale.GetEffectiveMorphWeights();
            if (effectiveWeights.Count == 0)
            {
                return;
            }

            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                SkinnedMeshRenderer rend = renderers[r];
                if (rend == null || rend.sharedMesh == null)
                {
                    continue;
                }

                Mesh mesh = rend.sharedMesh;
                for (int b = 0; b < mesh.blendShapeCount; b++)
                {
                    string shapeName = mesh.GetBlendShapeName(b);
                    if (effectiveWeights.TryGetValue(shapeName, out float weight))
                    {
                        rend.SetBlendShapeWeight(b, weight);
                    }
                }
            }
        }

        private void ApplyColorToInstance(GameObject instance, CosmeticOptionSO option, Color color)
        {
            if (instance == null || option == null || !option.SupportsColorTinting)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();

            IReadOnlyList<string> shaderProps = (option.ColorShaderProperties != null && option.ColorShaderProperties.Count > 0)
                ? option.ColorShaderProperties
                : fallbackColorProperties;

            if (shaderProps == null || shaderProps.Count == 0)
            {
                return;
            }

            IReadOnlyList<int> matIndices = option.TargetMaterialIndices;
            bool filterMaterialIndices = matIndices != null && matIndices.Count > 0;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer rend = renderers[r];
                if (!option.ShouldTintRenderer(rend))
                {
                    continue;
                }

                if (filterMaterialIndices)
                {
                    int sharedMatCount = rend.sharedMaterials != null ? rend.sharedMaterials.Length : 0;
                    for (int i = 0; i < matIndices.Count; i++)
                    {
                        int matIndex = matIndices[i];
                        if (matIndex < 0 || (sharedMatCount > 0 && matIndex >= sharedMatCount))
                        {
                            continue;
                        }

                        rend.GetPropertyBlock(_propertyBlock, matIndex);
                        for (int p = 0; p < shaderProps.Count; p++)
                        {
                            string propName = shaderProps[p];
                            if (!string.IsNullOrEmpty(propName))
                            {
                                _propertyBlock.SetColor(propName, color);
                            }
                        }

                        rend.SetPropertyBlock(_propertyBlock, matIndex);
                    }
                }
                else
                {
                    rend.GetPropertyBlock(_propertyBlock);
                    for (int p = 0; p < shaderProps.Count; p++)
                    {
                        string propName = shaderProps[p];
                        if (!string.IsNullOrEmpty(propName))
                        {
                            _propertyBlock.SetColor(propName, color);
                        }
                    }

                    rend.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        /// <summary>
        /// Synchronizes the internal equipment visibility restriction source from all items
        /// currently equipped on <see cref="WardrobeManager"/>.
        /// </summary>
        public void SyncEquipmentVisibilityRestrictions()
        {
            CosmeticVisibilityMask combinedEquipmentMask = CosmeticVisibilityMask.None;

            if (wardrobeManager != null && wardrobeManager.EquippedInstances != null)
            {
                IReadOnlyList<EquippedItemInstance> instances = wardrobeManager.EquippedInstances;
                for (int i = 0; i < instances.Count; i++)
                {
                    WardrobeItemSO itemSO = instances[i]?.ItemData;
                    if (itemSO != null)
                    {
                        combinedEquipmentMask |= itemSO.HiddenCosmetics;
                    }
                }
            }

            if (combinedEquipmentMask == CosmeticVisibilityMask.None)
            {
                if (_visibilityRestrictions.Remove(EquipmentRestrictionSourceId))
                {
                    RecalculateVisibility();
                }
            }
            else
            {
                _visibilityRestrictions[EquipmentRestrictionSourceId] = combinedEquipmentMask;
                RecalculateVisibility();
            }
        }

        private void RecalculateVisibility()
        {
            CosmeticVisibilityMask combined = CosmeticVisibilityMask.None;
            foreach (var pair in _visibilityRestrictions)
            {
                combined |= pair.Value;
            }

            bool maskChanged = ActiveVisibilityRestrictions != combined;
            ActiveVisibilityRestrictions = combined;

            bool showHair = (combined & CosmeticVisibilityMask.Hair) == 0;
            bool showBeard = (combined & CosmeticVisibilityMask.Beard) == 0;

            if (_currentHairInstance != null && _currentHairInstance.activeSelf != showHair)
            {
                _currentHairInstance.SetActive(showHair);
                if (showHair)
                {
                    ApplyColorToInstance(_currentHairInstance, _currentHairOption, hairColor);
                }
            }

            if (_currentBeardInstance != null && _currentBeardInstance.activeSelf != showBeard)
            {
                _currentBeardInstance.SetActive(showBeard);
                if (showBeard)
                {
                    ApplyColorToInstance(_currentBeardInstance, _currentBeardOption, beardColor);
                }
            }

            if (maskChanged)
            {
                OnVisibilityRestrictionsChanged?.Invoke(ActiveVisibilityRestrictions);
            }
        }

        private void EnsureInitialized()
        {
            if (_initializedReferences)
            {
                return;
            }

            AutoWireReferences();
            if (linkHairAndBeardColor)
            {
                beardColor = hairColor;
            }

            _initializedReferences = true;
            SubscribeToWardrobe();
        }

        private void AutoWireReferences()
        {
            if (targetAnimator == null)
            {
                targetAnimator = GetComponent<Animator>();
            }

            if (wardrobeManager == null)
            {
                wardrobeManager = GetComponent<WardrobeManager>();
            }

            if (bodyScale == null)
            {
                bodyScale = GetComponent<CharacterBodyScale>();
            }
        }

        private void SubscribeToWardrobe()
        {
            if (_subscribedToWardrobe || wardrobeManager == null)
            {
                return;
            }

            wardrobeManager.OnEquipmentChanged += HandleWardrobeEquipmentChanged;
            wardrobeManager.OnLoadoutChanged += HandleWardrobeLoadoutChanged;
            _subscribedToWardrobe = true;
        }

        private void UnsubscribeFromWardrobe()
        {
            if (!_subscribedToWardrobe || wardrobeManager == null)
            {
                _subscribedToWardrobe = false;
                return;
            }

            wardrobeManager.OnEquipmentChanged -= HandleWardrobeEquipmentChanged;
            wardrobeManager.OnLoadoutChanged -= HandleWardrobeLoadoutChanged;
            _subscribedToWardrobe = false;
        }

        private void HandleWardrobeEquipmentChanged(EquipmentSlot slot, GameObject itemObject)
        {
            SyncEquipmentVisibilityRestrictions();
        }

        private void HandleWardrobeLoadoutChanged(WardrobeLoadout loadout)
        {
            SyncEquipmentVisibilityRestrictions();
        }

        private static void DestroyInstance(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}
