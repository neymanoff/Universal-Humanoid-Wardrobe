using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Utility component that remaps the bone references of instantiated clothing/armor
    /// SkinnedMeshRenderers  to target character's active skeleton.
    /// </summary>

    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/Skinned Mesh Remapper")]
    public class SkinnedMeshRemapper : MonoBehaviour
    {
        /// <summary>
        /// Remaps all SkinnedMeshRenderers on this GameObject (and its children)
        /// to use the bone hierarchy of the target skeleton.
        /// </summary>
        /// <param name="targetSkeletonRoot"> The root transform of the target character skeleton (usually the Animator's GameObject). </param>
        public void Remap(Transform targetSkeletonRoot)
        {
            if (targetSkeletonRoot == null)
            {
                Debug.LogError($"[SkinnedMeshRemapper] Target skeleton is null on {gameObject.name}!");
                return;
            }
            
            Dictionary<string, Transform> targetBoneMap =  new Dictionary<string, Transform>();
            BuildBoneMapRecursive(targetSkeletonRoot, targetBoneMap);
            SkinnedMeshRenderer[] clothingRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer clothingRenderer in clothingRenderers)
            {
                Transform[] currentBones = clothingRenderer.bones;
                Transform[] newBones = new Transform[currentBones.Length];

                for (int i = 0; i < currentBones.Length; i++)
                {
                    if (currentBones[i] == null) continue;
                    
                    string boneName = currentBones[i].name;
                    if (targetBoneMap.TryGetValue(boneName, out Transform matchingTargetBone))
                    {
                        newBones[i] = matchingTargetBone;
                    }
                    else
                    {
                        Debug.LogWarning($"[SkinnedMeshRemapper] Target bone '{boneName}' not found in skeleton for {clothingRenderer.name}!");
                        newBones[i] = currentBones[i];
                    }
                }
                
                clothingRenderer.bones = newBones;

                if (clothingRenderer.rootBone == null) continue;
                string rootBoneName = clothingRenderer.rootBone.name;
                if (targetBoneMap.TryGetValue(rootBoneName, out Transform matchingRootBone))
                {
                    clothingRenderer.rootBone = matchingRootBone;
                }
            }

            CleanupDuplicateSkeleton();
        }

        /// <summary>
        /// Recursively traverses a transform hierarchy and maps bone names to transforms.
        /// </summary>
        private void BuildBoneMapRecursive(Transform current, Dictionary<string, Transform> map)
        {
            map.TryAdd(current.name, current);

            for (int i = 0; i < current.childCount; i++)
            {
                BuildBoneMapRecursive(current.GetChild(i), map);
            }
        }

        /// <summary>
        /// Cleans up the  duplicate skeleton hierarchy instantiated with the prefab to save preformance.
        /// </summary>
        private void CleanupDuplicateSkeleton()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (child.GetComponentInChildren<Renderer>() != null) continue;
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
