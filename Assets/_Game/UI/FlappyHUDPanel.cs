using UnityEngine;
using UnityEngine.UI;
using MobileFramework.Core.Bootstrap;
using MobileFramework.Core.Contracts;
using MobileFramework.Core.Managers.UI;
using MobileFramework.Core.Managers.UI.Panels;
using FlappyClone.Events;

namespace FlappyClone.UI
{
    /// <summary>
    /// Replaces the Core HUDPanel during gameplay.
    ///
    /// It extends <see cref="HUDPanel"/> (which extends UIPanel) — so it inherits the
    /// Core's pause hook — and implements <see cref="IUISlot"/> to claim
    /// CorePanelIds.HUD. It shows the live score purely by subscribing to the game's
    /// <see cref="PipePassedEvent"/>: it never touches gameplay objects, only the bus.
    /// That is the whole point of the EventBus — the HUD and the gameplay are fully
    /// decoupled.
    /// </summary>
    public sealed class FlappyHUDPanel : HUDPanel, IUISlot
    {
        public string SlotId => CorePanelIds.HUD;

        private GameContext _ctx;
        private Text _scoreText;

        public void OnSlotAttached(GameContext context)
        {
            _ctx = context;
            _scoreText = FlappyUIFactory.CreateText(transform, "0", 110, TextAnchor.UpperCenter,
                new Vector2(0f, 0.80f), new Vector2(1f, 0.95f));
        }

        public void OnSlotDetached()
        {
        }

        protected override void OnShow()
        {
            if (_scoreText != null)
            {
                _scoreText.text = "0";
            }

            _ctx.Events.Subscribe<PipePassedEvent>(OnPipePassed);
        }

        protected override void OnHide()
        {
            _ctx.Events.Unsubscribe<PipePassedEvent>(OnPipePassed);
        }

        private void OnPipePassed(PipePassedEvent evt)
        {
            if (_scoreText != null)
            {
                _scoreText.text = evt.Score.ToString();
            }
        }
    }
}
