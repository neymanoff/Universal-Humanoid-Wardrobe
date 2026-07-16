using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Component attached to an equipment prefab (like a weapon or shield)
    /// to define which humanoid bone it attaches to and its local offsets.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/Humanoid Attachment Point")]
    public class HumanoidAttachmentPoint : MonoBehaviour
    {
       [Header("Attachment Settings")]
       [Tooltip("The target bone on the humanoid skeleton to attach this item to")]
       [SerializeField] private HumanBodyBones targetBone = HumanBodyBones.RightHand;

       [Header("Offsets")]
       [Tooltip("Local position offset relative to hte bone.")]
       [SerializeField] private Vector3 localPosition =  Vector3.zero;
       
       [Tooltip("Local rotation offset relive to the bone (Euler angles).")]
       [SerializeField]  private Vector3 localRotation =  Vector3.zero;
       
       [Tooltip("Local scale override (usually 1, 1, 1).")]
       [SerializeField] private Vector3 localScale =  Vector3.one;
       
       public HumanBodyBones TargetBone => targetBone;
       public Vector3 LocalPosition => localPosition;
       public Vector3 LocalRotation => localRotation;
       public Vector3 LocalScale => localScale;

       /// <summary>
       /// Applies the configured local position, rotation, and scale offsets relative to its parent.
       /// </summary>
       public void ApplyOffsets()
       {
           transform.localPosition = localPosition;
           transform.localRotation = Quaternion.Euler(localRotation);
           transform.localScale = localScale;
       }

       [ContextMenu("Capture Current Transform as Offsets")]
       private void CaptureCurrentTransform()
       {
           localPosition = transform.localPosition;
           localRotation = transform.localRotation.eulerAngles;
           localScale = transform.localScale;
           Debug.Log($"[HumanoidAttachmentPoint] Captured offsets for {gameObject.name}");
       }
    }
}
