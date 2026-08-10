using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.UI
{
    public class CharacterRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 5f;

        private void OnMouseDrag()
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Mathf.Deg2Rad;
            transform.Rotate(Vector3.up, -rotX *  Time.deltaTime);
        }
    }
}
