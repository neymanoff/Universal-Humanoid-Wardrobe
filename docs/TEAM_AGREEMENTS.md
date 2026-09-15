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

## 4. Multi-Agent & Local Model Hierarchy (Ollama & MCP Integration)

To combine strategic cloud insights with high-throughput, private local execution, the project employs a tiered multi-model architecture connected via the Model Context Protocol (MCP):

### 4.1. Hardware Profile & Engine
* **Host Workstation**: AMD Ryzen 9 9950X (16C/32T), NVIDIA GeForce RTX 5080 (16 GB GDDR7), 64 GB DDR5, NVMe SSD.
* **Local Daemon**: Ollama (`http://127.0.0.1:11434`), storage directory `E:\AI\Models\Ollama`, context window up to 256k.
* **MCP Bridge**: Global stdio bridge configured in `~/.gemini/config/mcp_config.json` running `ollama-mcp`.

### 4.2. Registered Local Models & Dedicated Roles
| Model Identifier | Base Architecture | Specialization & Primary Role |
| :--- | :--- | :--- |
| `unity-coder:30b` | `qwen3-coder:30b` | **Lead Unity 6 C# Synthesis**: Pre-conditioned via custom `Modelfile` to strictly enforce modern Unity 6 APIs, zero-GC execution, and reject deprecated methods. |
| `unity-thinker:32b` | `deepseek-r1:32b` | **Red Team Reviewer & Deep Reasoner**: Pre-conditioned via custom `Modelfile` for adversarial code reviews, race-condition detection, and rig retargeting validation. |
| `nomic-embed-text:latest` | Nomic Embed | **Semantic Indexing & Embeddings**: Rapid vectorization for semantic codebase queries and documentation retrieval without context pollution. |

### 4.3. 3-Tier Defense-in-Depth Against Obsolete APIs
To ensure no deprecated or legacy code ever enters the repository, the project enforces a three-tier defense system:
1. **Tier 1 (Model Level - Modelfile System Constraints)**: Both `unity-coder:30b` and `unity-thinker:32b` have hardcoded system prohibitions against legacy APIs (`FindObjectOfType`, `UnityEngine.UI.Text`, `WWW`, `RandomRange`).
2. **Tier 2 (Context Level - Knowledge Base & Standards)**: The `docs/UNITY6_STANDARDS.md` architectural specification provides unambiguous rules for replacements, zero-allocation conventions, and bone remapping protocols.
3. **Tier 3 (Compiler Level - MSBuild Fatal Errors)**: The root `Directory.Build.props` configures `<WarningsAsErrors>CS0618</WarningsAsErrors>`. Any deprecated API call instantly causes compilation failure with compiler-generated remediation hints.

### 4.4. Operational Protocol & The Multi-Tier Workflow
1. **Tier 1: Cloud Advisory (ChatGPT Plus / Gemini Pro)**:
   * Consulted for macroscopic design questions, Unity Asset Store market standards, and third-party competitor patterns.
   * Prompts are structured as condensed challenge queries to minimize token consumption and avoid context bloat.
2. **Tier 2: Host Orchestrator (Antigravity)**:
   * Holds the project state, manages Git/LFS hygiene, enforces `TEAM_AGREEMENTS.md`, executes compilation builds (`dotnet build`), and applies file changes.
   * Serves as the arbiter between conflicting agent opinions.
3. **Tier 3: Local Specialist Swarm (Ollama / RTX 5080)**:
   * **Pre-implementation check**: Complex architecture plans are challenged by `unity-thinker:32b` for edge-case vulnerability.
   * **Implementation**: `unity-coder:30b` generates boilerplate and implementation drafts under orchestrator supervision.
   * Zero marginal cost: Unlimited local iterations without consumption of cloud API quotas.

### 4.5. Human Approval Rule
* Irrespective of consensus among local or cloud models, all functional changes, new features, and architectural pivots remain strictly subject to explicit human developer review and confirmation before being written or committed.


