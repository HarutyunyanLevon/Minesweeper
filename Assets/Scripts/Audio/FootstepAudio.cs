using UnityEngine;
using MinefieldClerk.Player;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// Footsteps of the deminer: one random step sound the moment walking starts, then one every few steps' time while moving.
    /// An injured deminer walks slower, so the steps slow down with him.
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        private PlayerController player;
        private float timer;
        private bool wasMoving;

        private void Update()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            AudioLibrary library = SoundDirector.CurrentLibrary;
            if (library == null) return;

            if (!player.IsMoving)
            {
                wasMoving = false;
                return;
            }

            if (!wasMoving)
            {
                wasMoving = true;
                timer = 0f;
                Sfx.Play(SfxEvent.StepDirt);
                return;
            }

            timer += Time.deltaTime * player.SpeedMultiplier;
            if (timer < library.footstepSeconds) return;
            timer = 0f;
            Sfx.Play(SfxEvent.StepDirt);
        }
    }
}
