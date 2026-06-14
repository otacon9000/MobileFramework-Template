using UnityEngine;
using MobileFramework.Core.Bootstrap;
using MobileFramework.Core.Contracts;
using MobileFramework.Core.Managers.UI;
using MobileFramework.Core.StateMachine;
using MobileFramework.Core.StateMachine.States;

namespace FlappyClone.UI
{
    /// <summary>
    /// Replaces the Core's main-menu slot.
    ///
    /// HOW the override works: the panel extends <see cref="UIPanel"/> and implements
    /// <see cref="IUISlot"/>, exposing SlotId == CorePanelIds.MainMenu. When the
    /// installer calls UIManager.BindSlot(this), the UIManager registers it under that
    /// id; so when MainMenuState does Push(CorePanelIds.MainMenu), THIS panel appears
    /// instead of the Core default. This is the framework's intended way for a game to
    /// override a built-in panel.
    ///
    /// Interaction uses the Core InputManager (tap anywhere) rather than a uGUI Button,
    /// to showcase IInputManager and avoid needing an EventSystem in the scene.
    /// </summary>
    public sealed class FlappyMainMenuPanel : UIPanel, IUISlot
    {
        // Make the panel's id equal the Core slot it replaces.
        protected override string DefaultPanelId => CorePanelIds.MainMenu;
        public string SlotId => CorePanelIds.MainMenu;

        private GameContext _ctx;

        public void OnSlotAttached(GameContext context)
        {
            _ctx = context;
            BuildView();
        }

        public void OnSlotDetached()
        {
        }

        private void BuildView()
        {
            FlappyUIFactory.CreatePanelBackground(transform, new Color(0.10f, 0.20f, 0.30f, 0.85f));

            // NOTE: strings are hard-coded English on purpose. To localise, swap these
            // for Loc.Get("flappy_title") etc. and merge Assets/_Game/Localization/*.json
            // into the Core LocalizationManager — that merge is an extension point the
            // Core 1.0.2 does not implement (see Assets/_Game/README.md).
            FlappyUIFactory.CreateText(transform, "FLAPPY", 140, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.55f), new Vector2(1f, 0.80f));
            FlappyUIFactory.CreateText(transform, "tap to play", 60, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.35f), new Vector2(1f, 0.50f));
        }

        // Subscribe to input only while the menu is actually on screen. OnShow/OnHide
        // are the UIPanel hooks the UIManager calls on Push/Pop.
        protected override void OnShow()
        {
            _ctx.Input.Tapped += OnTapped;
        }

        protected override void OnHide()
        {
            _ctx.Input.Tapped -= OnTapped;
        }

        private void OnTapped(Vector2 screenPosition)
        {
            // Ask the current state to start a game. We reach the GameManager through
            // the Core ServiceLocator, mirroring how the Core's own panels do it.
            if (ServiceLocator.Instance.TryGet<GameManager>(out var gameManager)
                && gameManager.CurrentState is MainMenuState menu)
            {
                menu.StartGame();
            }
        }
    }
}
