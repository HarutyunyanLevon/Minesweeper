using UnityEngine;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// Plays one looping track at a time and crossfades when the track changes. Two voices: the new track fades in
    /// while the old one fades out. Call <see cref="Tick"/> every frame.
    /// </summary>
    public sealed class CrossfadePlayer
    {
        private const int VoiceCount = 2;
        private const float Flat2D = 0f;   // AudioSource.spatialBlend: fully 2D

        private sealed class Voice
        {
            public AudioSource Source;
            public float Level;   // fade position, 0 silent .. 1 full
            public float Gain;    // the track's own volume
            public string Key;
        }

        private readonly Voice[] voices = new Voice[VoiceCount];
        private Voice active;

        public CrossfadePlayer(GameObject host)
        {
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = Flat2D;
                voices[i] = new Voice { Source = source };
            }
        }

        /// <summary>Starts a track (fading in), or only updates the volume when it is already the current one.</summary>
        public void Play(string key, AudioClip clip, float gain)
        {
            if (active != null && active.Key == key)
            {
                active.Gain = gain;
                return;
            }
            if (clip == null)
            {
                Stop();
                return;
            }

            Voice next = voices[0] == active ? voices[1] : voices[0];
            next.Source.Stop();
            next.Source.clip = clip;
            next.Key = key;
            next.Gain = gain;
            next.Level = 0f;
            next.Source.Play();
            active = next;
        }

        /// <summary>Fades the current track out.</summary>
        public void Stop()
        {
            active = null;
        }

        /// <summary>Name the current track was started with, or null when nothing is current.</summary>
        public string ActiveKey => active != null ? active.Key : null;

        /// <summary>The audio source of the current track, or null (cheat tools read its position).</summary>
        public AudioSource ActiveSource => active != null ? active.Source : null;

        public bool IsPaused { get; private set; }

        /// <summary>Freezes or resumes every voice, fades included.</summary>
        public void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            IsPaused = paused;
            foreach (Voice voice in voices)
            {
                if (paused) voice.Source.Pause();
                else voice.Source.UnPause();
            }
        }

        /// <summary>Plays the current track again from its start.</summary>
        public void Restart()
        {
            if (active == null) return;
            active.Source.time = 0f;
            if (!active.Source.isPlaying && !IsPaused) active.Source.Play();
        }

        public void Tick(float deltaSeconds, float fadeSeconds, float master)
        {
            float step = fadeSeconds > 0f ? deltaSeconds / fadeSeconds : 1f;
            foreach (Voice voice in voices)
            {
                float target = voice == active ? 1f : 0f;
                voice.Level = Mathf.MoveTowards(voice.Level, target, step);
                voice.Source.volume = voice.Level * voice.Gain * master;
                if (voice != active && voice.Level <= 0f && voice.Source.isPlaying) voice.Source.Stop();
            }
        }
    }
}
