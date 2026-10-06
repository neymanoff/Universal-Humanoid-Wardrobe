# Modular Project Development & Collaboration Agreements (MODULAR_DEVELOPMENT_AGREEMENTS.md)

> **MANDATORY STANDARDS FOR ALL AI AGENTS & CONTRIBUTORS ACROSS ALL CONNECTED GAME MODULES AND PROJECTS**
> This agreement governs how systems, assets, and code are engineered across the main game (`Legends: Legacy of the Lost`), all extracted standalone modules (`Universal-Humanoid-Wardrobe`, `Universal-Hex-Grid`, `Universal-RPG-Stats`, `Universal-Status-Effects`, `Universal-Inventory-Loot`, `Universal-Skill-Constructor`, `Universal-Turn-Combat`, `Universal-RPG-Roster`, `Universal-Scene-Navigator`, `Universal-Save-System`), and any derivative projects.

---

## 1. Core Principles: Respect for Maintainability & Developer Time

### 1.1. Inspector & Visual Authoring First (No Runtime Procedural Bloat)
* **Rule**: **NEVER write in C# code what can and should be configured visually in the Unity Inspector, Prefabs, ScriptableObjects, or Scenes.**
* Do not programmatically construct UI hierarchies, instantiate visual layouts, hardcode coordinates/offsets, configure cameras, or wire up references at runtime if it can be authored via serialized fields, ScriptableObjects, and the Unity Editor.
* Keep C# code lean and focused strictly on:
  1. Pure domain rules, mathematical calculations, and business logic.
  2. Data state transitions and event dispatching.
  3. Clean view bindings and adapter wrappers.
* Every component intended for designer or developer interaction must expose clean, self-explanatory `[SerializeField]` fields with tooltips and sensible defaults, allowing tweaking directly inside the Inspector without diving into code.

### 1.2. Agent Scene & Hierarchy Placement
* When an AI agent manipulates scenes, test setups, or prototypes, it must configure declarative assets, scene files, and prefabs directly rather than injecting procedural boilerplate code (e.g., dynamically creating GameObjects, adding components via endless `AddComponent` calls, or setting transforms in Awake/Start).
* Keep scene hierarchies clean, structured, and easy for the human developer to inspect, select, and adjust in the Unity Editor.

---

## 2. Leverage Existing Solutions & Package Ecosystem

### 2.1. "Never Reinvent the Wheel" Rule
* Before writing any new subsystem, check for existing official Unity packages or established, free, commercially-licensed third-party solutions (e.g., MIT, Apache 2.0, BSD, Unity Companion License).
* If an official or well-tested free package exists that can be legally used in a commercial game (e.g., Unity Input System, TextMeshPro, Cinemachine, Addressables, UniTask, A* Pathfinding / NavMesh, etc.), **use and integrate that package** instead of authoring thousands of lines of custom wheel-reinventing code.
* Custom implementation is only warranted when:
  1. No suitable, commercially-permissive package exists.
  2. The external dependency introduces unacceptable bloat or violates platform constraints.
  3. The core gameplay mechanic requires bespoke, tailored domain logic.

---

## 3. Mandatory Workflow: Source-First Extraction & Universality

### 3.1. Phase 0: Mandatory Legacy Codebase Inspection & Audit (`docs/LEGACY_AUDIT.md`)
* **Strict Prohibition**: An agent is **STRICTLY FORBIDDEN** from authoring code, creating mock scripts, or generating systems based solely on a high-level prompt without inspecting the original game!
* **Mandatory Action**: Before writing ANY line of code in Phase 1, the agent MUST:
  1. Open and thoroughly inspect the source code in `d:\Unity\My Projects\LegendsLegacyOfLost`.
  2. Identify all relevant existing classes, algorithms, math formulas, and data structures.
  3. Author an authoritative `docs/LEGACY_AUDIT.md` document in the module's repository, containing:
     - Exact source file paths in `LegendsLegacyOfLost`.
     - What each file does and what working game logic/math must be preserved.
     - What monolithic couplings (singletons like `GameManager`/`GridManager`, hardcoded UI calls, scene dependencies) must be refactored into clean interfaces or events.
     - What is discarded and why.
* **No coding begins until `docs/LEGACY_AUDIT.md` is fully drafted and grounded in the real codebase.**

### 3.2. True Package Universality (Zero Game Taint / Asset Store & Fab Ready)
* The extracted module is an **independent, reusable product** (for Unity Asset Store, Fab, or other games).
* **Zero Game Taint**: The module MUST NEVER contain references, namespaces, or dependencies specific to `LegendsLegacyOfLost` (e.g., no `Legends.*`, no `GridManager.Instance`, no specific game singletons).
* **Adapter Belongs in the Game**: Any bridge or adapter layer needed to connect `LegendsLegacyOfLost` with the module belongs **inside the `LegendsLegacyOfLost` project**, NOT inside the universal module.

### 3.3. Dual-Purpose Architecture (Pure C# Headless Core + Unity Presentation)
Every extracted module must be structured to support two usage modes:
1. **Headless Domain (`Runtime/Core/` with `noEngineReferences: true`)**:
   - Pure C# / .NET Standard 2.1 domain models, combat math, algorithms, state machines.
   - Zero dependency on `UnityEngine.dll`.
   - Runs on local/remote servers (headless microservices) and executes instant NUnit tests without engine overhead.
2. **Unity Presentation Adapter (`Runtime/Unity/` or `Runtime/Presentation/`)**:
   - Contains ScriptableObjects, Inspector drawers, view components, prefabs, and Unity 6 event bindings.

### 3.4. Prohibition of Superficial Quick Drafts & Fake "Done"
* An agent must NEVER rush out a shallow, unverified draft just to claim quick completion.
* The module must implement the full, real gameplay and mathematical requirements extracted from the source game, not a toy prototype.
* Jumping across multiple roadmap phases without thorough implementation, automated tests, and demo verification is a critical violation of developer trust.

### 3.5. Greybox Prototyping (Cubes & Spheres Verification)
* When building or testing systems in the module, verify them using minimal greybox setups (cubes, spheres, simple tilemaps, canvas testbeds) in `Samples~/Demo` before integrating production art.
* Each module must prove its autonomous functionality in its own isolated sample/demo scene.

### 3.6. Two-Stage Task Completion Protocol (`[IMPLEMENTED]` vs `[VERIFIED]`)
* **Pre-Work Registration**: Tasks in `TODO.md` must be marked `[IN PROGRESS]` before touching code.
* **`[IMPLEMENTED]`**: Marked by the agent ONLY when:
  1. Code is fully written and compiles with zero errors and zero warnings (`<WarningsAsErrors>CS0618</WarningsAsErrors>`).
  2. 100% of automated NUnit unit/integration tests pass.
  3. An Inspector-authored sample demo scene is verified and operational.
* **`[VERIFIED]`**: Marked **ONLY BY THE HUMAN DEVELOPER** after personally testing the feature in the Unity Editor PlayMode and explicitly approving it.
* **STRICT PROHIBITION**: An agent is **NEVER** permitted to mark a task as `[VERIFIED]` autonomously.

### 3.7. Non-Destructive In-Place Evolution (No Delete-And-Recreate)
* **Rule**: **NEVER instruct the developer to delete existing scene hierarchies, root GameObjects, or scenes to adopt or test new components.**
* All components must support in-place evolution:
  1. Adding a new component via `Add Component` on an existing object must gracefully self-heal and auto-wire dependencies via `Reset()` and `OnValidate()`.
  2. Modifications to existing components must preserve existing serialized data and designer overrides.

### 3.8. Step-by-Step Discipline & Tool Communication
* Work strictly step-by-step. Complete one phase at a time and review it before proceeding.
* If Unity batchmode CLI is required, explicitly request the developer to save and close the Editor. Never guess or run silent CLI tasks when the project is locked.

---

## 4. Summary of Developer Agreements Checklist

| Principle | Requirement | Violation Example |
| :--- | :--- | :--- |
| **Audit-First** | Must create `docs/LEGACY_AUDIT.md` before coding | Writing a stat or grid script from a prompt without reading `LegendsLegacyOfLost` |
| **Zero Game Taint** | Module has zero references to `Legends` | Adding `GridManager.Instance` or `Legends` namespaces inside the universal package |
| **Inspector-First** | Configure in Inspector/Prefabs/SO | Writing 200 lines of procedural C# code to create UI buttons or spawn hierarchies |
| **Non-Destructive** | In-place evolution via `Add Component` / auto-wiring | Telling the developer: "Delete the root object and run the setup menu again" |
| **No Reinventing** | Use official & free commercial packages | Authoring custom input polling instead of Unity 6 Input System |
| **Lean Code** | Clean, searchable, modular C# | Monolithic scripts that handle data, logic, UI, and visuals in one class |
| **No Fake "Done"** | Real, complete logic with tests | Claiming milestone finished with a superficial 20-line stub |
| **Two-Stage Verification** | `[IMPLEMENTED]` (agent) vs `[VERIFIED]` (human) | Agent declaring task verified without developer PlayMode sign-off |
| **Step-by-Step** | One phase at a time with developer review | Completing 4 phases in one turn and claiming "ready for the next module" |
| **No Duplication** | Reuse extracted code via shared packages | Copy-pasting stat math or grid classes across 3 different projects |
