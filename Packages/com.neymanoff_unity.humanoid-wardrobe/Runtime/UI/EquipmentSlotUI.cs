using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Neymanoff.HumanoidWardrobe.UI
{
    /// <summary>
    /// UI component representing a single equipment slot on the character paper-doll.
    /// Handles showing placeholder silhouettes, active item icons, and click events to unequip.
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Slot Configuration")]
        [Tooltip("The equipment slot this UI element represents.")]
        [SerializeField] public EquipmentSlot slotType;
        
        [Header("UI References")]
        [Tooltip("Image component displaying the icon of the currently equipped item.")]
        [SerializeField] public Image itemIconImage;
        
        [Tooltip("Image component displaying the neutral silhouette placeholder when slot is empty")]
        [SerializeField] public Image silhouetteImage;
        
        private WardrobeManager _wardrobeManager;
        private WardrobeItemSO _currentItem;
        
        public EquipmentSlot SlotType => slotType;

        public void Initialize(WardrobeManager manager)
        {
            _wardrobeManager = manager;

            if (_wardrobeManager != null)
            {
                _wardrobeManager.OnEquipmentChanged += HandleEquipmentChanged;
            }

            UpdateVisuals(null);
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
                _currentItem = null;
                UpdateVisuals(null);
            }
        }

        /// <summary>
        /// Updates the slot visuals with the equipped item's icon or placeholder silhouette.
        /// </summary>
        public void SetEquipmentItem(WardrobeItemSO item)
        {
            _currentItem = item;
            UpdateVisuals(item != null ? item.Icon : null);
        }

        private void UpdateVisuals(Sprite icon)
        {
            if (icon != null)
            {
                if (itemIconImage != null)
                {
                    itemIconImage.sprite = icon;
                    itemIconImage.enabled = true;
                }

                if (silhouetteImage != null)
                {
                    silhouetteImage.enabled = false;
                }
            }
            else
            {
                if (itemIconImage != null)
                {
                    itemIconImage.enabled = false;    
                }

                if (silhouetteImage != null)
                {
                    silhouetteImage.enabled = true;
                }
            }
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_wardrobeManager != null && _currentItem != null)
            {
                _wardrobeManager.Unequip(slotType);
            }
        }
    }
    
}