using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Neymanoff.HumanoidWardrobe.UI
{
    [DisallowMultipleComponent]
    public class CharacterRotator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Speed multiplier for character rotation.")]
        [SerializeField] private float rotationSpeed = 0.25f;
        
        private Camera _mainCamera;
        private bool _isDragging = false;

        private void Awake()
        {
            _mainCamera  = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current ==  null || _mainCamera == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Ray  ray = _mainCamera.ScreenPointToRay(mousePos);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        _isDragging = true;
                    }
                }
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (!_isDragging || !Mouse.current.leftButton.isPressed) return;
            float deltaX = Mouse.current.delta.x.ReadValue();
            transform.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}
