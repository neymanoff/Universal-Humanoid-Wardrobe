using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// UI component representing a single equipment slot on the character paper-doll.
    /// Supports item icons, default silhouettes, 2H weapon blocking tints, and mobile drag-and-drop.
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Slot Configuration")]
        [Tooltip("The equipment slot this UI element represents.")]
        [SerializeField] public EquipmentSlot slotType;
        
        [Header("UI References")]
        [Tooltip("Optional separate image for item icon. If left empty, Silhouette Image will be used for both.")]
        [SerializeField] public Image itemIconImage;
        [Tooltip("Image component displaying the neutral silhouette placeholder.")]
        [SerializeField] public Image silhouetteImage;
        
        private WardrobeManager _wardrobeManager;
        private WardrobeItemSO _currentItem;
        private Sprite _defaultSilhouetteSprite;
        private bool _isBlockedByTwoHanded = false;

        private Canvas _rootCanvas;
        private GameObject _dragProxyObject;
        private RectTransform _dragProxyRect;
        private bool _isDragging = false;
        
        public EquipmentSlot SlotType => slotType;
        public WardrobeItemSO CurrentItem => _currentItem;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            CacheImages();
        }

        private void CacheImages()
        {
            if (silhouetteImage == null)
            {
                silhouetteImage = GetComponent<Image>();
            }
            
            if (silhouetteImage != null && _defaultSilhouetteSprite == null)
            {
                _defaultSilhouetteSprite = silhouetteImage.sprite;
            }
        }

        public void Initialize(WardrobeManager manager)
        {
            _wardrobeManager = manager;
            CacheImages();

            if (_wardrobeManager != null)
            {
                _wardrobeManager.OnEquipmentChanged += HandleEquipmentChanged;
            }

            ResetToSilhouette();
        }

        private void OnDestroy()
        {
            if (_wardrobeManager != null)
            {
                _wardrobeManager.OnEquipmentChanged -= HandleEquipmentChanged;
            }
        }

        private void HandleEquipmentChanged(EquipmentSlot changedSlot, GameObject equippedObject)
        {
            if (changedSlot != slotType) return;

            if (equippedObject == null)
            {
                ResetToSilhouette();
            }
        }

        /// <summary>
        /// Updates the slot visuals with the equipped item's icon or placeholder silhouette.
        /// </summary>
        public void SetEquipmentItem(WardrobeItemSO item)
        {
            _currentItem = item;
            _isBlockedByTwoHanded = false;
            if (item != null && item.Icon != null)
            {
                SetVisualImage(item.Icon, Color.white);
            }
            else
            {
                ResetToSilhouette();
            }
        }

        /// <summary>
        /// Visually blocks this clot with a reddish tint when a 2H weapon is held in the main hand
        /// </summary>
        public void SetBlockedByTwoHanded(WardrobeItemSO twoHandedItem)
        {
            _currentItem = twoHandedItem;
            _isBlockedByTwoHanded = true;

            if (twoHandedItem != null && twoHandedItem.Icon != null)
            {
                Color blockedColor = new Color(1f, 0.45f, 0.45f, 0.65f);
                SetVisualImage(twoHandedItem.Icon, blockedColor);
            }
        }

        public void ResetToSilhouette()
        {
            _currentItem = null;
            _isBlockedByTwoHanded = false;
            SetVisualImage(_defaultSilhouetteSprite, Color.white);
        }

        private void SetVisualImage(Sprite sprite, Color color)
        {
            if (itemIconImage != null)
            {
                if (sprite != null && sprite != _defaultSilhouetteSprite)
                {
                    itemIconImage.sprite = sprite;
                    itemIconImage.color = color;
                    itemIconImage.enabled = true;
                    if (silhouetteImage != null) 
                        silhouetteImage.enabled = false;
                }
                else
                {
                    itemIconImage.enabled = false;
                    if (silhouetteImage != null)
                    {
                        silhouetteImage.sprite = _defaultSilhouetteSprite;
                        silhouetteImage.color = Color.white;
                        silhouetteImage.enabled = true;
                    }
                }
            } 
            else if (silhouetteImage != null)
            {
                silhouetteImage.enabled = true;
                silhouetteImage.sprite = sprite != null ? sprite : _defaultSilhouetteSprite;
                silhouetteImage.color = color;
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging) return;
            if (_wardrobeManager == null) return;

            if (_isBlockedByTwoHanded)
            {
                _wardrobeManager.Unequip(EquipmentSlot.MainHand);
            }
            else if (_currentItem != null)
            {
                _wardrobeManager.Unequip(slotType);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_wardrobeManager == null || eventData.pointerDrag == null) return;

            WardrobeItemSO droppedItem = null;
            EquipmentSlot? sourceSlot = null;

            // Check if dropped from an inventory item
            if (eventData.pointerDrag.TryGetComponent<DraggableItemUI>(out var draggable))
            {
                droppedItem = draggable.ItemData;
                sourceSlot = draggable.SourceSlot;
            }
            // Or dragged from another equipment slot
            else if (eventData.pointerDrag.TryGetComponent<EquipmentSlotUI>(out var sourceSlotUI))
            {
                droppedItem = sourceSlotUI.CurrentItem;
                sourceSlot = sourceSlotUI.SlotType;
            }

            if (droppedItem == null) return;

            // Validate slot compatibility
            if (!IsItemAllowedInSlot(droppedItem, slotType))
            {
                Debug.LogWarning($"[EquipmentSlotUI] Item '{droppedItem.ItemName}' cannot be equipped into slot {slotType}!");
                return;
            }

            // Dragged from another slot (e.g. MainHand <-> OffHand swapping)
            if (sourceSlot.HasValue)
            {
                if (sourceSlot.Value == slotType) return;

                WardrobeItemSO currentTargetItem = _currentItem;
                if (currentTargetItem != null && IsItemAllowedInSlot(currentTargetItem, sourceSlot.Value))
                {
                    // Swap items between source and target hands
                    _wardrobeManager.Unequip(sourceSlot.Value);
                    _wardrobeManager.Unequip(slotType);
                    _wardrobeManager.Equip(droppedItem, slotType);
                    _wardrobeManager.Equip(currentTargetItem, sourceSlot.Value);
                    return;
                }

                // Move from source slot to target slot
                _wardrobeManager.Unequip(sourceSlot.Value);
                _wardrobeManager.Equip(droppedItem, slotType);
                return;
            }

            // Dragged directly from inventory grid into this slot (Dual-Wielding support)
            _wardrobeManager.Equip(droppedItem, slotType);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentItem == null || _isBlockedByTwoHanded) return;

            _isDragging = true;
            CreateDragProxy(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _dragProxyRect == null) return;

            UpdateProxyPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            DestroyDragProxy();

            // If dragged outside any UI or dropped in the void, unequip
            if (eventData.pointerEnter == null)
            {
                _wardrobeManager.Unequip(slotType);
            }
        }

        public static bool IsItemAllowedInSlot(WardrobeItemSO item, EquipmentSlot slot)
        {
            if (item == null) return false;
            var allowed = item.AllowedSlots;
            if (allowed != null && allowed.Count > 0)
            {
                for (int i = 0; i < allowed.Count; i++)
                {
                    if (allowed[i] == slot) return true;
                }
                return false;
            }
            return item.TargetSlot == slot;
        }

        private void CreateDragProxy(PointerEventData eventData)
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }

            _dragProxyObject = new GameObject($"DragProxy_{slotType}");
            if (_rootCanvas != null)
            {
                _dragProxyObject.transform.SetParent(_rootCanvas.transform, false);
                _dragProxyObject.transform.SetAsLastSibling();
            }

            _dragProxyRect = _dragProxyObject.AddComponent<RectTransform>();
            _dragProxyRect.sizeDelta = new Vector2(72f, 72f);

            Image proxyImage = _dragProxyObject.AddComponent<Image>();
            if (_currentItem != null && _currentItem.Icon != null)
            {
                proxyImage.sprite = _currentItem.Icon;
            }
            proxyImage.color = new Color(1f, 1f, 1f, 0.85f);
            proxyImage.raycastTarget = false;

            CanvasGroup canvasGroup = _dragProxyObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            UpdateProxyPosition(eventData);
        }

        private void UpdateProxyPosition(PointerEventData eventData)
        {
            if (_dragProxyRect == null) return;

            if (_rootCanvas != null && _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _dragProxyRect.position = eventData.position;
            }
            else if (_rootCanvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                _dragProxyRect.localPosition = localPoint;
            }
            else
            {
                _dragProxyRect.position = eventData.position;
            }
        }

        private void DestroyDragProxy()
        {
            if (_dragProxyObject != null)
            {
                Destroy(_dragProxyObject);
                _dragProxyObject = null;
                _dragProxyRect = null;
            }
        }
    }
    
}