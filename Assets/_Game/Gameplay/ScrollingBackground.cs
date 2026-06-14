using UnityEngine;
using FlappyClone.Data;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// Cosmetic scrolling ground plus a static sky backdrop. Two ground tiles are
    /// leap-frogged leftward to fake an infinite floor. Like everything else it is
    /// driven by FlappyMiniGame.Tick, so it pauses with the rest of the game.
    ///
    /// The ground tiles also carry trigger colliders: touching them kills the bird,
    /// exactly like a pipe.
    /// </summary>
    public sealed class ScrollingBackground : MonoBehaviour
    {
        private FlappyGameConfig _config;
        private Transform _tileA;
        private Transform _tileB;
        private float _tileWidth;

        public void Setup(FlappyGameConfig config, float leftX, float rightX)
        {
            _config = config;
            _tileWidth = (rightX - leftX) + 2f;

            // Sky: a single large quad behind everything (sorting order 0, z pushed back).
            var sky = SpriteFactory.CreateSprite("Sky", config.backgroundColor, transform, 0);
            sky.transform.localScale = new Vector3(_tileWidth + 4f, config.ceilingY * 2.4f, 1f);
            sky.transform.position = new Vector3(0f, 0f, 1f);

            _tileA = CreateGroundTile("GroundA");
            _tileB = CreateGroundTile("GroundB");
            _tileA.position = new Vector3(0f, _config.floorY - 0.5f, 0f);
            _tileB.position = new Vector3(_tileWidth, _config.floorY - 0.5f, 0f);
        }

        private Transform CreateGroundTile(string name)
        {
            var tile = SpriteFactory.CreateSprite(name, _config.groundColor, transform, 8);
            tile.transform.localScale = new Vector3(_tileWidth, 1.2f, 1f);

            var collider = tile.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return tile.transform;
        }

        public void Tick(float deltaTime)
        {
            float dx = -_config.scrollSpeed * deltaTime;
            _tileA.position += new Vector3(dx, 0f, 0f);
            _tileB.position += new Vector3(dx, 0f, 0f);

            Recycle(_tileA);
            Recycle(_tileB);
        }

        private void Recycle(Transform tile)
        {
            // When a tile scrolls fully off the left edge, jump it two widths to the
            // right so the pair forms a seamless loop.
            if (tile.position.x <= -_tileWidth)
            {
                tile.position += new Vector3(_tileWidth * 2f, 0f, 0f);
            }
        }
    }
}
