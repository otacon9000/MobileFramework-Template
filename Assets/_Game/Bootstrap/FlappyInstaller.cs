using UnityEngine;
using MobileFramework.Core.Bootstrap;
using MobileFramework.Core.Contracts;
using MobileFramework.Core.Managers.UI;
using FlappyClone.Gameplay;
using FlappyClone.UI;

namespace FlappyClone.Bootstrap
{
    /// <summary>
    /// The glue between the Core and this game. It lives on the Core scene next to the
    /// CoreBootstrapper and does exactly two things:
    ///   1. builds + registers the game's UI panels so they override the Core slots;
    ///   2. registers the FlappyMiniGame implementation with the CoreBootstrapper.
    ///
    /// WHY everything happens in Awake (not Start): the CoreBootstrapper runs at
    /// [DefaultExecutionOrder(-1000)], so its Awake (which creates the UIManager and
    /// the GameContext) is guaranteed to run BEFORE this Awake. Meanwhile the
    /// CoreBootstrapper drives Boot -> MainMenu from its Start(). Because all Awake
    /// calls finish before any Start, registering the panels here in Awake guarantees
    /// the main-menu panel exists by the time MainMenuState tries to push it.
    ///
    /// REQUIRED PROJECT SETUP — see Assets/_Game/README.md and FlappyMiniGame.cs:
    ///   - add Core.unity (index 0) and FlappyGame.unity (index 1) to Build Settings;
    ///   - set Player > Active Input Handling = "Both" (the Core InputManager uses the
    ///     legacy UnityEngine.Input API).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlappyInstaller : MonoBehaviour
    {
        private void Awake()
        {
            var bootstrapper = FindFirstObjectByType<CoreBootstrapper>();
            if (bootstrapper == null || bootstrapper.Context == null)
            {
                Debug.LogError("[FlappyInstaller] CoreBootstrapper not found or not initialised. " +
                               "Place this component on the Core scene alongside CoreBootstrapper.");
                return;
            }

            var context = bootstrapper.Context;

            // One overlay canvas, kept alive across gameplay scene load/unload so the
            // HUD/menus persist while FlappyGame.unity is added and removed.
            var canvas = FlappyUIFactory.CreateOverlayCanvas("FlappyUICanvas");
            DontDestroyOnLoad(canvas.gameObject);

            // Register each panel. BindSlot -> RegisterPanel keys the panel by its id,
            // and a panel whose id equals a CorePanelIds value transparently replaces
            // the Core default of the same id.
            context.UI.BindSlot(CreatePanel<FlappyMainMenuPanel>("MainMenuPanel", canvas.transform));
            context.UI.BindSlot(CreatePanel<FlappyHUDPanel>("HUDPanel", canvas.transform));
            context.UI.BindSlot(CreatePanel<FlappyPausePanel>("PausePanel", canvas.transform));
            context.UI.BindSlot(CreatePanel<FlappyGameOverPanel>("GameOverPanel", canvas.transform));

            // Register the gameplay contract. The Core will Initialize it when the
            // player leaves the menu (LoadingState).
            bootstrapper.RegisterMiniGame(new FlappyMiniGame());
        }

        // Creates a full-screen RectTransform GameObject and attaches the panel
        // component. The constraint "where T : UIPanel, IUISlot" lets us pass the
        // result straight to UIManager.BindSlot.
        private static T CreatePanel<T>(string name, Transform parent) where T : UIPanel, IUISlot
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<T>();
        }
    }
}
