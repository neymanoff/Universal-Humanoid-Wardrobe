# Team & Agent Collaboration Agreements

This document defines the universal interaction protocol, operational boundaries, and collaboration rules between human developers and AI pair-programming agents on this Unity project (and serves as a portable standard across all repositories).

---


## 1. Operational Protocol: Discussion vs. Execution

### 1.1. Inquiries Are Not Action Directives
* Any prompt phrased as a question, hypothesis, architectural review, or discussion (e.g., *"What do you say to this?"*, *"What do you think about X?"*, *"Could we do Y?"*) is an **explicit invitation to analyze and debate**.
* In response to an inquiry, the agent **MUST NOT** edit, create, or delete source code or documentation files.
* The agent's role during an inquiry is to:
  1. Cross-reference the proposal against the actual codebase and Unity engine constraints.
  2. Identify potential blind spots, edge cases, or trade-offs.
  3. Offer reasoned arguments in favor of or against the proposal.
  4. Formulate actionable options for the developer to approve.

### 1.2. Explicit Action Triggers
* Source code, assets, and project documentation may only be modified upon a **direct, unambiguous call to action** from the developer (e.g., *"Approved, implement it"*, *"Make this change"*, *"Go ahead"*).

### 1.3. Critical Review Filter
* External code reviews or architectural feedback often evaluate documentation without full visibility into Unity-specific mechanics (e.g., Animator state machines, SkinnedMeshRenderer culling, shader variants, import settings).
* The agent must evaluate all incoming feedback critically against the actual Unity project state rather than accepting theoretical patterns at face value.

---

## 2. Language & Documentation Standards

* **Chat & Communication**: Discussion, explanations, and planning in chat are conducted in **Russian**.
* **Code & Repository Artifacts**: All C# source code, XML documentation, code comments, commit messages, and repository documents (`README.md`, `docs/*.md`) are written strictly in **English**.

---

## 3. Git & File Safety Rules

* **Manual Developer Commits**: The agent must **NEVER** execute `git commit` commands. The human developer reviews all changed files, inspections, and diffs manually via GitHub Desktop before pushing. Upon completing work, the agent must provide only the proposed Git commit `Summary` and `Description` in English.
* **Staging Hygiene**: Avoid blind bulk staging (`git add -A` or `git commit -a`) when binary 3D assets or textures are in flux.
* **Git LFS Awareness**: Large binaries (`*.fbx`, `*.png`, `*.ttf`) are managed via Git LFS. Never delete or commit deletions of binary assets unless explicitly intended.
* **Meta File Protection**: Unity `.meta` files preserve GUIDs and asset references. Never delete orphaned `.meta` files without verifying whether the corresponding source asset exists.


---

## 4. Agent Orchestration & Local Worker Models

Cloud agents and local models have different responsibilities. The active agent is the orchestrator and remains accountable for the repository outcome; local models provide execution capacity for bounded work. This portable policy does not depend on a specific agent brand, host machine, model identifier, or bridge implementation.

### 4.1. Responsibility Hierarchy
1. **Human Developer / Product Owner**:
   * Defines intent, priorities, constraints, and acceptance criteria.
   * Approves functional changes and performs final Git review and commits.
2. **Orchestrating Agent**:
   * Maintains task context and reads the actual repository before making decisions.
   * Decomposes work, delegates suitable subtasks, reviews returned output, integrates changes, validates the result, and keeps documentation consistent.
   * Remains responsible for errors introduced by worker output; delegation is not a transfer of accountability.
3. **Local Worker Models**:
   * Produce bounded drafts for implementation, refactoring, tests, repetitive analysis, or adversarial review.
   * Do not decide product scope, mark their own work verified, perform Git commits, or receive secrets.
4. **Specialist Tools**:
   * Vision models and image/audio generators may assist with review or produce staged source material when the task requires it.
   * Generated or visually assessed output still requires license/provenance review and the appropriate Unity, human, or target-device validation.

### 4.2. Local-First, Evidence-Driven Workflow
* Use an appropriate local worker first for non-trivial code generation, mechanical refactoring, test drafting, repetitive consistency checks, or broad static review when local tooling is available.
* Reserve cloud reasoning primarily for architecture, unclear requirements, cross-system impact, integration, policy/security-sensitive decisions, and final review.
* Give workers narrow prompts with exact files, constraints, expected output, and acceptance criteria. Large vague prompts waste local compute and produce difficult-to-review changes.
* Review every proposed diff against the real codebase. Validate with builds, tests, static checks, profiler captures, visual checks, or device checks as appropriate. Model consensus is not evidence.
* If a local worker is unavailable, exceeds practical context/hardware limits, or repeatedly fails review, the orchestrator may implement directly or select another worker and must report the fallback briefly.
* Load GPU-heavy workers sequentially by default and unload each after its bounded task. Simultaneous residency is permitted only after measured dedicated-VRAM use, context/KV cache, generator memory, and a safety margin are shown to fit.
* Follow `docs/LOCAL_AI_TOOLCHAIN.md` for model admission, Unity command-line validation, external art libraries, and generated-asset provenance.

### 4.3. Capability-Based Model Registry
Each repository may maintain a project-specific worker registry in its root agent instructions. Roles should be assigned using representative project benchmarks, for example:

| Capability | Suitable Worker Role |
| :--- | :--- |
| Unity/C# implementation | Bounded code and refactoring drafts |
| Deep reasoning | Independent edge-case and lifecycle review |
| Test generation | NUnit fixtures, boundary cases, regression matrices |
| Retrieval/embeddings | Repository and documentation search support |
| Localization consistency | Key parity, placeholder, terminology, and layout-risk review |
| Vision/UI review | Screenshot comparison, clipping, safe-area, contrast, and localization layout risks |
| Image/audio generation | Staged, licensed source material for an explicitly approved asset need |

Model names, quantization levels, context limits, hardware, and storage paths belong in project-specific configuration, not in this portable agreement.

### 4.4. Defense-in-Depth Against Obsolete APIs
1. **Worker Context**: Give implementation and review workers the repository's current engine/API standards.
2. **Repository Standards**: Maintain explicit modern API and performance guidance such as `docs/UNITY6_STANDARDS.md`.
3. **Tool Enforcement**: Treat obsolete API diagnostics as errors where supported and validate with the actual Unity/compiler pipeline. A model instruction alone is not enforcement.

### 4.5. Human Approval & TODO Semantics
* A direct, unambiguous developer instruction is approval for the described scope; agents should not demand a redundant second confirmation.
* An unchecked TODO or roadmap item records memory and intent but is not authorization to implement it. Confirm backlog items before execution because priorities and earlier decisions may have changed.
* Before approved implementation begins, update one consolidated TODO entry with the current scope and acceptance criteria. Mark it complete only after verification.
* No local or cloud consensus can expand scope beyond the developer's request or authorize a commit.
