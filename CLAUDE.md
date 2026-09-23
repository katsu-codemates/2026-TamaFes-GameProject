# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A Unity game built for a school festival (たまフェス / TamaFes). Visitors draw an animal illustration on an external website; the illustration is imported into the game as a "racehorse" and raced against others. The flow is: **BaseScene** (an idle showcase screen where imported animal illustrations wander around and can be inspected) → **GameScene** (the race itself, with commentary and camera direction).

## Environment

- Engine: Unity **6000.3.6f1** (see `ProjectSettings/ProjectVersion.txt`). Open the project with the matching Unity Hub install.
- Render pipeline: URP (Universal Render Pipeline).
- UI: Unity UGUI (Canvas/RectTransform), plus TextMeshPro for text.
- Tweening: [DOTween](Assets/Plugins/Demigiant/DOTween) is used throughout for animation instead of Animator/sprite-sheet animation — animal artwork is a single static image manipulated via transform/scale/color tweens (there are no animation frames).
- Assets use **Git LFS** (`.gitattributes`); run `git lfs pull` after cloning or the imported textures/fonts will be missing.
- There is no CLI build/lint/test pipeline in this repo (no CI workflows, no npm/make scripts). All building, running, and testing happens inside the Unity Editor. `com.unity.test-framework` is a listed package dependency but no test assemblies currently exist in `Assets/`.
- Two scenes are registered in the build (`ProjectSettings/EditorBuildSettings.asset`): `Assets/00_BaseScene/BaseScene.unity` and `Assets/01_GameScene/GameScene.unity`. `Assets/Scenes/SampleScene.unity` is the unused default template scene (disabled in build settings).

## Cross-scene data flow

Animal data does not persist via `DontDestroyOnLoad` — it's carried between scenes through a **ScriptableObject asset**, `AnimalRoster` (`Assets/99_Common/Scripts/AnimalRoster.cs`), which both scenes reference in their Inspector. `AnimalData` (`Assets/00_BaseScene/Scripts/Animal/AnimalData.cs`) is the plain serializable record (name, image as base64, stats: speed/power/wisdom/luck/stamina) carried by the roster.

`BaseSceneManager.OnRaceStartButtonClicked()` pulls the loaded animals from `IllustrationManager`, writes them into the `AnimalRoster` asset, and loads `GameScene`. `RaceManager.SelectParticipants()` then draws a random subset of the roster to actually race.

## BaseScene architecture

- **`IllustrationManager`** (`Assets/00_BaseScene/Scripts/IllustrationManagers/`) is the entry point that fetches the list of illustrations and spawns one `AnimalSprite` prefab instance per image. It talks to an `IImageProvider` abstraction so the data source is swappable: `TestImageProvider` reads local files from `Assets/StreamingAssets/TestImages/`, `ServerImageProvider` fetches from a deployed API. The switch is the `isDevelopment` flag on `IllustrationManager` in the scene Inspector — check this before assuming animals will load from the network or not.
- **`AnimalSelectionManager`** mediates the single-selection state: clicking an animal (`AnimalsClickDetector`, via `IPointerClickHandler`/`IPointerEnterHandler`/`IPointerExitHandler` and the scene's `PhysicsRaycaster`) focuses the camera on it (`CameraFocus`), fades out the other animals (`IllustrationManager.MakeTransparentAllImages`), and shows its info panel (`UIManager`).
- **`AnimalActionScheduler`** + **`AnimalIdleAnimation`** drive the idle wandering behavior: each animal randomly rolls between wandering, eating, sleeping, or fighting another animal. The scheduler arbitrates shared resources — feeding/bed spot `Transform`s (assigned in the Inspector, see the `FeedingSpot`/`bedSpot` prefabs) and opponent matching for fights — so two animals don't use the same spot or fight partner simultaneously. Animals set `IsBusy` while mid-action so the scheduler and their own random-action loop skip them.
- **`CameraMover`** is the free-fly camera used while idle (WASD move, QE up/down, right-drag pan, left-drag rotate, scroll zoom, Space to toggle input, P to reset to the default pose). Its movement is clamped every frame to a range derived from the `Floor Reference` Collider's bounds (`_boundsMarginMultiplier`/`_minHeightAboveFloor`/`_maxHeightAboveFloor`), and mouse-drag pitch is clamped (`_minPitchAngle`/`_maxPitchAngle`) to prevent flipping upside down. `CameraFocus` (used during animal selection) disables `CameraMover` while it overrides the camera position, so free-fly and focus-follow never fight each other.
- `Billboard` keeps an animal sprite facing the camera every frame — any code that needs to rotate an animal's `transform` directly (e.g. lying down to sleep) must temporarily disable the animal's `Billboard` component first or the rotation gets overwritten next frame.

## GameScene architecture

- **`RaceManager`** (singleton, `Instance`) is the orchestrator: picks participants from the roster, spawns one racer view per participant, and tracks finish order via `NotifyFinished`.
- **`RaceParticipant`** is the per-racer runtime state; **`RaceTrack`**/**`RaceSimulator`** compute race progress along a 0–1 track-progress value (`Mathf.Clamp01`), not physical movement.
- **`RaceEventBus`** is a static C# event bus (`OnSpurtStarted`, `OnAccidentStarted`, `OnMiracleStarted`, `OnFinished`, `OnStaminaDepleted`, `OnOvertake`) that decouples the simulation from reactive systems — `RaceCameraController` and `RaceCommentator` subscribe to it rather than being called directly by `RaceManager`/`RaceSimulator`. `OnOvertake` is raised by `RaceManager` (margin-based confirmed ranking, on-screen racers only); the others are raised from `AnimalRacerView` state transitions.
- **`RaceCommentator`** (`RaceManager/RaceCommentManager/`) generates commentary text from `CommentTempletes` keyed to race events. Comments flagged `isForce` (miracle, 1st-place finish, and camera cuts it hears about via `RaceCameraController.OnGoalCameraStarted`/`OnChaserShotStarted`) skip the normal 2–3s display interval and interrupt immediately so the text matches what the camera shows.
- **`RaceTuningConfig`** is a `ScriptableObject` holding the race's numeric tuning knobs (speeds, phase thresholds, etc.) — prefer adding new tunable race parameters here over hardcoding them in `RaceManager`/`RaceSimulator`.
- `RaceEntryScreen`/`RaceResultScreen` (under `RaceEntryViewManager`/`RaceResultView`) are the pre-race roster display and post-race results UI.

## Conventions

- Code comments, logs, and UI copy are in Japanese; match this when editing existing files.
- New UGUI buttons in this project are wired via the Inspector's `Button.onClick` persistent calls pointing at a public parameterless method (see `BaseSceneManager.OnRaceStartButtonClicked`) — there's no code-based `AddListener` pattern to follow instead.
- The project's custom `FGUIStarter.CustomButton` (`Assets/maanetorn/Farm Game UI - Simple 2D UI/`) requires a `TextMeshProUGUI` child or it throws a `NullReferenceException` in `Awake()`.
