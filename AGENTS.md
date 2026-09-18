# Project Guide

## Project and entry points

- **MASK SHIFTER** is a Unity C# game built around collecting masks, switching abilities, and completing levels.
- Use the editor version in `ProjectSettings/ProjectVersion.txt`. At initialization, the working copy uses **6000.6.0f1**; the upgrade from 6000.3.5f2 is an existing uncommitted change.
- `Packages/manifest.json` defines package versions. The current stack includes URP 17.6.0, Input System 1.20.0, Cinemachine 6.6.0, UGUI/TextMesh Pro, and the Unity Test Framework.
- Zenject, DOTween, and Odin Inspector are bundled under `Assets/Plugins`.
- `Assets/Scenes/Level0.unity` is the only enabled scene in `ProjectSettings/EditorBuildSettings.asset`. Start gameplay checks there.

## Code and asset map

- `Assets/Scripts/Installer/AppInstaller.cs`: binds `GameConfig` through `Assets/Resources/ProjectContext.prefab`.
- `Assets/Scripts/Installer/SceneInstaller.cs`: binds the scene's player, mask manager, level manager, and popups. Preserve serialized references and Zenject injection when changing these objects.
- `Assets/Scripts/Player/Player.cs`: wires input, movement, animation, stats, and level events. Plain C# controllers live in `Player/Controllers`; movement uses `IPlayerMotor` and `CharacterControllerMotor`.
- `Assets/Scripts/Mask/MaskManager.cs`: available masks, current mask, and equip/unequip/update events. `Mask.cs` handles pickups; `Mask/Controllers` contains dragging, moving, visibility, phase, highlight, and color behavior.
- `Assets/Scripts/Settings/Enums.cs`: serialized mask IDs: None=0, Strength=1, Intelligence=2, Agility=3, Mover=4, Shadow=5. Preserve these numeric values.
- `Assets/Scripts/Objectives/LevelManager.cs`: instantiates level prefabs through Zenject, positions the player, advances or restarts levels, and emits level events. Runtime progression uses its serialized list, not separate scene loads.
- `Assets/Scripts/UI`: mask selection and information popups. The namespace is `GameUI`.
- `Assets/Scripts/GenerationLevel/LevelGenerator.cs`: inspector-triggered procedural generation and the `LevelPalette` definition.
- `Assets/Scripts/TutorStep.cs`: tutorial trigger behavior.
- `Assets/Prefab/Player.prefab`, `Assets/Prefab/SceneContext.prefab`, `Assets/Prefab/Levels`, and `Assets/Prefab/TakeMask`: primary gameplay prefabs.
- `Assets/Resources/GameConfig.asset`, `Assets/Resources/Data/MaskData.asset`, and `Assets/Resources/LevelPalette.asset`: gameplay configuration, mask presentation data, and generation palette. `MaskPopup` loads `Data/MaskData` by its Resources path.
- `Assets/Editor`: project editor tools. Runtime game scripts currently compile into `Assembly-CSharp`; no game-specific assembly definitions were found.
- `Assets/ThirdParty`, `Assets/Plugins`, `Assets/MK`, `Assets/PlayerPrefsEditor`, and `Assets/TextMesh Pro` contain vendor assets. Keep changes there limited to the requested task.

## Input

- Edit `Assets/Scripts/Player/PlayerInputActions.inputactions` for bindings, then regenerate its C# wrapper through Unity. `PlayerInputActions.cs` is generated; do not hand-edit it.
- The `Gameplay` map binds WASD movement, Space jump, Shift run, and Q/Tab/Left Alt to hold the mask popup open. A/D navigates masks while the popup is open. Mouse bindings include drag, right click, pointer position, and look delta.
- Keep action names and serialized input asset references consistent across the player, popups, and physics controllers.

## Required coding rules

### 1. Architecture & Static Usage

- NEVER use `static` fields or methods for general game logic, state management, or utility functions.
- The `static` keyword is STRICTLY RESERVED for accessing instances of Manager classes (e.g., Singleton accessors like `public static GameManager Instance { get; private set; }`).
- Do not use local static variables inside methods.
- Prefer event-driven architecture (C# `Action`, `event`, or interface-based listeners) over singletons where possible to avoid tight coupling.

### 2. Component & Method Caching

- Never call `GetComponent<T>()`, `GetComponentInChildren<T>()`, `FindObjectOfType<T>()`, or `Camera.main` inside `Update()` or continuous loops. Cache these references in `Awake()` or `Start()`.
- Remove all empty Unity lifecycle methods (e.g., empty `Start()` or `Update()`) as they incur C++ to C# overhead.

### 3. String & Tag Optimization

- Never use standard string equality checks for tags. Always use `GameObject.CompareTag("TagName")`.
- Never pass strings directly to Animators or Shaders in hot loops. Always cache the integer hash using `Animator.StringToHash("ParamName")` or `Shader.PropertyToID("PropName")` in `Awake()`.

### 4. Encapsulation & Inspector Best Practices

- Never use `public` fields just to expose variables to the Unity Inspector.
- Keep state variables private and strictly use `[SerializeField] private Type _variableName;` for Inspector configuration.
- Use `[RequireComponent(typeof(T))]` at the top of the class for any components the script strictly relies on.

## Change workflow

- Inspect the working tree before editing and preserve existing changes. Initialization found extensive local asset metadata changes, package/editor upgrades, generated input changes, and other source/settings changes. Do not assume they are disposable.
- Prefer the existing Zenject bindings and events for dependencies. Pair event subscriptions with appropriate unsubscriptions and clean up owned input actions and tweens.
- Existing scripts contain patterns that differ from the required rules. Follow the rules when implementing changes without expanding into unrelated rewrites.
- Preserve Unity asset GUIDs and `.meta` files. Move assets together with their metadata and retain serialized values when renaming fields, using `FormerlySerializedAs` where needed.
- Do not hand-edit generated `.sln`/`.csproj` files or generated data in `Library`, `Temp`, `obj`, and `Logs`.
- Avoid broad asset reserialization, package upgrades, and vendor edits unless needed for the task.

## Validation

- Use the matching Unity editor to compile and inspect the Console. For gameplay changes, check the affected behavior in Play Mode from `Assets/Scenes/Level0.unity`.
- Relevant smoke checks include movement/jumping, mask pickup and selection, affected world interactions, death/restart, and level completion. Verify serialized and injected references when changing scene components or prefabs.
- No project-owned tests were found in `Assets/Scripts` or `Assets/Editor` at initialization. Bundled Zenject tests do not establish game behavior coverage.
- Generated C# project builds are only a supplemental compile check; they do not validate Unity imports, scene wiring, shaders, or gameplay.
- Keep validation proportional to the change, inspect the final diff, and report what was actually checked. Initialization itself only inspected files and created this guide; it did not run a build or Play Mode test.
