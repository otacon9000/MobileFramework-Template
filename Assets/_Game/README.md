# Flappy Clone — MobileFramework demo game

This `_Game/` folder is a **complete, playable Flappy Bird clone** built on top of
**MobileFramework-Core** (`com.otaforge.mobileframework`, v1.0.2, imported via UPM).
Its job is to be a reference you can fork: every architectural choice is commented in
the source, and this file explains how the pieces fit the framework.

> **Golden rule of the framework:** *the Core knows nothing about the game; the game
> knows everything about the Core.* Nothing in this folder is referenced by the Core —
> it all plugs into Core contracts.

---

## 1. Required one-time project setup

Two manual steps are needed before pressing Play. They are intentionally **not**
automated so you stay in control of project-level settings.

### a) Build Settings — scene order

`File ▸ Build Settings…` and add the two scenes in **exactly** this order:

| Index | Scene | Why |
|------:|-------|-----|
| 0 | `Assets/_Game/Scenes/Core.unity` | Holds `CoreBootstrapper` — must boot first. |
| 1 | `Assets/_Game/Scenes/FlappyGame.unity` | Loaded additively by name at runtime. |

Index 0 **must** be the Core scene. `FlappyMiniGame` loads the gameplay scene by name
(`SceneManager.LoadScene("FlappyGame", Additive)`), so it must be present in the list.

### b) Active Input Handling = "Both"

`Project Settings ▸ Player ▸ Other Settings ▸ Active Input Handling` → **Both**
(changing this restarts the editor).

The Core `InputManager` reads the legacy `UnityEngine.Input` API. With the project's
default of *Input System Package (New)* only, that API throws at runtime and **no taps
or swipes fire** — so the bird never flaps. "Both" keeps the legacy backend alive.

Then open `Core.unity` and press **Play**: `Boot → MainMenu`, tap to play, flap through
the pipes.

---

## 2. The two scenes

- **`Core.unity`** — the always-loaded scene. Contains just two GameObjects:
  - `CoreSystems` with **`CoreBootstrapper`** (the framework entry point; it auto-creates
    the Audio/UI/Input managers and builds the `GameContext`).
  - `FlappyInstaller` with **`FlappyInstaller`** (our glue — see below).
- **`FlappyGame.unity`** — loaded **additively** by `FlappyMiniGame.Initialize` and
  unloaded in `Cleanup`. It only ships a 2D `Main Camera`; the whole world (bird, pipes,
  ground) is built from code so there are no fragile serialized references to wire.

There are **no art or audio assets**: sprites are solid-colour squares generated at
runtime (`SpriteFactory`) and SFX are short procedurally-generated tones
(`FlappyMiniGame.Tone`). Replace these in a fork with real sprites/clips.

---

## 3. How the game plugs into the Core

### IMiniGame — the one contract every game implements
`Gameplay/FlappyMiniGame.cs` is the heart of the integration. The Core state machine
drives its lifecycle (you never call these yourself):

```
LoadingState     → Initialize(GameContext)   load config, additive-load scene, build world
PlayingState     → StartGame()               new run
                 → Tick(deltaTime)           per-frame loop (only while playing)
                 → ResumeGame()              re-entry after a pause
Paused/OSInterrupt → PauseGame()             user pause / phone call / app backgrounded
GameOverState    → EndGame(reason)           persist score via SaveSystem
UnloadingState   → Cleanup()                 destroy world, unload scene, unsubscribe
```

It is a **plain C# class, not a MonoBehaviour**: the Core owns the update loop and calls
`Tick` only in `PlayingState`. That single design choice gives us **free pause** — when
the Core leaves `PlayingState`, it stops ticking us and the whole world freezes. No
`Time.timeScale` juggling, no per-object pause flags.

### GameContext — the only way to reach Core services
`Initialize(GameContext context)` hands us every service as an interface. We cache it and
go through it for everything — never a singleton, never `FindObjectOfType`:

| Service | Used for |
|---------|----------|
| `Events` (`IEventBus`) | all game events (see map below) |
| `Input` (`IInputManager`) | tap = flap, tap = start/resume, swipe-down = menu |
| `Audio` (`IAudioManager`) | flap / score / hit SFX |
| `Haptics` (`IHapticManager`) | failure buzz on death |
| `Save` (`ISaveSystem`) | best score / totals (`FlappySaveData`) |
| `UI` (`IUIManager`) | panel registration (done in `FlappyInstaller`) |
| `Settings` (`ISettingsManager`) | (available; not customised here) |

### IGameSaveData — versioned, migrating save
`Data/FlappySaveData.cs` extends the Core helper `VersionedSaveData`. The `SaveSystem`
stores one checksummed JSON file keyed by `SaveKey` and calls `MigrateFrom` automatically
when the on-disk `DataVersion` is older. This file is at version 2 to demonstrate a real
migration (a field renamed `highScore` → `bestScore`).

### EventBus — every game event is a `struct`
The bus enforces `where T : struct` (allocation-free dispatch). Our events live in
`Events/`:

| Event | Emitted by | Consumed by | Purpose |
|-------|-----------|-------------|---------|
| `BirdFlappedEvent` | `BirdController` | `FlappyMiniGame` (flap SFX) | a flap happened |
| `PipePassedEvent` | `ScoreTracker` | `FlappyHUDPanel` (score), `FlappyMiniGame` (score SFX) | score increased |
| `BirdDiedEvent` | `BirdController` | `FlappyMiniGame` | physical death |
| `GameOverEvent` *(Core)* | `FlappyMiniGame` | `GameManager` | end the run → `GameOverState` |

Note the deliberate split: gameplay (`BirdController`) only reports `BirdDiedEvent` on the
bus and stays unaware of the state machine. `FlappyMiniGame` turns that into the Core's
`GameOverEvent`, which is what actually drives `GameOverState`.

### UIPanel + IUISlot — overriding Core panels
The Core ships no canvas; a game must provide its panels. Each panel in `UI/` extends a
Core `UIPanel` (or a Core panel subclass) **and** implements `IUISlot` with a `SlotId`
equal to a `CorePanelIds` value:

| Panel | Extends | Replaces slot |
|-------|---------|---------------|
| `FlappyMainMenuPanel` | `UIPanel` | `CorePanelIds.MainMenu` |
| `FlappyHUDPanel` | `HUDPanel` | `CorePanelIds.HUD` |
| `FlappyPausePanel` | `PausePanel` | `CorePanelIds.Pause` |
| `FlappyGameOverPanel` | `GameOverPanel` | `CorePanelIds.GameOver` |

`FlappyInstaller` builds a single overlay canvas, instantiates the panels and calls
`UIManager.BindSlot(panel)`. Because the panel's id matches a Core slot, the `UIManager`
shows our panel wherever the Core (or a Core state) pushes that id. The panels build their
visuals from code via `FlappyUIFactory` and drive interaction through the Core
`InputManager` (so no `EventSystem` is needed).

### FlappyInstaller — the glue, and why it runs in Awake
`Bootstrap/FlappyInstaller.cs` registers the panels and the mini-game. It does this in
`Awake`: `CoreBootstrapper` has `[DefaultExecutionOrder(-1000)]`, so its `Awake` (which
builds the `GameContext`) runs first, while it kicks off `Boot → MainMenu` from `Start`.
Since all `Awake`s run before any `Start`, registering in `Awake` guarantees the main-menu
panel exists by the time `MainMenuState` pushes it.

---

## 4. Play flow

```
Boot ─► MainMenu ─(tap)─► Loading ─► Playing ─(crash)─► GameOver ─(tap)──► Loading ─► …
   ▲                                    │                   └─(swipe down)─► Unloading ─► MainMenu
   └──────────────────────────── Unloading ◄───────────────────────────────────────────┘
OS interrupt while Playing ─► OSInterrupt ─(app resumes)─► Paused ─(tap)─► Playing
```

---

## 5. Localization

`Localization/en.json` and `Localization/it.json` follow the framework's per-game string
convention from the Core SPEC. They are included as a **pattern reference**: the UI uses
hard-coded English strings, because Core 1.0.2's `LocalizationManager` loads only the
Core's own `Resources/Localization`. Merging a game's strings is an extension point — once
your Core build supports it, swap the literals in the panels for `Loc.Get("flappy_title")`
etc. The matching keys are already present in these JSON files.

---

## 6. File map

```
_Game/
├── FlappyClone.asmdef         → references MobileFramework.Core + UnityEngine.UI
├── Bootstrap/
│   └── FlappyInstaller.cs      registers panels + the mini-game (runs on Core.unity)
├── Data/
│   ├── FlappyGameConfig.cs     ScriptableObject of tunables
│   └── FlappySaveData.cs       IGameSaveData (versioned + migration)
├── Events/
│   ├── BirdFlappedEvent.cs     struct
│   ├── PipePassedEvent.cs      struct
│   └── BirdDiedEvent.cs        struct (+ DeathCause enum)
├── Gameplay/
│   ├── FlappyMiniGame.cs       IMiniGame — the integration core
│   ├── BirdController.cs       Rigidbody2D bird
│   ├── Pipe.cs                 one pooled pipe pair
│   ├── PipeSpawner.cs          spawn / move / score / recycle
│   ├── ScrollingBackground.cs  looping ground + sky
│   ├── ScoreTracker.cs         score state → PipePassedEvent
│   └── SpriteFactory.cs        runtime solid-colour sprites
├── UI/
│   ├── FlappyUIFactory.cs      builds uGUI from code
│   ├── FlappyMainMenuPanel.cs  : UIPanel        (MainMenu slot)
│   ├── FlappyHUDPanel.cs       : HUDPanel       (HUD slot)
│   ├── FlappyPausePanel.cs     : PausePanel     (Pause slot)
│   └── FlappyGameOverPanel.cs  : GameOverPanel  (GameOver slot)
├── Resources/
│   └── FlappyConfig.asset       loaded via Resources.Load("FlappyConfig")
├── Localization/                en.json, it.json (pattern reference)
└── Scenes/
    ├── Core.unity               build index 0
    └── FlappyGame.unity         build index 1
```

---

## 7. Ideas for extending the fork

- Drop in real sprites/audio: replace `SpriteFactory` calls and add `AudioClip` fields to
  `FlappyGameConfig`, played through `_ctx.Audio`.
- Add a `TutorialState` (extend `GameState`, register it on the `GameManager`, add a rule
  via `StateTransitionValidator.AddRule`) for a first-run "tap to flap" overlay.
- Add a `SkinSelectorPanel : UIPanel` (a pure game panel, no Core slot) and store the
  chosen skin in `FlappySaveData`.
- Wire a rewarded-ad "continue" using the Core `RewardedState.CompleteReward(true)`.
