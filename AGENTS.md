# AI Agent Collaboration & Engineering Standards (AGENTS.md)

> **MANDATORY INSTRUCTIONS FOR ALL AI AGENTS ENTERING THIS WORKSPACE**:
> You must strictly adhere to the operational agreements defined below.

---

## 1. Operational Protocol: Discussion vs. Execution
* **Questions != Directives**: Any prompt phrased as a question, hypothesis, review, or discussion (*"What do you say to this?"*, *"Could we do X?"*) is strictly an analytical inquiry. **DO NOT edit, create, or delete code or docs.** Debate, analyze trade-offs, and wait for confirmation.
* **Direct Action Triggers**: Only execute code changes or file modifications when receiving an explicit, unambiguous command (e.g. *"Implement this"*, *"Make changes"*, *"Go ahead"*).
* **Language Rules**:
  * Chat dialogue and interactive discussions with the developer: **RUSSIAN**.
  * Code, XML docstrings, comments, Git commit messages, and documentation: **ENGLISH**.

---

## 2. 3-Tier Anti-Obsolete Defense & Zero-Warning Policy (Unity 6 Standards)
This project targets the exact Unity version recorded in `ProjectSettings/ProjectVersion.txt` and strictly prohibits obsolete Unity APIs (enforced via `Directory.Build.props` with `<WarningsAsErrors>CS0618</WarningsAsErrors>`):
1. **Never use obsolete methods**:
   * `FindObjectOfType<T>()` and `FindFirstObjectByType<T>()` -> Use `FindAnyObjectByType<T>()`.
   * `UnityEngine.UI.Text` -> Use `TMPro.TextMeshProUGUI`.
   * `WWW` -> Use `UnityEngine.Networking.UnityWebRequest`.
   * `Application.LoadLevel(...)` -> Use `UnityEngine.SceneManagement.SceneManager.LoadScene(...)`.
   * `Random.RandomRange(...)` -> Use `UnityEngine.Random.Range(...)`.
   * Legacy `UnityEngine.Input.*` -> Use Unity 6 Input System (`UnityEngine.InputSystem`).
2. **Zero-Warning Policy**: All compiler warnings, obsolete API notices (CS0618), and Unity console warnings MUST be treated as errors and resolved immediately. Zero warning tolerance in build and tests.

---

## 3. Mandatory Modular & Inspector Standards
* **Inspector First**: Do not write in C# code what can and should be configured visually in the Unity Inspector, Prefabs, or ScriptableObjects (`WardrobeItemSO`, slot bindings, bone retargeting settings).
* **Greybox Prototyping First**: Test humanoid bone retargeting and clothing swaps on standard rigs before attaching high-poly armor.
* **Do Not Reinvent the Wheel**: Leverage official Unity animation/rigging systems and standard C# collections.
* **Clean Decoupling**: Maintain strict separation between Headless Domain (Pure C# in `Runtime/Core/` with `noEngineReferences: true`) and Unity Presentation (in `Runtime/Unity/`).
* Refer to `MODULAR_DEVELOPMENT_AGREEMENTS.md` for the full cross-project engineering standard.

---

## 4. Source-First Grounding & Architectural Truth
1. **Mandatory Legacy Inspection & Audit (`docs/LEGACY_AUDIT.md`)**:
   * Before writing, generating, or refactoring ANY code in Phase 1, the agent MUST inspect and cite the corresponding working files in the main game (`d:\Unity\My Projects\LegendsLegacyOfLost`).
   * For this module, inspect: `Assets/Scripts/Features/Wardrobe/` or existing avatar/equipment mesh attachment systems, bone mapping, and material tinting.
   * The agent MUST author `docs/LEGACY_AUDIT.md` before Phase 1 code, detailing source files, preserved logic/mechanics, refactored monolithic couplings, and discarded obsolete items.
   * Never invent new abstractions or speculative architectures without first verifying how the system was originally solved in `Legends: Legacy of the Lost`.
2. **Zero Game Taint**:
   * This module is an independent package (`com.neymanoff.wardrobe`). It must NEVER depend on or reference `LegendsLegacyOfLost` namespaces, classes, or singletons. Any integration bridge belongs in `LegendsLegacyOfLost`.
3. **Dual Scope Requirement**:
   * **Pure C# Core (`Runtime/Core`)**: Slot definitions, equipment compatibility matrices, color palette tint definitions, serialized equipment loadout DTOs.
   * **Unity Presentation (`Runtime/Unity`)**: SkinnedMeshRenderer bone retargeting, material property blocks, character preview stage.
4. **Step-by-Step Discipline & Communication**:
   * Strictly execute one bounded phase at a time. Never bundle multiple phases or jump ahead without explicit user review.
   * If Unity batchmode CLI or background test execution is required, explicitly ask the developer to save and close the Unity Editor. Never run batchmode while the project is locked by an active Editor instance.

---

## 5. Task Progression & Verification Protocol ([IMPLEMENTED] vs [VERIFIED])
1. **Pre-Execution Registration**: Before writing any code, the intended task must be registered as `[IN PROGRESS]` in `TODO.md` with specific acceptance criteria.
2. **"Implemented" vs "Verified"**:
   * `[IMPLEMENTED]`: The agent has completed the code, passed 100% of automated NUnit CLI tests, and verified zero compilation errors.
   * `[VERIFIED]`: The human developer has personally tested the feature in the Unity Editor PlayMode and explicitly confirmed that it works as expected.
   * **STRICT PROHIBITION**: The agent is NEVER permitted to mark a task as `[VERIFIED]` on its own. Only the human developer grants verification status.
3. **Non-Destructive In-Place Evolution**:
   * Never instruct the developer to delete existing scene hierarchies, root GameObjects, or scenes. Components must auto-wire via `Reset()` and `OnValidate()`.

---

## 6. Git & Asset Safety
* **Manual Developer Commits**: The agent must NEVER execute `git commit`. The developer reviews all diffs and commits manually via GitHub Desktop. Upon completing work, the agent provides only the suggested commit `Summary` and `Description` in English.
* **Git LFS**: Never stage or commit deletions of binary 3D assets (`*.fbx`, `*.png`, `*.mat`) without explicit developer approval.
* **Unity Meta Files**: Every asset must have a valid `.meta` file. Never delete `.meta` files without verifying source existence.
