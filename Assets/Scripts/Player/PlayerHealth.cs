using System;
using UnityEngine;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// First mine hit: injured for the rest of the day (slower, tinted). Second hit: dead.
    /// Medicine, replacement by a relative and the family economy arrive in M3 and M6.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private float injuredSpeed = 0.6f;

        private int hits;

        public event Action Changed;

        public int Hits => hits;
        public bool Injured => hits == 1;
        public bool Dead => hits >= 2;

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

            body.color = Dead ? new Color(0.45f, 0.4f, 0.4f) : (Injured ? new Color(1f, 0.72f, 0.68f) : Color.white);
            // A dead deminer lies down; the root rotates around the feet.
            player.transform.rotation = Dead ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;

            Changed?.Invoke();
        }
    }
}
