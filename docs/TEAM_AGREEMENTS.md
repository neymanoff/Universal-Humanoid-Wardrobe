# Team & Agent Collaboration Agreements

This document defines the interaction protocol, operational boundaries, and collaboration rules between human developers and AI pair-programming agents on the **Universal Humanoid Wardrobe** project.

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

* **Staging Hygiene**: Avoid blind bulk staging (`git add -A` or `git commit -a`) when binary 3D assets or textures are in flux.
* **Git LFS Awareness**: Large binaries (`*.fbx`, `*.png`, `*.ttf`) are managed via Git LFS. Never delete or commit deletions of binary assets unless explicitly intended.
* **Meta File Protection**: Unity `.meta` files preserve GUIDs and asset references. Never delete orphaned `.meta` files without verifying whether the corresponding source asset exists.

---

## 4. Multi-Agent & Local Model Hierarchy

When external or local AI agents (e.g. DeepSeek, Hermes, local LLM harnesses) are integrated into the workflow:
1. **Host Orchestrator**: The primary agent manages project state, Unity compilation verification, and Git operations.
2. **Review & Specialist Agents**: Local models provide secondary architectural reviews, unit test generation, or domain-specific analysis via MCP or local APIs (`localhost`).
3. **Consensus Requirement**: Major architectural changes proposed by any agent must be reviewed and unanimously approved by the human developer before implementation.
