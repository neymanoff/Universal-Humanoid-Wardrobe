# Unity 6 Architecture & Modern Scripting Standards

This document establishes portable architectural, performance, and API conventions for Unity 6 projects across 2D, 3D, and UI-heavy games. Project-specific architecture and measured performance budgets belong in that project's living documentation. All AI models, human contributors, and automated tooling must follow the exact Unity version and packages declared by the repository rather than assuming an older, more familiar API surface.

---

## 1. Obsolete & Deprecated API Replacements

Where the repository treats `CS0618` as an error, obsolete APIs are prohibited. Confirm replacements against the project's installed Editor and package documentation because deprecation state and behavior can change between Unity 6 releases:

| Prohibited Legacy API | Modern Unity 6 Replacement | Rationale & Behavioral Notes |
| :--- | :--- | :--- |
| `Object.FindObjectOfType<T>()` | `Object.FindFirstObjectByType<T>()` or `Object.FindAnyObjectByType<T>()` | `FindFirstObjectByType` preserves deterministic scene order; `FindAnyObjectByType` is significantly faster when order is irrelevant. |
| `Object.FindObjectsOfType<T>()` | `Object.FindObjectsByType<T>(FindObjectsSortMode)` | Requires explicit specification of sort mode (`None` vs `InstanceID`) to prevent unexpected sorting overhead. |
| `UnityEngine.UI.Text` | `TMPro.TextMeshProUGUI` | Legacy UI Text lacks SDF rasterization, subpixel anti-aliasing, and modern typography formatting. |
| `WWW` / `WWWForm` | `UnityEngine.Networking.UnityWebRequest` | Obsolete memory-leaking networking stack; `UnityWebRequest` provides streaming, memory recycling, and async disposal. |
| `Application.LoadLevel(...)` | `UnityEngine.SceneManagement.SceneManager.LoadScene(...)` | Deprecated since Unity 5.3; `SceneManager` supports additive, asynchronous, and addressable loading. |
| `UnityEngine.Random.RandomRange(...)` | `UnityEngine.Random.Range(...)` | Legacy naming alias removed from modern assemblies. |
| `Component.guiText` / `guiTexture` | Canvas UI or UI Toolkit (`UnityEngine.UIElements`) | Completely removed legacy IMGUI/GUI component layer. |
| Repeated `Camera.main` access in hot paths | Cached `Camera` reference where profiling justifies it | Avoid repeated lookups and hidden dependencies in frequently executed code; validate lifetime when cameras can change. |
| Legacy `UnityEngine.Input.*` in projects standardized on the Input System | Unity Input System (`UnityEngine.InputSystem`) | Follow the project's selected Active Input Handling and installed package; do not mix input stacks accidentally. |

---

## 2. Zero-Allocation & Memory Hygiene

Garbage Collection (GC) spikes can cause visible frame-time instability during input, physics, animation, UI layout, spawning, or camera motion. Profile first, then enforce allocation-free behavior in verified hot paths:

### 2.1. Per-Frame Methods (`Update`, `LateUpdate`, `FixedUpdate`)
* **NO LINQ**: Prohibit `Where`, `Select`, `Any`, `FirstOrDefault`, `ToList()` inside per-frame updates or frequent animation callbacks.
* **NO Dynamic Allocations**: Prohibit `new List<T>()`, `new Dictionary<K,V>()`, or string concatenations (`"score_" + val`) inside loops.
* **Non-Alloc Queries & Buffer Reuse**:
  * Use non-alloc variants where available (e.g. `GetComponentsInChildren<T>(bool, List<T>)` passing a pre-allocated reusable buffer).
  * Throttle or cache frequently formatted UI values when profiling shows meaningful allocation pressure. Do not claim that `StringBuilder` is allocation-free without measurement.

### 2.2. Event & State Payloads
* Use structured DTOs passed by `in` or `ref` where practical, or lightweight `readonly record struct` instances to eliminate boxing overhead.

---

## 3. Runtime Object Pooling & View Lifecycle

Frequently spawned/despawned objects, UI rows, projectiles, effects, enemies, props, or cards can cause allocation spikes, hierarchy churn, and repeated initialization. Pool only where lifetime frequency and profiling justify the added complexity:

### 3.1. Pre-Warmed Object Pooling
* **Pre-Warm Measured Capacity**: Pre-warm a documented baseline when runtime spikes are unacceptable; define safe growth and exhaustion behavior instead of assuming a fixed universal count.
* **View Recycling**: Reset transient state, event subscriptions, cancellation sources, animation state, physics state, and ownership before a pooled object is reused.
* **Transform/Hierarchy Cost**: Batch or minimize reparenting and sibling-order changes in UI and scene hierarchies when profiling shows layout, canvas, or transform rebuild cost.

### 3.2. State-Driven View Presentation
* Views and scene components should present state and translate engine events; domain rules should live in independently testable C# where practical.
* Keep pooling mechanics separate from gameplay rules. A returned object's domain ownership and presentation state must both be reset explicitly.

---

## 4. Rendering, Canvas, Physics & Responsive Bounds

Performance depends on the actual platform bottleneck. Use the Unity Profiler, Frame Debugger, platform GPU tools, and representative devices before selecting an optimization:

### 4.1. Sub-Canvas Segregation
* **Static vs. Dynamic Canvases**: Separate frequently changing UI from stable UI when measurements show costly canvas rebuilds. Avoid excessive canvases that increase batches or sorting complexity.
* **Raycast Target Optimization**: Disable raycast targets on non-interactive graphics where appropriate and verify event behavior after the change.
* **2D/3D Rendering**: Control material instances, shader variants, transparency/overdraw, shadow cost, LOD, occlusion, and batching according to the active render pipeline and target hardware.
* **Physics**: Use the correct 2D or 3D physics stack consistently, apply sensible layer collision matrices, and avoid unbounded allocation-heavy queries in hot paths.

### 4.2. Responsive Layout, Camera & Bounds Management
* **Safe Areas and Input Reachability**: Interactive UI must respect safe areas, aspect ratio, orientation, input method, localization expansion, and minimum touch targets on supported devices.
* **World Bounds**: Camera framing, spawning, navigation, and interaction volumes must handle supported aspect ratios and 2D/3D world extents without hiding required content.
* **Project-Specific Algorithms**: Layout compression, camera rails, split-screen, or responsive object placement rules belong in the project's architecture/design documentation and tests.

---

## 5. Modern Asynchronous Operations

* Choose **`UnityEngine.Awaitable`**, standard `Task`-based async, jobs, Coroutines, or another installed project standard based on lifecycle, threading, WebGL/platform support, error propagation, and allocation profile. Do not mechanically replace working Coroutines without a measured or architectural reason.
* Async work tied to a Unity object or scene must have explicit cancellation/lifetime ownership (for example, a token linked to `destroyCancellationToken` where supported) and must return to the correct thread before accessing Unity objects.
