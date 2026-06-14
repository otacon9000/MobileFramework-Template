using UnityEngine;
using MobileFramework.Core.Events;
using FlappyClone.Data;
using FlappyClone.Events;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// The bird.
    ///
    /// WHY a MonoBehaviour: it needs Unity 2D physics (Rigidbody2D) for the gravity
    /// + flap arc and trigger-based collision. It is deliberately ignorant of the
    /// Core state machine AND of scoring: it only flaps when told, falls under
    /// gravity, and reports its own death on the EventBus. The higher-level rules
    /// live in <see cref="FlappyMiniGame"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class BirdController : MonoBehaviour
    {
        private FlappyGameConfig _config;
        private IEventBus _events;
        private Rigidbody2D _body;

        public bool IsAlive { get; private set; }

        /// <summary>
        /// Builds the bird's physics + visuals from code (no prefab, no art asset).
        /// Called once by FlappyMiniGame right after the gameplay scene is loaded.
        /// </summary>
        public void Setup(FlappyGameConfig config, IEventBus events)
        {
            _config = config;
            _events = events;

            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = config.gravityScale;
            _body.freezeRotation = true;
            _body.bodyType = RigidbodyType2D.Dynamic;

            var circle = GetComponent<CircleCollider2D>();
            circle.radius = 0.5f; // unit-square sprite is scaled below; radius is in local space
            // The bird is the ONLY non-trigger collider; pipes and ground are triggers,
            // so OnTriggerEnter2D fires on the bird and we never need physical bounce.

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.UnitSquare();
            renderer.color = config.birdColor;
            renderer.sortingOrder = 10;

            float diameter = config.birdRadius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
            transform.position = new Vector3(config.birdX, 0f, 0f);
        }

        /// <summary>Resets the bird to its starting state for a new run.</summary>
        public void ResetBird()
        {
            IsAlive = true;
            _body.simulated = true;
            _body.linearVelocity = Vector2.zero;
            _body.position = new Vector2(_config.birdX, 0f);
            transform.rotation = Quaternion.identity;
        }

        /// <summary>Applies an upward impulse. Called from FlappyMiniGame on a tap.</summary>
        public void Flap()
        {
            if (!IsAlive)
            {
                return;
            }

            _body.linearVelocity = new Vector2(0f, _config.flapVelocity);
            _events.Emit(new BirdFlappedEvent(transform.position));
        }

        /// <summary>Freezes/unfreezes physics when the Core pauses/resumes the session.</summary>
        public void SetPaused(bool paused)
        {
            // Disabling simulation stops gravity from accumulating velocity while the
            // game sits in PausedState/OSInterruptState; ResumeGame re-enables it.
            _body.simulated = !paused && IsAlive;
        }

        /// <summary>
        /// Per-frame update, driven by FlappyMiniGame.Tick (NOT by Unity's Update),
        /// so it only runs while the Core is in PlayingState.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            // Cosmetic: tilt the bird toward its vertical velocity.
            float angle = Mathf.Clamp(_body.linearVelocity.y * 6f, -75f, 35f);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Out-of-bounds is a death condition too, in case the bird outruns the
            // ground/ceiling colliders at high speed.
            if (transform.position.y >= _config.ceilingY)
            {
                Die(DeathCause.Ceiling);
            }
            else if (transform.position.y <= _config.floorY)
            {
                Die(DeathCause.Ground);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAlive)
            {
                return;
            }

            // A pipe pair carries a Pipe component on its root; anything else that is a
            // trigger here is the ground.
            var cause = other.GetComponentInParent<Pipe>() != null ? DeathCause.Pipe : DeathCause.Ground;
            Die(cause);
        }

        private void Die(DeathCause cause)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            _body.simulated = false;
            _events.Emit(new BirdDiedEvent(cause));
        }
    }
}
