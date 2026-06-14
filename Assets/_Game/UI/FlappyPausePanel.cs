using UnityEngine;
using MobileFramework.Core.Bootstrap;
using MobileFramework.Core.Contracts;
using MobileFramework.Core.Managers.UI;
using MobileFramework.Core.Managers.UI.Panels;
using MobileFramework.Core.StateMachine;
using MobileFramework.Core.StateMachine.States;

namespace FlappyClone.UI
{
    /// <summary>
    /// Replaces the Core PausePanel.
    ///
    /// In this template the pause screen is reached only via an OS interrupt: the
    /// Core's AppLifecycleHandler -> GameManager.NotifyOSInterrupt -> OSInterruptState
    /// (which calls IMiniGame.PauseGame), then on resume -> PausedState, which pushes
    /// this panel. Tap to resume.
    ///
    /// PausedState disables Core input on entry, so we re-enable it here — otherwise no
    /// tap would ever reach us. We leave it to the next state to manage input again.
    /// </summary>
    public sealed class FlappyPausePanel : PausePanel, IUISlot
    {
        public string SlotId => CorePanelIds.Pause;

        private GameContext _ctx;

        public void OnSlotAttached(GameContext context)
        {
            _ctx = context;
            FlappyUIFactory.CreatePanelBackground(transform, new Color(0f, 0f, 0f, 0.60f));
            FlappyUIFactory.CreateText(transform, "PAUSED", 120, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.50f), new Vector2(1f, 0.70f));
            FlappyUIFactory.CreateText(transform, "tap to resume", 56, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.35f), new Vector2(1f, 0.50f));
        }

        public void OnSlotDetached()
        {
        }

        protected override void OnShow()
        {
            _ctx.Input.InputEnabled = true; // PausedState disabled it on entry
            _ctx.Input.Tapped += OnTapped;
        }

        protected override void OnHide()
        {
            _ctx.Input.Tapped -= OnTapped;
        }

        private void OnTapped(Vector2 screenPosition)
        {
            if (ServiceLocator.Instance.TryGet<GameManager>(out var gameManager)
                && gameManager.CurrentState is PausedState paused)
            {
                paused.Resume();
            }
        }
    }
}
