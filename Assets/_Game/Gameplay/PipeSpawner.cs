using System.Collections.Generic;
using UnityEngine;
using FlappyClone.Data;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// Spawns, moves, scores and recycles pipe pairs.
    ///
    /// WHY it is driven by FlappyMiniGame.Tick and not by its own Update(): when the
    /// Core leaves PlayingState (pause / OS interrupt) it simply stops calling Tick,
    /// so the pipes freeze automatically with zero extra code. This is the payoff of
    /// routing the whole game loop through IMiniGame.Tick.
    ///
    /// Pipes are pooled (toggled active/inactive) so a long run never allocates.
    /// </summary>
    public sealed class PipeSpawner : MonoBehaviour
    {
        private FlappyGameConfig _config;
        private ScoreTracker _score;
        private float _spawnX;
        private float _despawnX;
        private readonly List<Pipe> _pipes = new List<Pipe>();

        public void Setup(FlappyGameConfig config, ScoreTracker score, float spawnX, float despawnX)
        {
            _config = config;
            _score = score;
            _spawnX = spawnX;
            _despawnX = despawnX;
        }

        /// <summary>Hides every pooled pipe, ready for a fresh run.</summary>
        public void ResetSpawner()
        {
            foreach (var pipe in _pipes)
            {
                pipe.gameObject.SetActive(false);
            }
        }

        public void Tick(float deltaTime)
        {
            float dx = -_config.scrollSpeed * deltaTime;
            float rightmost = float.NegativeInfinity;

            foreach (var pipe in _pipes)
            {
                if (!pipe.gameObject.activeSelf)
                {
                    continue;
                }

                pipe.Move(dx);

                // Score the moment the pipe centre passes the (fixed) bird position.
                if (!pipe.Scored && pipe.X < _config.birdX)
                {
                    pipe.Scored = true;
                    _score.AddPoint();
                }

                if (pipe.X > rightmost)
                {
                    rightmost = pipe.X;
                }

                if (pipe.X < _despawnX)
                {
                    pipe.gameObject.SetActive(false);
                }
            }

            // Spawn a new pair when the field is empty, or when the rightmost pipe has
            // travelled one spacing-length to the left.
            if (rightmost == float.NegativeInfinity || rightmost <= _spawnX - _config.pipeSpacing)
            {
                SpawnPipe();
            }
        }

        private void SpawnPipe()
        {
            var pipe = GetPooledPipe();
            float gapCenter = Random.Range(-_config.gapVerticalJitter, _config.gapVerticalJitter);
            pipe.Place(_spawnX, gapCenter, _config);
            pipe.gameObject.SetActive(true);
        }

        private Pipe GetPooledPipe()
        {
            foreach (var pipe in _pipes)
            {
                if (!pipe.gameObject.activeSelf)
                {
                    return pipe;
                }
            }

            // Pool miss: grow it. This happens only a handful of times at startup.
            var go = new GameObject($"Pipe_{_pipes.Count}");
            go.transform.SetParent(transform, false);
            var created = go.AddComponent<Pipe>();
            created.Build(_config);
            _pipes.Add(created);
            return created;
        }
    }
}
