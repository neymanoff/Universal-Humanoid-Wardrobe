using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// UI component representing a single equipment slot on the character paper-doll.
    /// Supports item icons, default silhouettes, and 2H weapon blocking tints.
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
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
        
        public EquipmentSlot SlotType => slotType;
        public WardrobeItemSO CurrentItem => _currentItem;

        private void Awake()
        {
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
            if (_wardrobeManager == null) return;

            if (_isBlockedByTwoHanded)
            {
                _wardrobeManager.Unequip(EquipmentSlot.MainHand);
            } else if (_currentItem != null)
            {
                _wardrobeManager.Unequip(slotType);
            }
        }
    }
    
}