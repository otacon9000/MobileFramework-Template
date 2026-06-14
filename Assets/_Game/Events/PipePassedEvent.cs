namespace FlappyClone.Events
{
    /// <summary>
    /// Raised when the bird clears a pipe and the score increases. It carries the new
    /// running score so the HUD can update by reacting to the event, without ever
    /// reaching into gameplay objects. This is the core idea of the EventBus: the
    /// producer (ScoreTracker) and the consumer (FlappyHUDPanel) never reference each
    /// other.
    /// </summary>
    public readonly struct PipePassedEvent
    {
        public readonly int Score;

        public PipePassedEvent(int score)
        {
            Score = score;
        }
    }
}
