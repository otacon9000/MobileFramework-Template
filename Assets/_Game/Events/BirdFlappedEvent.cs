using UnityEngine;

namespace FlappyClone.Events
{
    /// <summary>
    /// Raised every time the player makes the bird flap.
    ///
    /// It is a <c>readonly struct</c> because the Core EventBus constrains every
    /// event to <c>where T : struct</c> — this is the framework rule that keeps event
    /// dispatch allocation-free (no garbage per event). Consumers in this template:
    /// the flap sound effect. A fork could add particle/feather VFX with zero changes
    /// to gameplay code, just by subscribing to this event.
    /// </summary>
    public readonly struct BirdFlappedEvent
    {
        public readonly Vector2 WorldPosition;

        public BirdFlappedEvent(Vector2 worldPosition)
        {
            WorldPosition = worldPosition;
        }
    }
}
