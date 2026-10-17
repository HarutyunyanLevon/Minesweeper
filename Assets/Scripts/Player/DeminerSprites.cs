using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Plays the deminer's sprite animation. Clips are named "[inj]front|back_walk|idle|probe|flag":
    /// front/back follows the facing (mirrored for left), "inj" is used after the first mine hit.
    /// Walk and idle loop by movement; probe and flag are one-shots triggered by PlayerProbe.
    /// Runs after PlayerController, so it also owns flipX. Falls back to the still sprites if clips are missing.
    /// </summary>
    public class DeminerSprites : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public string name;
            public Sprite[] frames;
            public float fps = 8f;
        }

        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PlayerHealth health;
        [SerializeField, HideInInspector] private Sprite front;
        [SerializeField, HideInInspector] private Sprite back;
        [SerializeField] private List<Clip> clips = new List<Clip>();

        private readonly Dictionary<string, Clip> byName = new Dictionary<string, Clip>();
        private bool flip;
        private string loopAct;
        private float loopTime;
        private string action;
        private float actionTime;

        public void Configure(PlayerController playerController, SpriteRenderer renderer, Sprite frontSprite, Sprite backSprite)
        {
            player = playerController;
            body = renderer;
            front = frontSprite;
            back = backSprite;
        }

        public void SetClips(List<Clip> newClips, PlayerHealth playerHealth)
        {
            clips = newClips;
            health = playerHealth;
        }

        /// <summary>Plays a one-shot ("probe" or "flag") on top of walking and idling.</summary>
        public void PlayAction(string name)
        {
            action = name;
            actionTime = 0f;
        }

        private void Awake()
        {
            RebuildLookup();
            if (health == null && player != null) health = player.GetComponent<PlayerHealth>();
        }

        private void RebuildLookup()
        {
            byName.Clear();
            foreach (Clip c in clips)
            {
                if (c != null && c.frames != null && c.frames.Length > 0 && !string.IsNullOrEmpty(c.name)) byName[c.name] = c;
            }
        }

        private void LateUpdate()
        {
            Vector2Int f = player.Facing;
            bool up = f.x + f.y > 0;
            int sx = f.x - f.y;
            if (sx != 0) flip = sx < 0;
            body.flipX = flip;

            bool dead = health != null && health.Dead;
            bool injured = health != null && health.Injured;
            bool moving = player.IsMoving;

            if (byName.Count == 0)
            {
                body.sprite = (up && back != null) ? back : front;
                return;
            }

            string prefix = ((dead || injured) ? "inj" : "") + (up ? "back_" : "front_");

            if (dead)
            {
                action = null;
                SetFrame(prefix + "idle", 0f, true);
                return;
            }

            if (action != null)
            {
                actionTime += Time.deltaTime;
                if (!byName.TryGetValue(prefix + action, out Clip c) || actionTime * c.fps >= c.frames.Length)
                {
                    action = null;
                }
                else
                {
                    SetFrame(prefix + action, actionTime, false);
                    return;
                }
            }

            string act = moving ? "walk" : "idle";
            if (act != loopAct)
            {
                loopAct = act;
                loopTime = 0f;
            }
            loopTime += Time.deltaTime;
            SetFrame(prefix + act, loopTime, true);
        }

        private void SetFrame(string key, float time, bool loop)
        {
            if (!byName.TryGetValue(key, out Clip c)) return;
            int n = c.frames.Length;
            int i = Mathf.FloorToInt(time * c.fps);
            i = loop ? i % n : Mathf.Min(i, n - 1);
            body.sprite = c.frames[i];
        }
    }
}
