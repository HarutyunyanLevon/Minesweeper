using UnityEngine;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// First mine hit: injured for the rest of the day (slower, tinted). Second hit: dead.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private float injuredSpeed = 0.6f;
        [Tooltip("Body tint while dead")]
        [SerializeField] private Color deadTint = new Color(0.45f, 0.4f, 0.4f);
        [Tooltip("Degrees the root rotates when dead (lying down)")]
        [SerializeField] private float deadRotation = 90f;

        private const int InjuredHits = 1;
        private const int DeadHits = 2;

        private int hits;

        public bool Injured => hits == InjuredHits;
        public bool Dead => hits >= DeadHits;

        public void Hit()
        {
            hits++;
            Apply();
        }

        public void ResetDay()
        {
            hits = 0;
            Apply();
        }

        private void Apply()
        {
            player.SpeedMultiplier = Injured ? injuredSpeed : 1f;
            player.Frozen = Dead;

            // The injured look (bandages, blood) is in the injured sprite clips, so only the dead state is tinted.
            body.color = Dead ? deadTint : Color.white;
            // A dead deminer lies down; the root rotates around the feet.
            player.transform.rotation = Dead ? Quaternion.Euler(0f, 0f, deadRotation) : Quaternion.identity;
        }
    }
}
