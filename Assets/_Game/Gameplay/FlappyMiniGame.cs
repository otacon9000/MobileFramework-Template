using UnityEngine;
using UnityEngine.SceneManagement;
using MobileFramework.Core.Bootstrap;
using MobileFramework.Core.Contracts;
using MobileFramework.Core.Events;
using MobileFramework.Core.Managers.Haptic;
using FlappyClone.Data;
using FlappyClone.Events;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// THE most important file in the game: the single implementation of the Core
    /// contract <see cref="IMiniGame"/>. The Core state machine drives this lifecycle
    /// (see MobileFramework.Core states):
    ///
    ///   LoadingState   -> Initialize(GameContext)   load config, build the world
    ///   PlayingState   -> StartGame()               first entry of a run
    ///                   -> Tick(dt)                  every frame while playing
    ///                   -> ResumeGame()              re-entry after a pause
    ///   PausedState /
    ///   OSInterruptState -> PauseGame()             user pause / phone call / app backgrounded
    ///   GameOverState  -> EndGame(reason)           persist the score
    ///   UnloadingState -> Cleanup()                 free everything, ready to re-init
    ///
    /// WHY it is a PLAIN C# class (not a MonoBehaviour): the Core owns the update loop
    /// and calls Tick for us only while in PlayingState. Routing the whole game loop
    /// through Tick means pause/resume comes for free — the world simply stops
    /// updating when the Core stops ticking us. It also makes the game unit-testable
    /// with the framework's Fake* services.
    ///
    /// HOW Core services are reached: everything goes through the <see cref="GameContext"/>
    /// handed to <see cref="Initialize"/> — Events, Input, Audio, Haptics, Save, UI,
    /// Settings. We never use a singleton or FindObjectOfType to reach the framework.
    ///
    /// ─────────────────────────────────────────────────────────────────────────────
    /// REQUIRED PROJECT SETUP (manual, one-time — see Assets/_Game/README.md):
    ///
    ///  1. BUILD SETTINGS — File > Build Settings, add the two scenes in THIS order:
    ///         index 0: Assets/_Game/Scenes/Core.unity        (has CoreBootstrapper)
    ///         index 1: Assets/_Game/Scenes/FlappyGame.unity  (loaded below by name)
    ///     Index 0 MUST be the Core scene; this class loads the gameplay scene by
    ///     name (it must be present in the build list for LoadScene to find it).
    ///
    ///  2. INPUT — Project Settings > Player > Active Input Handling = "Both".
    ///     The Core InputManager reads UnityEngine.Input (legacy), so taps/swipes
    ///     only fire if the legacy input backend is enabled.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public sealed class FlappyMiniGame : IMiniGame
    {
        private const string GameplaySceneName = "FlappyGame";
        private const string ConfigResourcePath = "FlappyConfig"; // Assets/_Game/Resources/FlappyConfig.asset

        /// <summary>Unique title id used by the Core (e.g. for save namespacing / logging).</summary>
        public string GameId => "flappy_clone";

        // The Core service bundle, captured in Initialize and used everywhere.
        private GameContext _ctx;
        private FlappyGameConfig _config;

        // Runtime world (rebuilt every session, all parented under one root).
        private GameObject _worldRoot;
        private Scene _gameplayScene;
        private BirdController _bird;
        private PipeSpawner _pipes;
        private ScrollingBackground _background;
        private ScoreTracker _score;
        private FlappySaveData _save;

        // Procedurally generated SFX (no audio assets shipped). Generated once.
        private AudioClip _flapSfx;
        private AudioClip _scoreSfx;
        private AudioClip _hitSfx;

        // True only between StartGame/ResumeGame and a pause or death. Gates Tick and
        // input so stray taps after death do nothing.
        private bool _running;

        // ── IMiniGame lifecycle ──────────────────────────────────────────────────

        public void Initialize(GameContext context)
        {
            _ctx = context;

            // A "Retry" from the game-over screen re-enters LoadingState WITHOUT going
            // through UnloadingState, so Initialize can run again on the same instance.
            // Make it idempotent: tear down anything left from the previous run first.
            UnsubscribeAll();
            DestroyWorld();

            // 1. Load tunables via Resources so the gameplay scene needs no wiring.
            _config = Resources.Load<FlappyGameConfig>(ConfigResourcePath);
            if (_config == null)
            {
                Debug.LogWarning("[FlappyMiniGame] FlappyConfig not found under a Resources folder; using code defaults.");
                _config = ScriptableObject.CreateInstance<FlappyGameConfig>();
            }

            // 2. Load the gameplay scene additively (the Core scene stays loaded).
            //    On Retry the scene is still loaded, so we reuse it instead of stacking
            //    a second copy.
            var existing = SceneManager.GetSceneByName(GameplaySceneName);
            if (!existing.IsValid() || !existing.isLoaded)
            {
                SceneManager.LoadScene(GameplaySceneName, LoadSceneMode.Additive);
            }
            _gameplayScene = SceneManager.GetSceneByName(GameplaySceneName);

            // 3. Make sure there is a 2D camera framed for play.
            EnsureCamera();

            // 4. Build the world from code under a single root (so Cleanup is one Destroy).
            _worldRoot = new GameObject("FlappyWorld");
            if (_gameplayScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(_worldRoot, _gameplayScene);
            }

            float halfWidth = GetCameraHalfWidth();
            float spawnX = halfWidth + _config.pipeWidth;     // just off the right edge
            float despawnX = -halfWidth - _config.pipeWidth;  // just off the left edge

            _background = new GameObject("Background").AddComponent<ScrollingBackground>();
            _background.transform.SetParent(_worldRoot.transform, false);
            _background.Setup(_config, despawnX, spawnX);

            var birdGo = new GameObject("Bird");
            birdGo.transform.SetParent(_worldRoot.transform, false);
            _bird = birdGo.AddComponent<BirdController>();
            _bird.Setup(_config, _ctx.Events);

            _pipes = new GameObject("Pipes").AddComponent<PipeSpawner>();
            _pipes.transform.SetParent(_worldRoot.transform, false);
            _score = new ScoreTracker(_ctx.Events);
            _pipes.Setup(_config, _score, spawnX, despawnX);

            // 5. One-time procedural SFX.
            if (_flapSfx == null)
            {
                _flapSfx = Tone(660f, 0.08f);
                _scoreSfx = Tone(880f, 0.10f);
                _hitSfx = Tone(140f, 0.25f);
            }

            // 6. Load persisted progress through the Core SaveSystem.
            _save = _ctx.Save.Load<FlappySaveData>();

            // 7. Wire input + gameplay events. Tap = flap; the bus carries the score
            //    and the death notification.
            SubscribeAll();
        }

        public void StartGame()
        {
            _score.Reset();
            _bird.ResetBird();
            _pipes.ResetSpawner();
            _running = true;
        }

        public void PauseGame()
        {
            _running = false;
            _bird.SetPaused(true);
        }

        public void ResumeGame()
        {
            _bird.SetPaused(false);
            _running = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_running)
            {
                return;
            }

            _bird.Tick(deltaTime);
            _pipes.Tick(deltaTime);
            _background.Tick(deltaTime);
        }

        public void EndGame(GameOverReason reason)
        {
            _running = false;

            // Persist progress. Save<T> writes one versioned, checksummed file keyed by
            // FlappySaveData.SaveKey. The game-over panel reads it straight back.
            _save.totalGames++;
            _save.lastScore = _score.Current;
            if (_score.Current > _save.bestScore)
            {
                _save.bestScore = _score.Current;
            }
            _ctx.Save.Save(_save);
        }

        public void Cleanup()
        {
            // The Core clears the EventBus in UnloadingState anyway, but we still
            // unsubscribe input + null our refs so re-initialisation stays leak-free.
            UnsubscribeAll();
            DestroyWorld();

            if (_gameplayScene.IsValid() && _gameplayScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_gameplayScene);
            }

            _running = false;
        }

        // ── Event / input handlers ───────────────────────────────────────────────

        private void OnTapped(Vector2 screenPosition)
        {
            if (!_running)
            {
                return;
            }

            _bird.Flap();
            _ctx.Audio.PlaySfx(_flapSfx);
        }

        private void OnPipePassed(PipePassedEvent evt)
        {
            _ctx.Audio.PlaySfx(_scoreSfx);
        }

        private void OnBirdDied(BirdDiedEvent evt)
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _ctx.Audio.PlaySfx(_hitSfx);
            _ctx.Haptics.Play(HapticFeedbackType.Failure);

            // Translate the gameplay death into the Core's GameOverEvent. The
            // GameManager listens for it and transitions us into GameOverState, which
            // calls EndGame(reason) above. This indirection keeps gameplay code unaware
            // of the Core state machine.
            _ctx.Events.Emit(new GameOverEvent(GameOverReason.Lose, _score.Current));
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void SubscribeAll()
        {
            _ctx.Input.Tapped += OnTapped;
            _ctx.Events.Subscribe<PipePassedEvent>(OnPipePassed);
            _ctx.Events.Subscribe<BirdDiedEvent>(OnBirdDied);
        }

        private void UnsubscribeAll()
        {
            if (_ctx == null)
            {
                return;
            }

            _ctx.Input.Tapped -= OnTapped;
            _ctx.Events.Unsubscribe<PipePassedEvent>(OnPipePassed);
            _ctx.Events.Unsubscribe<BirdDiedEvent>(OnBirdDied);
        }

        private void DestroyWorld()
        {
            if (_worldRoot != null)
            {
                Object.Destroy(_worldRoot);
                _worldRoot = null;
            }
        }

        private void EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                // Fallback: the FlappyGame scene normally ships a Main Camera, but build
                // one if it is missing so the template still runs.
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                if (_gameplayScene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(camGo, _gameplayScene);
                }
            }

            camera.orthographic = true;
            camera.orthographicSize = _config.ceilingY; // vertical half-size in world units
            camera.backgroundColor = _config.backgroundColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static float GetCameraHalfWidth()
        {
            var camera = Camera.main;
            float halfHeight = camera != null ? camera.orthographicSize : 5f;
            float aspect = camera != null ? camera.aspect : (9f / 16f);
            return halfHeight * aspect;
        }

        /// <summary>
        /// Builds a short sine-wave AudioClip so the demo has audible feedback without
        /// shipping .wav files. A fork would replace these with real clips assigned in
        /// the config and played via _ctx.Audio.PlaySfx.
        /// </summary>
        private static AudioClip Tone(float frequency, float duration)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / sampleCount; // fade out to avoid a click
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * envelope;
            }

            var clip = AudioClip.Create($"tone_{frequency:F0}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
