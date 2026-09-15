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

## 2. 3-Tier Anti-Obsolete Defense (Unity 6 Standards)
This project strictly prohibits obsolete Unity APIs (enforced via `Directory.Build.props` with `<WarningsAsErrors>CS0618</WarningsAsErrors>`):
1. **Never use obsolete methods**:
   * `FindObjectOfType<T>()` -> Use `FindFirstObjectByType<T>()` or `FindAnyObjectByType<T>()`.
   * `UnityEngine.UI.Text` -> Use `TMPro.TextMeshProUGUI`.
   * `WWW` -> Use `UnityEngine.Networking.UnityWebRequest`.
   * `Application.LoadLevel(...)` -> Use `UnityEngine.SceneManagement.SceneManager.LoadScene(...)`.
   * `Random.RandomRange(...)` -> Use `UnityEngine.Random.Range(...)`.
2. **Read the Full Specification**:
   * Refer to `docs/UNITY6_STANDARDS.md` for zero-allocation memory guidelines, bone retargeting rules, and `UnityEngine.Awaitable` patterns.

---

## 3. Local Model Swarm & MCP Delegation (Ollama)
The host machine runs a local Ollama daemon (`http://127.0.0.1:11434`) connected via global MCP (`ollama-mcp`):
* **`unity-coder:30b`**: Primary engine for C# synthesis, refactoring, and NUnit test writing.
* **`unity-thinker:32b`**: Adversarial reviewer (Chain of Thought) for edge-case and race-condition hunting.
* **`nomic-embed-text`**: Semantic codebase and documentation embeddings.

---

## 4. Git & Asset Safety
* **Manual Developer Commits**: The agent must NEVER execute `git commit`. The developer reviews all diffs and commits manually via GitHub Desktop. Upon completing work, the agent provides only the suggested commit `Summary` and `Description` in English.
* **Git LFS**: Never stage or commit deletions of binary 3D assets (`*.fbx`, `*.png`, `*.mat`) without explicit developer approval.
* **Unity Meta Files**: Every asset must have a valid `.meta` file. Never delete `.meta` files without verifying source existence.

