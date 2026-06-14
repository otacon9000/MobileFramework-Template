# MobileFramework-Template

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-6000.0-black?logo=unity)](https://unity.com/releases/unity-6)
[![Core](https://img.shields.io/badge/MobileFramework--Core-1.0.2-blue)](https://github.com/otacon9000/MobileFramework-Core)

Starter template for Unity mobile games built on **[MobileFramework-Core](https://github.com/otacon9000/MobileFramework-Core)**.
Fork it, swap the gameplay, ship a title — the framework backbone (state machine,
service locator, typed event bus, audio/UI/save/input/haptic/settings managers and
localization) is already wired in.

It ships with a **complete, playable Flappy Bird clone** as a worked reference: a single
`IMiniGame` implementation, a versioned `IGameSaveData` save, struct events on the
`EventBus`, and UI panels that override the Core slots — every architectural choice is
commented in the source.

> **Golden rule of the framework:** *the Core knows nothing about the game; the game
> knows everything about the Core.*

## Screenshot

> _screenshot coming soon_

## Using the template

### 1. Fork this repository

Use the **Fork** button on GitHub (or "Use this template"), then clone your copy and open
it in **Unity 6 (6000.0)**.

### 2. Install the Core via UPM

The Core is referenced as a git dependency in `Packages/manifest.json`:

```json
"com.otaforge.mobileframework": "https://github.com/otacon9000/MobileFramework-Core.git#v1.0.2"
```

It is already declared, so Unity restores it on first open. To add or update it manually:
**Package Manager ▸ Add package from git URL**

```
https://github.com/otacon9000/MobileFramework-Core.git#v1.0.2
```

Repo: **[otacon9000/MobileFramework-Core](https://github.com/otacon9000/MobileFramework-Core)**

### 3. Three manual setup steps

These are intentionally **not** automated so you keep control of project-level settings.

| # | Where | What |
|---|-------|------|
| **Build Profiles** | `File ▸ Build Profiles` (Scene List) | Add `Assets/_Game/Scenes/Core.unity` at **index 0** and `Assets/_Game/Scenes/FlappyGame.unity` at **index 1**. Index 0 must be the Core scene; the gameplay scene is loaded additively by name at runtime. |
| **Input Handling** | `Project Settings ▸ Player ▸ Active Input Handling` | Set to **Both**. The Core `InputManager` reads the legacy `UnityEngine.Input` API; with the new Input System only, taps/swipes throw at runtime and the bird never flaps. |
| **Company / Product Name** | `Project Settings ▸ Player` | Set your own **Company Name** and **Product Name**. They feed `Application.persistentDataPath`, where the `SaveSystem` stores save files — change them **before** your first build, or existing players' saves move location. |

Then open `Core.unity` and press **Play**: `Boot → MainMenu`, tap to play, flap through
the pipes.

## Project structure

| Path | Contents |
|------|----------|
| `Assets/_Game/` | All game-specific code (gameplay, data, events, UI, bootstrap, scenes). The only folder you edit when building your own title. |
| `Assets/Scenes/` | Default Unity scenes from the project template (unused by the game). |
| `Assets/Settings/` | URP render pipeline assets and volume profiles. |
| `Packages/` | `manifest.json` with the Core UPM dependency. |
| `ProjectSettings/` | Unity project settings (Player, Physics, Quality, etc.). |

The `_Game/` folder mirrors the layout recommended by the Core SPEC:

```
Assets/_Game/
├── Bootstrap/      FlappyInstaller — registers panels + the mini-game
├── Data/           FlappyGameConfig (ScriptableObject) + FlappySaveData (IGameSaveData)
├── Events/         struct events on the EventBus
├── Gameplay/       FlappyMiniGame (IMiniGame) + bird / pipes / scoring
├── UI/             panels that override the Core UI slots (IUISlot)
├── Resources/      FlappyConfig.asset (loaded at runtime)
├── Localization/   en.json / it.json (per-game string pattern)
└── Scenes/         Core.unity (index 0) + FlappyGame.unity (index 1)
```

## Documentation

- **[`Assets/_Game/README.md`](Assets/_Game/README.md)** — architectural deep dive: how
  the game implements `IMiniGame`, the EventBus event cycle, how to override Core UI
  panels, the full file map, and ideas for extending your fork.
- **[MobileFramework-Core](https://github.com/otacon9000/MobileFramework-Core)** —
  framework documentation: systems overview, contracts, and versioning.

## License

[MIT](LICENSE) © Otaforge
