using UnityEngine;
using FlappyClone.Data;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// A single top+bottom pipe pair, built entirely from code. The two halves use
    /// trigger BoxCollider2D so the bird's OnTriggerEnter2D detects a crash. The pair
    /// also remembers whether the bird already cleared it, so <see cref="PipeSpawner"/>
    /// awards exactly one point per pipe.
    ///
    /// Pipes are pooled by the spawner (enabled/disabled, never destroyed during a
    /// run) to avoid per-spawn allocations and GC spikes on low-end phones.
    /// </summary>
    public sealed class Pipe : MonoBehaviour
    {
        private const float PipeHeight = 12f; // each half is a tall square, clipped by the camera

        private Transform _top;
        private Transform _bottom;

        /// <summary>Set once the bird has passed this pipe, so it is scored only once.</summary>
        public bool Scored { get; set; }

        public float X => transform.position.x;

        /// <summary>Builds the two halves. Called once when the pool grows.</summary>
        public void Build(FlappyGameConfig config)
        {
            _top = CreateHalf("Top", config).transform;
            _bottom = CreateHalf("Bottom", config).transform;
        }

        private GameObject CreateHalf(string halfName, FlappyGameConfig config)
        {
            var half = SpriteFactory.CreateSprite(halfName, config.pipeColor, transform, 5);
            half.transform.localScale = new Vector3(config.pipeWidth, PipeHeight, 1f);

            var collider = half.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return half;
        }

        /// <summary>Positions the whole pair: horizontal x and vertical gap centre.</summary>
        public void Place(float x, float gapCenterY, FlappyGameConfig config)
        {
            transform.position = new Vector3(x, gapCenterY, 0f);
            Scored = false;

            float halfGap = config.pipeGap * 0.5f;
            _top.localPosition = new Vector3(0f, halfGap + PipeHeight * 0.5f, 0f);
            _bottom.localPosition = new Vector3(0f, -halfGap - PipeHeight * 0.5f, 0f);
        }

        public void Move(float dx)
        {
            transform.position += new Vector3(dx, 0f, 0f);
        }
    }
}
