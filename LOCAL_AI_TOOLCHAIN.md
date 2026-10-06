# Local AI & Generative Toolchain Standard

This document defines a portable, capability-based workflow for using local AI workers and generative tools in Unity 2D and 3D projects. Repository-specific model identifiers, hardware, paths, and benchmark results belong in the root `AGENTS.md`, not in this reusable standard.

---

## 1. Roles and Authority

* **Human Developer / Product Owner**: Defines product intent, approves functional or artistic direction, accepts licenses, and performs the final Git review and commit.
* **Orchestrating Agent**: Decomposes work, selects tools, supplies grounded context, reviews outputs, integrates approved changes, runs validation, and remains accountable for the result.
* **Worker Model**: Produces a bounded code draft, test draft, analysis, review, localization check, or visual assessment. It is not an autonomous repository authority.
* **Asset Generator**: Produces staged source material. Generated output is not automatically production-ready or licensed for the project merely because generation succeeded.
* **Validator**: The real compiler, Unity Editor, test runner, profiler, target device, or human visual/audio review. Model agreement is never a substitute for this evidence.

Workers may suggest changes but must not expand scope, mark their own work verified, accept external licenses, receive secrets, or commit to Git.

## 2. Local Model Lifecycle and Resource Budget

1. Prefer the smallest measured model that reliably handles the bounded task.
2. Load one GPU-heavy model at a time by default. Unload it after the task (`keep_alive: 0`, the runtime's stop command, or equivalent) before starting another heavy model or generator.
3. Concurrent models are allowed only when observed dedicated VRAM usage, context/KV cache, generator memory, and a safety margin all fit. Advertised parameter size or combined system graphics memory is not proof.
4. Do not keep large models resident merely for convenience. CPU/RAM offload is an explicit slower fallback, not an assumed success state.
5. Keep only the context needed for the delegated unit of work. Large unfiltered repository dumps waste memory and reduce answer quality.
6. Record resource failures, timings and quality separately. A deadline or output budget exhausted by a healthy worker is not, by itself, evidence of poor model quality. Allow for model loading, reasoning and CPU/RAM offload before deciding whether a smaller worker or shorter context is needed.

### 2.1. Time, Reasoning Budget and Responsive Waiting

* Use separate budgets for wall-clock execution and generated tokens (including reasoning where the runtime counts it). More time does not repair a token cutoff; more tokens do not repair a request already cancelled by its caller.
* For substantive local work, use a generous initial request allowance (normally 10 minutes, extending to 20 for complex/offloaded tasks) and a complete-answer token budget (around 4,096 for bounded drafts/reviews, 8,192 for reasoning-heavy implementation). Adjust to measured task needs and memory; these are defaults, not a reason to force a task to run for the full duration. Tiny few-hundred-token budgets are reserved for tiny deterministic smoke tests.
* Keep the user informed using asynchronous execution and short polling. A polling yield is not a worker deadline; do not restart or cancel a healthy request merely because a short poll returned no final answer.
* When throughput is known, estimate a compatible deadline from loading time plus token allowance divided by measured tokens per second, with a safety margin. Extend beyond the initial 10/20-minute default or explicitly reduce/split scope when necessary; do not pair a large reasoning allowance with a known-inadequate time limit. Measurements from one task are estimates, not permanent speed guarantees.
* Inspect stop/completion reason and final content. Retry a healthy, budget-limited attempt once with more suitable resources or a narrower brief before declaring the worker unsuitable. Distinguish resource failure, incomplete output, factual errors and successfully reviewed work. Do not turn retries into an unlimited loop.
* Compare model candidates on the same grounded tasks with sufficient budgets for each to finish, recording correctness and integration/review cost as well as latency and memory. Do not equate a newer version, a faster answer, or a developer's promising trial with proven superiority across roles.

## 3. Capability Registry and Admission

A repository-specific registry must record, at minimum:

| Field | Required Evidence |
| :--- | :--- |
| Exact identifier | Runtime-visible name and tag; do not rely on a marketing family name |
| Runtime and format | For example, Ollama/quantization or ComfyUI/checkpoint format |
| Intended role | Code, reasoning, tests, retrieval, vision, image, audio, or another bounded capability |
| License/provenance | Source repository and license status, including any gated terms |
| Hardware result | Dedicated VRAM/RAM use, context, latency, and offload behavior on the current workstation |
| Quality result | A representative project benchmark and review outcome |
| Status and date | Installed/verified, candidate, rejected, or pending; include the verification date |

Do not create permanent roles for uninstalled or unbenchmarked models. A copied or derived local tag must be identified as such rather than presented as an independent model.

## 4. Delegation Protocol

Each worker brief should contain:

* the precise question or deliverable;
* exact relevant files or extracts;
* the current Unity/package/API constraints;
* invariants, exclusions, and acceptance criteria;
* the required output form (analysis, patch proposal, test matrix, or structured findings).

The orchestrator must inspect the relevant repository state before delegating, review returned claims against that state, and apply only the portions that survive review. Use a separate reviewer for high-risk gameplay, persistence, lifecycle, monetization, platform, build/release, security, or cross-cutting changes when that review adds useful independence.

## 5. Unity Command-Line Validation

* Resolve the exact Editor version from `ProjectSettings/ProjectVersion.txt` and use its installed executable. Do not silently open the project with another Editor version or upgrade packages.
* Ensure only one Unity process owns a project. If the project is already open or locked, use an approved live-editor bridge where available or ask the developer to close/release it before batch execution.
* For batch validation, use explicit parameters such as `-projectPath`, `-batchmode`, `-quit`, `-logFile`, `-runTests`, `-testPlatform`, `-testResults`, or a reviewed `-executeMethod` entry point as appropriate.
* Treat the process exit code, complete Editor log, generated test XML, and requested output artifact as a single evidence set. Launching Unity or seeing a model report success is insufficient.
* Run the narrowest relevant check first, then broader EditMode, PlayMode, build, profiler, device, or visual checks in proportion to risk.
* Never terminate an unrelated Unity process, overwrite a developer's active scene, or invoke release/signing steps without explicit authorization.

## 6. Vision and UI Review

Vision workers may assist with screenshot comparison, localization overflow, clipping, contrast, spacing, safe-area risks, incorrect sprite use, and 2D/3D visual consistency. Their findings remain hypotheses until checked in the correct Unity scene, Game view resolution/orientation, build, or target device. RTL behavior, animation, interaction states, dynamic layout, lighting, shader behavior, and audio-visual timing require runtime evidence.

## 7. External and Generated Assets

### 7.1. External Source Libraries

* Treat art/audio folders outside the Unity project as source libraries, not automatically imported production content.
* Read or copy only task-relevant files. Do not modify or reorganize the source library by default.
* Verify license, author/source, allowed commercial use, and modification/attribution requirements before import.
* Import deliberately into the project, preserve Unity GUIDs for existing assets, and review the generated `.meta` and import settings.

### 7.2. Generation Rules

* Generate assets only for an approved need; do not create replacements simply because a generator is available.
* Do not accept gated model licenses or third-party terms on the developer's behalf.
* Write outputs to a staging location first. Never overwrite production assets directly.
* Record generator/model version, source and license, prompt or brief, seed/settings where applicable, date, output path, and post-processing in an asset provenance record.
* Review generated content for visual/audio quality, project style, unwanted text/logos, IP or likeness risk, seams, alpha, color space, dimensions, loop points, loudness, performance, and platform import/compression settings as relevant.
* Human approval and an in-Unity check are required before a generated asset becomes production content.

## 8. Storage, Security, and Reproducibility

* Keep model caches and generator installations outside the Unity repository unless a small configuration file is intentionally versioned.
* Never send credentials, keystores, signing material, private user data, paid source assets, or other secrets to local or cloud models.
* Prefer pinned model revisions and documented workflows over mutable `latest` tags for reproducible asset generation.
* Do not commit model weights, caches, generated previews, temporary logs, or runtime environments to the game repository.
* Re-run representative smoke tests after runtime, driver, model, workflow, Unity version, or major package changes.

## 9. Definition of Done

A local-worker or generator task is complete only when:

1. the output was reviewed against the current repository and task scope;
2. required licenses and provenance are known;
3. appropriate Unity/compiler/test/visual/audio/device checks passed or remaining manual checks are explicitly recorded;
4. temporary heavy models are unloaded when no longer needed;
5. the authoritative TODO and affected documentation are updated without duplicating status across unrelated files.
