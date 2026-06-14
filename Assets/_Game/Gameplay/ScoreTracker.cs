using MobileFramework.Core.Events;
using FlappyClone.Events;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// Owns the running score for a single run.
    ///
    /// WHY a plain C# class (not a MonoBehaviour): it is pure state with no Unity
    /// lifecycle. That makes it trivial to unit-test with a fake IEventBus, and it
    /// keeps scoring logic independent of any GameObject.
    ///
    /// Every increment is broadcast as a <see cref="PipePassedEvent"/>, so the HUD
    /// updates by listening to the bus and never needs a reference to this object.
    /// </summary>
    public sealed class ScoreTracker
    {
        private readonly IEventBus _events;

        public int Current { get; private set; }

        public ScoreTracker(IEventBus events)
        {
            _events = events;
        }

        public void Reset()
        {
            Current = 0;
        }

        public void AddPoint()
        {
            Current++;
            _events.Emit(new PipePassedEvent(Current));
        }
    }
}
