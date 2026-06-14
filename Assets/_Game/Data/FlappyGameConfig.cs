using UnityEngine;

namespace FlappyClone.Data
{
    /// <summary>
    /// Tunable gameplay parameters, authored as a Unity ScriptableObject.
    ///
    /// WHY a ScriptableObject instead of constants in code:
    ///  - designers can rebalance the game from the Inspector without recompiling;
    ///  - the same <see cref="FlappyClone.Gameplay.FlappyMiniGame"/> can be reused
    ///    with different .asset profiles (easy / hard / seasonal variants);
    ///  - it keeps balance data out of the gameplay logic.
    ///
    /// The single runtime instance is loaded with Resources.Load("FlappyConfig")
    /// inside FlappyMiniGame.Initialize, so the gameplay scene needs ZERO serialized
    /// wiring — everything is built from code at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "FlappyConfig", menuName = "Flappy/Game Config", order = 0)]
    public sealed class FlappyGameConfig : ScriptableObject
    {
        [Header("Bird")]
        [Tooltip("Downward acceleration applied to the bird, expressed as a Rigidbody2D gravity scale.")]
        public float gravityScale = 3.2f;

        [Tooltip("Instant upward velocity (world units/second) applied on every flap.")]
        public float flapVelocity = 6.5f;

        [Tooltip("Fixed horizontal position of the bird; the world scrolls past it.")]
        public float birdX = -2.4f;

        [Tooltip("Radius of the bird collision circle, in world units.")]
        public float birdRadius = 0.28f;

        [Header("Pipes")]
        [Tooltip("World units/second the pipes (and ground) scroll to the left.")]
        public float scrollSpeed = 3.0f;

        [Tooltip("Vertical opening the bird flies through, in world units.")]
        public float pipeGap = 2.6f;

        [Tooltip("Horizontal distance between consecutive pipes, in world units.")]
        public float pipeSpacing = 3.4f;

        [Tooltip("Width of a pipe, in world units.")]
        public float pipeWidth = 1.0f;

        [Tooltip("Half-range of vertical randomisation for the gap centre, in world units.")]
        public float gapVerticalJitter = 1.6f;

        [Header("World bounds (world units from centre)")]
        [Tooltip("Hitting this height kills the bird.")]
        public float ceilingY = 5.0f;

        [Tooltip("Hitting this depth kills the bird.")]
        public float floorY = -4.2f;

        [Header("Colours")]
        [Tooltip("The game ships no art assets: all sprites are solid-colour squares generated at runtime.")]
        public Color birdColor = new Color(0.98f, 0.80f, 0.20f);
        public Color pipeColor = new Color(0.30f, 0.78f, 0.32f);
        public Color backgroundColor = new Color(0.40f, 0.72f, 0.92f);
        public Color groundColor = new Color(0.86f, 0.74f, 0.40f);
    }
}
