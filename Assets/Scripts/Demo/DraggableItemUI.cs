using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// Component placed on inventory item representations and equipment slots to enable
    /// mobile-friendly touch and pointer drag-and-drop operations across the wardrobe UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Tooltip("The wardrobe item associated with this draggable element.")]
        [SerializeField] private WardrobeItemSO itemData;

        private EquipmentSlot? _sourceSlot = null;
        private Canvas _rootCanvas;
        private GameObject _dragProxyObject;
        private RectTransform _dragProxyRect;
        private bool _isDragging = false;

        public event Action<WardrobeItemSO> OnItemClicked;

        public WardrobeItemSO ItemData
        {
            get => itemData;
            set => itemData = value;
        }

        public EquipmentSlot? SourceSlot
        {
            get => _sourceSlot;
            set => _sourceSlot = value;
        }

        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        public void Initialize(WardrobeItemSO item, EquipmentSlot? sourceSlot = null)
        {
            itemData = item;
            _sourceSlot = sourceSlot;
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemData == null) return;

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
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging) return;
            if (itemData != null)
            {
                OnItemClicked?.Invoke(itemData);
            }
        }

        private void CreateDragProxy(PointerEventData eventData)
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }

            _dragProxyObject = new GameObject("DragProxy_" + (itemData != null ? itemData.ItemName : "Item"));
            if (_rootCanvas != null)
            {
                _dragProxyObject.transform.SetParent(_rootCanvas.transform, false);
                _dragProxyObject.transform.SetAsLastSibling();
            }

            _dragProxyRect = _dragProxyObject.AddComponent<RectTransform>();
            _dragProxyRect.sizeDelta = new Vector2(72f, 72f); // Touch-friendly thumbnail size

            Image proxyImage = _dragProxyObject.AddComponent<Image>();
            if (itemData != null && itemData.Icon != null)
            {
                proxyImage.sprite = itemData.Icon;
            }
            proxyImage.color = new Color(1f, 1f, 1f, 0.85f);
            proxyImage.raycastTarget = false; // Allows underlying slots to detect OnDrop

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
