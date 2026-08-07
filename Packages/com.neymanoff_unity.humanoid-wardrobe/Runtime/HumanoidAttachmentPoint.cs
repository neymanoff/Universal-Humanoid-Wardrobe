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
        [Header("Bone Selection")]
        [Tooltip("If true, ignores the slot's default bone and uses TargetBone below")]
        [SerializeField]
        private bool useCustomBone = false;
        
       [Tooltip("Custom target bone (only used if UseCustomBone is enabled).")]
       [SerializeField] private HumanBodyBones targetBone = HumanBodyBones.RightHand;

       [Header("Offsets")]
       [Tooltip("Local position offset relative to hte bone.")]
       [SerializeField] private Vector3 localPosition =  Vector3.zero;
       
       [Tooltip("Local rotation offset relive to the bone (Euler angles).")]
       [SerializeField]  private Vector3 localRotation =  Vector3.zero;
       
       [Tooltip("Local scale override (usually 1, 1, 1).")]
       [SerializeField] private Vector3 localScale =  Vector3.one;
       
       [Header("Mirroring Options")]
       [Tooltip("Automatically mirror Position X and Rotation Y/Z when equipped in OffHand / Left slots.")]
       [SerializeField] private bool autoMirrorForLeftSlot = true;
       
       public bool UseCustomBone => useCustomBone;
       public HumanBodyBones TargetBone => targetBone;
       public Vector3 LocalPosition => localPosition;
       public Vector3 LocalRotation => localRotation;
       public Vector3 LocalScale => localScale;

       /// <summary>
       /// Applies local offsets, optionally mirroring for left-side slots (OffHand, LeftRing).
       /// </summary>
       public void ApplyOffsets(bool isLeftSlot = false)
       {
           Vector3 pos = localPosition;
           Vector3 rot = localRotation;

           if (isLeftSlot && autoMirrorForLeftSlot)
           {
               pos.x = -pos.x;
               rot.y = -rot.y;
               rot.z = -rot.z;
           }
           
           transform.localPosition = pos;
           transform.localRotation = Quaternion.Euler(rot);
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
