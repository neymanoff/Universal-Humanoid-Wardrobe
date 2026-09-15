using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Utility component placed on clothing and armor prefabs that dynamically remaps
    /// all child SkinnedMeshRenderer bones to the target Humanoid character's skeleton.
    /// Implements hierarchical ancestor fallback for twist/helper bones, normalized prefix matching,
    /// and bounds stabilization against frustum culling flicker.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Humanoid Wardrobe/Skinned Mesh Remapper")]
    public class SkinnedMeshRemapper : MonoBehaviour
    {
        private static readonly string[] CommonRigPrefixes = new[]
        {
            "mixamorig:",
            "mixamorig6:",
            "mixamorig_",
            "Bip01_",
            "Bip01 ",
            "Bip_",
            "DEF-",
            "Bone_",
            "ValveBiped."
        };

        /// <summary>
        /// Remaps all SkinnedMeshRenderers in this equipment item to the target character skeleton.
        /// </summary>
        /// <param name="targetSkeletonRoot">The root transform of the target character (containing the Animator).</param>
        public void Remap(Transform targetSkeletonRoot)
        {
            if (targetSkeletonRoot == null)
            {
                Debug.LogError($"[SkinnedMeshRemapper] Target skeleton is null on {gameObject.name}!");
                return;
            }

            Animator animator = targetSkeletonRoot.GetComponentInChildren<Animator>();

            // Build exact and normalized bone maps for fast lookup
            Dictionary<string, Transform> exactBoneMap = new();
            Dictionary<string, Transform> normalizedBoneMap = new(StringComparer.OrdinalIgnoreCase);
            BuildBoneMapsRecursive(targetSkeletonRoot, exactBoneMap, normalizedBoneMap);

            // Determine target root bone (strictly Hips for Humanoid rigs)
            Transform targetRootBone = ResolveTargetRootBone(animator, exactBoneMap, targetSkeletonRoot);

            // Locate base character renderer for bounds stabilization
            SkinnedMeshRenderer baseCharacterRenderer = FindBaseCharacterRenderer(targetSkeletonRoot);

            SkinnedMeshRenderer[] clothingRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (int r = 0; r < clothingRenderers.Length; r++)
            {
                SkinnedMeshRenderer clothingRenderer = clothingRenderers[r];
                Transform[] currentBones = clothingRenderer.bones;
                Transform[] newBones = new Transform[currentBones.Length];

                for (int i = 0; i < currentBones.Length; i++)
                {
                    Transform currentBone = currentBones[i];
                    if (currentBone == null)
                    {
                        newBones[i] = targetRootBone;
                        continue;
                    }

                    // 1. Exact match by name
                    if (exactBoneMap.TryGetValue(currentBone.name, out Transform matchingTargetBone))
                    {
                        newBones[i] = matchingTargetBone;
                        continue;
                    }

                    // 2. Normalized match (stripping rig prefixes like mixamorig:, DEF-, Bip01_)
                    string normalizedName = NormalizeBoneName(currentBone.name);
                    if (normalizedBoneMap.TryGetValue(normalizedName, out Transform normalizedTargetBone))
                    {
                        newBones[i] = normalizedTargetBone;
                        continue;
                    }

                    // 3. Hierarchical Ancestor Fallback (walk up parent chain for twist/helper bones)
                    Transform ancestorMatch = ResolveAncestorFallback(currentBone, exactBoneMap, normalizedBoneMap);
                    if (ancestorMatch != null)
                    {
                        Debug.LogWarning(
                            $"[SkinnedMeshRemapper] Bone '{currentBone.name}' missing in target skeleton for '{clothingRenderer.name}'. " +
                            $"Safely re-routed to ancestor '{ancestorMatch.name}'.");
                        newBones[i] = ancestorMatch;
                        continue;
                    }

                    // 4. Fallback to target root bone to guarantee no dangling/destroyed transform references
                    Debug.LogWarning(
                        $"[SkinnedMeshRemapper] Bone '{currentBone.name}' and its ancestors missing in target skeleton for '{clothingRenderer.name}'. " +
                        $"Falling back to root bone '{targetRootBone.name}'.");
                    newBones[i] = targetRootBone;
                }

                clothingRenderer.bones = newBones;
                clothingRenderer.rootBone = targetRootBone;

                // Stabilize bounds to prevent frustum culling flicker
                StabilizeBounds(clothingRenderer, baseCharacterRenderer);
            }

            CleanupDuplicateSkeleton();
        }

        /// <summary>
        /// Traverses up the parent chain of an unmapped bone until a matching ancestor is found in the target skeleton.
        /// Prevents vertex explosion and broken transform references for twist and helper bones.
        /// </summary>
        private Transform ResolveAncestorFallback(
            Transform originalBone,
            Dictionary<string, Transform> exactMap,
            Dictionary<string, Transform> normalizedMap)
        {
            Transform parent = originalBone.parent;
            while (parent != null && parent != transform)
            {
                if (exactMap.TryGetValue(parent.name, out Transform matchingParent))
                {
                    return matchingParent;
                }

                string normalizedParentName = NormalizeBoneName(parent.name);
                if (normalizedMap.TryGetValue(normalizedParentName, out Transform normalizedMatchingParent))
                {
                    return normalizedMatchingParent;
                }

                parent = parent.parent;
            }

            return null;
        }

        /// <summary>
        /// Normalizes bone names by stripping standard DCC and engine prefixes.
        /// </summary>
        private static string NormalizeBoneName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;

            string name = rawName;
            for (int i = 0; i < CommonRigPrefixes.Length; i++)
            {
                string prefix = CommonRigPrefixes[i];
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            return name.Trim();
        }

        /// <summary>
        /// Recursively maps target skeleton bone names and normalized names to Transforms.
        /// </summary>
        private void BuildBoneMapsRecursive(
            Transform current,
            Dictionary<string, Transform> exactMap,
            Dictionary<string, Transform> normalizedMap)
        {
            exactMap.TryAdd(current.name, current);

            string normalized = NormalizeBoneName(current.name);
            normalizedMap.TryAdd(normalized, current);

            for (int i = 0; i < current.childCount; i++)
            {
                BuildBoneMapsRecursive(current.GetChild(i), exactMap, normalizedMap);
            }
        }

        /// <summary>
        /// Resolves the canonical root bone (Hips) for the target skeleton.
        /// </summary>
        private Transform ResolveTargetRootBone(
            Animator animator,
            Dictionary<string, Transform> exactMap,
            Transform fallbackRoot)
        {
            if (animator != null && animator.isHuman)
            {
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null) return hips;
            }

            if (exactMap.TryGetValue("Hips", out Transform hipsByName)) return hipsByName;
            if (exactMap.TryGetValue("Pelvis", out Transform pelvisByName)) return pelvisByName;
            if (exactMap.TryGetValue("Root", out Transform rootByName)) return rootByName;

            return fallbackRoot;
        }

        /// <summary>
        /// Finds the primary SkinnedMeshRenderer on the target character (e.g. base body).
        /// </summary>
        private SkinnedMeshRenderer FindBaseCharacterRenderer(Transform targetSkeletonRoot)
        {
            SkinnedMeshRenderer[] renderers = targetSkeletonRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                // Exclude any renderers belonging to currently instantiated clothing items
                if (!renderers[i].transform.IsChildOf(transform))
                {
                    return renderers[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Stabilizes local bounds against the base character mesh to eliminate frustum culling flicker.
        /// </summary>
        private void StabilizeBounds(SkinnedMeshRenderer clothingRenderer, SkinnedMeshRenderer baseRenderer)
        {
            if (baseRenderer != null)
            {
                Bounds combined = baseRenderer.localBounds;
                combined.Encapsulate(clothingRenderer.localBounds);
                combined.Expand(0.2f);
                clothingRenderer.localBounds = combined;
            }
        }

        /// <summary>
        /// Safely cleans up the duplicate skeleton hierarchy instantiated with the equipment prefab.
        /// </summary>
        private void CleanupDuplicateSkeleton()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (child.GetComponentInChildren<Renderer>(true) != null) continue;

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
