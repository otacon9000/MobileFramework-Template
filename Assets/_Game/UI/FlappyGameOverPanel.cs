using UnityEngine;
using UnityEngine.UI;
using MobileFramework.Core.Bootstrap;
using MobileFramework.Core.Contracts;
using MobileFramework.Core.Managers.Input;
using MobileFramework.Core.Managers.UI;
using MobileFramework.Core.Managers.UI.Panels;
using MobileFramework.Core.StateMachine;
using MobileFramework.Core.StateMachine.States;
using FlappyClone.Data;

namespace FlappyClone.UI
{
    /// <summary>
    /// Replaces the Core GameOverPanel.
    ///
    /// It shows the run score and the persisted best, which it reads back through the
    /// Core SaveSystem — the very same <see cref="FlappySaveData"/> the game wrote in
    /// IMiniGame.EndGame. This demonstrates the full save round-trip.
    ///
    /// Controls (via the Core InputManager): tap = retry, swipe down = back to menu.
    /// GameOverState disables input on entry, so OnShow re-enables it.
    /// </summary>
    public sealed class FlappyGameOverPanel : GameOverPanel, IUISlot
    {
        public string SlotId => CorePanelIds.GameOver;

        private GameContext _ctx;
        private Text _scoreText;

        public void OnSlotAttached(GameContext context)
        {
            _ctx = context;
            FlappyUIFactory.CreatePanelBackground(transform, new Color(0.10f, 0.10f, 0.15f, 0.85f));
            FlappyUIFactory.CreateText(transform, "GAME OVER", 110, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.62f), new Vector2(1f, 0.82f));
            _scoreText = FlappyUIFactory.CreateText(transform, "", 64, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.40f), new Vector2(1f, 0.60f));
            FlappyUIFactory.CreateText(transform, "tap to retry   -   swipe down for menu", 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.22f), new Vector2(1f, 0.34f));
        }

        public void OnSlotDetached()
        {
        }

        protected override void OnShow()
        {
            // EndGame already persisted the result; load it back for display.
            var save = _ctx.Save.Load<FlappySaveData>();
            _scoreText.text = $"Score  {save.lastScore}\nBest  {save.bestScore}";

            _ctx.Input.InputEnabled = true; // GameOverState disabled it on entry
            _ctx.Input.Tapped += OnTapped;
            _ctx.Input.Swiped += OnSwiped;
        }

        protected override void OnHide()
        {
            _ctx.Input.Tapped -= OnTapped;
            _ctx.Input.Swiped -= OnSwiped;
        }

        private void OnTapped(Vector2 screenPosition)
        {
            // GameOverState.Retry() runs another LoadingState -> PlayingState cycle.
            if (ServiceLocator.Instance.TryGet<GameManager>(out var gameManager)
                && gameManager.CurrentState is GameOverState gameOver)
            {
                gameOver.Retry();
            }
        }

        private void OnSwiped(SwipeDirection direction, Vector2 origin)
        {
            if (direction != SwipeDirection.Down)
            {
                return;
            }

            // GameOverState.GoHome() runs UnloadingState -> MainMenuState.
            if (ServiceLocator.Instance.TryGet<GameManager>(out var gameManager)
                && gameManager.CurrentState is GameOverState gameOver)
            {
                gameOver.GoHome();
            }
        }
    }
}
