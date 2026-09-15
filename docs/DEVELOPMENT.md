# Universal Humanoid Wardrobe — Development & Repository Guide

This document contains internal development notes, Git/LFS troubleshooting, and recovery procedures for contributors working on the **Universal Humanoid Wardrobe** repository.

---

## 1. Git LFS Architecture & Recovery

All 3D mesh files (`*.fbx`) and texture binaries (`*.png`, `*.ttf`) are tracked via **Git LFS** in [`.gitattributes`](../.gitattributes).

### 1.1. The "404 Object does not exist" Issue
When cloning the repository on a new machine, Git LFS may encounter 404 errors if binary blobs were not pushed or bandwidth quota was exhausted on GitHub:
```text
Error downloading object: Assets/3D_Models/... (404) Object does not exist on the server
```

If this occurs:
1. **Never delete `.meta` files**: Unity deletes missing asset `.meta` files automatically if launched without the binaries. If deleted, restore them immediately:
   ```bash
   git checkout -- "*.meta"
   ```
2. **Obtain Raw Binaries**: Copy the original FBX and PNG files directly into the target folders (`Assets/3D_Models/...`). Because the `.meta` files retain the original GUIDs, Unity will immediately link all meshes, rigs, and materials without breaking references.
3. **LFS Smudge Bypass**: If Git blocks checkout due to LFS download failures:
   ```powershell
   $env:GIT_LFS_SKIP_SMUDGE="1"
   git checkout -- .
   ```

---

## 2. Commit & Staging Hygiene

To avoid polluting the repository with local caches:
* The root [`.gitignore`](../.gitignore) excludes `.gemini/`, `.antigravity/`, `.vscode/`, `.idea/`, `ProjectAuditorSettings.asset`, and OS temporary files.
* **Never use `git commit -a` or `git add -A` blindly** when test models are missing or in-flux, as this can accidentally stage deletions of large binaries.
* Always stage specific files:
  ```bash
  git add Packages/ docs/ README.md .gitignore
  ```

---

## 3. Incident History (Phase 1 Post-Mortem)

During initial project transfer:
* An accidental `git rm -r --cached .` staged the deletion of 401 tracked files.
* Missing LFS blobs triggered Unity's automatic deletion of 50+ `.meta` files.
* Typos existed in test assembly definitions (`Neymanoff.HumanoidWardrobeardrobe`).
* `WardrobeManagerEditor.cs` contained a duplicate prefab leak in edit-mode preview.

All issues were resolved in commit `6fd88c9`:
* Index restored via `git reset HEAD`.
* All `.meta` files recovered and GUID bindings validated.
* Test `.asmdef` references repaired.
* In-editor instantiation leak fixed with proper `Undo` group collapsing.
