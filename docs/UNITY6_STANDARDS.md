# Unity 6 Architecture & Modern Scripting Standards

This document establishes the mandatory architectural and API conventions for the **Universal Humanoid Wardrobe** package in Unity 6 (6000.x LTS). All AI models, human contributors, and automated tooling must adhere to these standards.

---

## 1. Obsolete & Deprecated API Replacements

To ensure compatibility with modern Unity 6 runtimes and strict compilation rules (`CS0618`), the following legacy APIs are strictly prohibited across the codebase:

| Prohibited Legacy API | Modern Unity 6 Replacement | Rationale & Behavioral Notes |
| :--- | :--- | :--- |
| `Object.FindObjectOfType<T>()` | `Object.FindFirstObjectByType<T>()` or `Object.FindAnyObjectByType<T>()` | `FindFirstObjectByType` preserves deterministic scene order; `FindAnyObjectByType` is significantly faster when exact order is irrelevant. |
| `Object.FindObjectsOfType<T>()` | `Object.FindObjectsByType<T>(FindObjectsSortMode)` | Requires explicit specification of sort mode (`None` vs `InstanceID`) to prevent unexpected sorting overhead. |
| `UnityEngine.UI.Text` | `TMPro.TextMeshProUGUI` | Legacy UI Text has inferior font rendering, lacking SDF rasterization and modern formatting features. |
| `WWW` / `WWWForm` | `UnityEngine.Networking.UnityWebRequest` | Obsolete memory-leaking networking stack; `UnityWebRequest` provides streaming, memory recycling, and async disposal. |
| `Application.LoadLevel(...)` | `UnityEngine.SceneManagement.SceneManager.LoadScene(...)` | Deprecated since Unity 5.3; `SceneManager` supports additive, asynchronous, and addressable loading. |
| `UnityEngine.Random.RandomRange(...)` | `UnityEngine.Random.Range(...)` | Legacy naming alias removed from modern assemblies. |
| `Component.guiText` / `guiTexture` | Canvas UI or UI Toolkit (`UnityEngine.UIElements`) | Completely removed legacy IMGUI/GUI component layer. |
| `Camera.main` in per-frame loops | Cached `Camera` reference or local lookup | `Camera.main` internally performs `FindObjectWithTag("MainCamera")` per call; always cache in `Awake`/`Start`. |

---

## 2. Zero-Allocation & Memory Hygiene

Garbage Collection (GC) spikes cause micro-stutters during character movement and outfit customization. The wardrobe module must enforce zero-GC execution in hot execution paths:

### 2.1. Per-Frame Methods (`Update`, `LateUpdate`, `FixedUpdate`)
* **NO LINQ**: Prohibit `Where`, `Select`, `Any`, `FirstOrDefault` inside per-frame updates or frequent animation callbacks.
* **NO Dynamic Allocations**: Prohibit `new List<T>()`, `new Dictionary<K,V>()`, or string concatenations (`"item_" + id`) inside loops.
* **Non-Alloc Physics & Hierarchy Queries**:
  * Use non-alloc variants where available (e.g. `GetComponentsInChildren<T>(bool, List<T>)` passing a pre-allocated reusable buffer).

### 2.2. Event Invocations
* Use structured DTOs passed by `in` or `ref` where practical, or lightweight `readonly record struct` instances to eliminate boxing overhead.

---

## 3. Humanoid Rig & SkinnedMesh Retargeting Rules

The wardrobe module's primary responsibility is attaching apparel and accessories to standard Unity Humanoid skeletons:

### 3.1. Skinned Mesh Remapping (`SkinnedMeshRemapper`)
1. **Bone Cache Mapping**:
   * Build an index lookup table (dictionary or hashed array) mapping bone names from the target character skeleton.
   * Remap the clothing `SkinnedMeshRenderer.bones` array directly to corresponding `Transform` nodes of the base character.
2. **Root Bone Assignment**:
   * Always bind `SkinnedMeshRenderer.rootBone` to the character's `Hips` bone (`HumanBodyBones.Hips`), NEVER to the root `GameObject`. This ensures proper frustum culling and bounding box calculations during locomotion.
3. **Bindpose Integrity**:
   * Retain the apparel's original rest-pose `bindposes` unless custom retargeting or proportional bone compensation is explicitly performed.

### 3.2. Rigid Socket Attachments
1. **Bone Socket Resolution**:
   * Access target bones via `Animator.GetBoneTransform(HumanBodyBones bone)`.
   * If the requested bone is unmapped in the avatar, return an explicit `EquipResult.MissingBone` failure instead of failing silently or throwing a `NullReferenceException`.
2. **Transform Reset**:
   * When parenting rigid accessories (weapons, helmets, shields) to bone sockets, reset local offsets to the item's configured `socketOffset` and `socketRotation`.

---

## 4. Multi-Slot & Equipment Atomicity Rules

1. **Atomic Swapping**:
   * Two-handed items (e.g., greatswords, bows) occupying both `MainHand` and `OffHand` must be treated as a single atomic unit.
   * Unequipping or replacing one occupied slot must cleanly unequip all associated secondary slots without leaving orphaned visual instances.
2. **Rule Decoupling**:
   * Slot conflict resolution, occupancy checks, and loadout validation must reside in pure domain helpers (`EquipmentRuleResolver`), isolated from Unity rendering logic.

---

## 5. Modern Asynchronous Operations

* For asynchronous operations (e.g., asset addressable loading, loadout rehydration, cross-fade item transitions), prefer **`UnityEngine.Awaitable`** or standard `async/await` patterns over Coroutines.
* Ensure all async routines accept and check `CancellationToken` linked to the host `MonoBehaviour`'s `destroyCancellationToken`.
