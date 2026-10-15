using UnityEngine;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Picks the deminer's front or back sprite from the facing direction (mirrored for left),
    /// and gives walking a two-step bob. Runs after PlayerController, so it also owns flipX.
    /// </summary>
    public class DeminerSprites : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer body;
        [SerializeField, HideInInspector] private Sprite front;
        [SerializeField, HideInInspector] private Sprite back;
        [SerializeField] private float stepSeconds = 0.16f;

        private bool flip;
        private Vector3 lastPos;
        private float walkTimer;

        public void Configure(PlayerController playerController, SpriteRenderer renderer, Sprite frontSprite, Sprite backSprite)
        {
            player = playerController;
            body = renderer;
            front = frontSprite;
            back = backSprite;
        }

        private void LateUpdate()
        {
            Vector2Int f = player.Facing;
            bool up = f.x + f.y > 0;
            int sx = f.x - f.y;
            if (sx != 0) flip = sx < 0;

            body.sprite = (up && back != null) ? back : front;
            body.flipX = flip;

            bool moving = (transform.position - lastPos).sqrMagnitude > 1e-8f;
            lastPos = transform.position;
            if (moving)
            {
                walkTimer += Time.deltaTime;
                if (Mathf.FloorToInt(walkTimer / stepSeconds) % 2 == 1)
                {
                    transform.position += new Vector3(0f, 1f / PixelConstants.PixelsPerUnit, 0f);
                }
            }
            else
            {
                walkTimer = 0f;
            }
        }
    }
}
