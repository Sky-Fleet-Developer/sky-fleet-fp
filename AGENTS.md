# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project overview

- **Engine:** Unity `6000.2.5f1` (see `ProjectSettings/ProjectVersion.txt`). Do not upgrade the editor version.
- **Render pipeline:** HDRP (embedded copy at `com.unity.render-pipelines.high-definition@28361c4e5271/`).
- **Language:** C# (Unity scripting), plus HLSL / Shader Graph for visuals.
- **Key libraries:** Zenject/Extenject (DI), UniTask, UniRx, Odin Inspector (Sirenix), DOTween (Demigiant), xNode, Addressables, Input System, Localization, Cinemachine, NWH Wheel Controller, Newtonsoft JSON.
- **Local package dependency:** `com.skyfleetdeveloper.qne` is referenced from `file:E:/Projects/quests-and-events` — it lives outside this repo.

## Repository layout

All game-specific content lives under `Assets/_game/`:

| Path | Purpose |
| --- | --- |
| `Scripts/Core/` | `Core` assembly: gameplay systems, DI bootstrap (`Bootstrapper.cs`), terrain generator, world, trading, AI, serialization, UI structure. Namespace `Core.*`. |
| `Scripts/Runtime/` | `Runtime` assembly: MonoBehaviour-level implementations built on top of `Core`. Namespace `Runtime.*`. |
| `Scripts/SphereWorld/` | `SphereWorld` assembly: spherical world / polar coordinate prototype. |
| `Scripts/WorldEditor/` | `WorldEditor` assembly: in-editor location/content editing tools. |
| `Scripts/Editor/` | Editor-only utilities (in `Assembly-CSharp-Editor`). |
| `Scripts/Core/Tests/` | `Tests` assembly (Editor-only NUnit tests, `UNITY_INCLUDE_TESTS`). |
| `Data/` | ScriptableObjects and generated data assets. |
| `Scenes/` | Scenes (`Menu`, `Game`, `TerrainGenerator`, `CloudVallyCompilation`, `TestArena`, ...). |
| `Visual/` | Shaders, Shader Graphs, HLSL includes, materials. |
| `Prefabs/`, `Sprites/`, `_Resources/` | Content assets. |

Third-party code is in `Assets/Plugins/`, `Assets/NWH/`, `Assets/TextMesh Pro/` and similar — treat these as vendor code and do not modify them unless explicitly asked.

## Feature development rules

These rules are mandatory for every new feature.

1. **Abstraction → realization.** Design the abstraction first, then implement it.
   - **Abstraction** (interfaces, abstract classes, data contracts, events, configs) goes in the **`Core`** assembly.
   - **Realization** (concrete implementations, MonoBehaviours, views, scene-bound logic) goes in the **`Runtime`** assembly.
2. **Isolation.** A new feature should be as independent of other features as possible. It must not reach into other features' concrete classes.
3. **Link through Core + DI.** Features talk to each other only through `Core` abstractions, with dependencies injected via Zenject (bind the realization to its abstraction in an installer). Avoid direct references between `Runtime` implementations, static access, and `FindObjectOfType`-style lookups.
4. **Stop on heavy cross-feature changes.** If a new feature turns out to need significant changes to other existing features, **stop and report to the user** before continuing. Explain what would need to change and why, and propose options (e.g., a new abstraction in `Core`), instead of modifying those features on your own.

## Architecture notes

- Startup is driven by `Core.Bootstrapper` via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`; it builds the project-level Zenject `DiContainer` and the session context. Prefer registering new services through the existing installers rather than adding singletons.
- Asynchronous code uses **UniTask** (not `System.Threading.Tasks` / coroutines) and reactive streams use **UniRx**.
- Assembly boundaries matter: `Core` must not depend on `Runtime` or `WorldEditor`. When adding references, edit the `.asmdef` files (references are by GUID).

## Coding conventions

- Follow the style of surrounding code: file-scoped classes inside `namespace Core.<Area>` / `Runtime.<Area>` matching the folder.
- Private fields: `_camelCase`; public members and types: `PascalCase`.
- Use `[SerializeField] private` for inspector fields; Odin attributes are available and already used.
- Keep editor-only code behind `#if UNITY_EDITOR` or in Editor assemblies so player builds compile.

## Working with Unity assets

- **Every asset has a `.meta` file.** When creating, moving, renaming, or deleting files under `Assets/`, keep the `.meta` file in sync (move/rename/delete it together). Never regenerate or hand-edit GUIDs.
- Do not hand-edit `.unity`, `.prefab`, `.asset`, `.mat`, or `.shadergraph` YAML unless the change is small and well-understood; prefer doing scene/prefab changes in the Unity Editor.
- Do not touch generated or local-only folders: `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`, `Build*/`, `*.csproj`, `*.sln` (Unity regenerates these).

## Building and testing

- There is no CLI build script; compile by opening the project in Unity `6000.2.5f1` and checking the Console for errors.
- Tests run through the Unity Test Runner (Window > General > Test Runner), or headless:
  ```
  Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./TestResults.xml
  ```
- After code changes, verify the project compiles with no new errors or warnings before considering the task done.

## Git

- Main branch: `master`.
- Keep commits focused; include the corresponding `.meta` files with any asset change.
- Do not commit `Library/`, build outputs, or personal `UserSettings/` changes.
