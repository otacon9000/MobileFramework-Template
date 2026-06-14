namespace FlappyClone.Events
{
    /// <summary>How the bird met its end. Useful for picking a death animation/sound.</summary>
    public enum DeathCause
    {
        Pipe,
        Ground,
        Ceiling
    }

    /// <summary>
    /// Raised the instant the bird collides with a pipe, the ground or the ceiling.
    /// This is the *physical* death signal, emitted by <see cref="FlappyClone.Gameplay.BirdController"/>.
    ///
    /// IMPORTANT DESIGN NOTE: this is NOT the same as ending the game. FlappyMiniGame
    /// subscribes to this event and translates it into the Core's GameOverEvent, which
    /// is what actually drives the state machine into GameOverState. Keeping the two
    /// separate means the gameplay code (BirdController) stays completely unaware of
    /// the Core state machine — it just reports "I died" on the bus.
    /// </summary>
    public readonly struct BirdDiedEvent
    {
        public readonly DeathCause Cause;

        public BirdDiedEvent(DeathCause cause)
        {
            Cause = cause;
        }
    }
}
