using UnityEngine;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// The one call every script uses to make a sound: Sfx.Play(SfxEvent.DigHole).
    /// Picks a random variant of the event (never the same one twice in a row) and does nothing when the sound system is not up.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Plays a random variant of the event. False when there is nothing to play (callers may fall back).</summary>
        public static bool Play(string eventName, float volumeScale = 1f)
        {
            SoundDirector director = SoundDirector.Instance;
            return director != null && director.PlayEvent(eventName, volumeScale);
        }

        /// <summary>True when the event has at least one recording (for optional extra layers).</summary>
        public static bool Has(string eventName)
        {
            SoundDirector director = SoundDirector.Instance;
            return director != null && director.HasEvent(eventName);
        }

        /// <summary>Plays the event after a delay.</summary>
        public static void PlayDelayed(string eventName, float delaySeconds)
        {
            SoundDirector director = SoundDirector.Instance;
            if (director != null) director.PlayEventDelayed(eventName, delaySeconds);
        }

        /// <summary>Picks a random variant and its volume without playing it, for scripts that run their own AudioSource.</summary>
        public static bool TryPick(string eventName, out AudioClip clip, out float volume)
        {
            SoundDirector director = SoundDirector.Instance;
            if (director == null)
            {
                clip = null;
                volume = 0f;
                return false;
            }
            return director.TryPick(eventName, out clip, out volume);
        }
    }
}
