using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Sound
{
    /// <summary>One sound event (a folder under Assets/Audio/Sfx or Assets/Audio/Amb): every call plays one random variant.</summary>
    [Serializable]
    public class SoundEvent
    {
        public string name;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = AudioLibrary.DefaultVolume;
        [Tooltip("Random pitch variation around 1, so repeated sounds do not feel copied")]
        [Range(0f, AudioLibrary.MaxPitchJitter)] public float pitchJitter = AudioLibrary.DefaultPitchJitter;
    }

    /// <summary>One music file from Assets/Audio/Music, named after the file.</summary>
    [Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = AudioLibrary.DefaultMusicVolume;
    }

    /// <summary>
    /// Every sound of the game in one asset (Assets/Resources/Audio/AudioLibrary.asset), plus the audio settings.
    /// The audio folders are the source of truth: Minefield > Audio > Rebuild Library (also automatic when files change)
    /// refreshes the clip lists and keeps the volumes you set here.
    /// </summary>
    public class AudioLibrary : ScriptableObject
    {
        public const string ResourcePath = "Audio/AudioLibrary";
        public const string AssetFolder = "Assets/Resources/Audio";
        public const string AssetPath = AssetFolder + "/AudioLibrary.asset";

        public const float DefaultVolume = 1f;
        public const float DefaultPitchJitter = 0.04f;
        public const float MaxPitchJitter = 0.5f;
        public const float DefaultMusicVolume = 0.6f;
        public const float DefaultFadeSeconds = 2.5f;
        public const float DefaultStepSeconds = 0.42f;
        public const float DefaultLeavesMinSeconds = 14f;
        public const float DefaultLeavesMaxSeconds = 32f;
        public const float DefaultWindupPitch = 0.85f;
        public const float DefaultFuseDuck = 0.3f;
        public const float DefaultDuckSeconds = 0.25f;
        public const float MinWindupPitch = 0.25f;
        public const float MaxWindupPitch = 2f;

        [Header("Master levels")]
        [Range(0f, 1f)] public float sfxMaster = 1f;
        [Range(0f, 1f)] public float musicMaster = 1f;

        [Header("Timing")]
        [Tooltip("Seconds a music or ambience change takes to fade")]
        [Min(0f)] public float musicFadeSeconds = DefaultFadeSeconds;
        [Tooltip("Seconds between footsteps at normal speed")]
        [Min(0f)] public float footstepSeconds = DefaultStepSeconds;
        [Tooltip("Random gap (min, max seconds) between leaf rustles in the field")]
        public Vector2 leavesGapSeconds = new Vector2(DefaultLeavesMinSeconds, DefaultLeavesMaxSeconds);

        [Header("Mine fuse")]
        [Tooltip("Playback speed of the clockwork wind-up: below 1 is slower and deeper, 1 is as recorded")]
        [Range(MinWindupPitch, MaxWindupPitch)] public float windupPitch = DefaultWindupPitch;
        [Tooltip("Music and wind level while a fuse burns, so the wind-up stands out (1 = no change)")]
        [Range(0f, 1f)] public float fuseDuckLevel = DefaultFuseDuck;
        [Tooltip("Seconds the music takes to drop to that level and come back")]
        [Min(0f)] public float duckFadeSeconds = DefaultDuckSeconds;

        [Header("Content (rebuilt from the audio folders)")]
        public List<SoundEvent> events = new List<SoundEvent>();
        public List<MusicTrack> music = new List<MusicTrack>();

        [NonSerialized] private Dictionary<string, SoundEvent> eventLookup;
        [NonSerialized] private Dictionary<string, MusicTrack> musicLookup;

        private void OnEnable() { ClearLookups(); }
        private void OnValidate() { ClearLookups(); }

        private void ClearLookups()
        {
            eventLookup = null;
            musicLookup = null;
        }

        public SoundEvent Find(string eventName)
        {
            if (eventLookup == null)
            {
                eventLookup = new Dictionary<string, SoundEvent>();
                foreach (SoundEvent e in events) eventLookup[e.name] = e;
            }
            eventLookup.TryGetValue(eventName, out SoundEvent found);
            return found;
        }

        public MusicTrack FindMusic(string trackName)
        {
            if (musicLookup == null)
            {
                musicLookup = new Dictionary<string, MusicTrack>();
                foreach (MusicTrack t in music) musicLookup[t.name] = t;
            }
            musicLookup.TryGetValue(trackName, out MusicTrack found);
            return found;
        }

        public bool HasMusic(string trackName)
        {
            MusicTrack t = FindMusic(trackName);
            return t != null && t.clip != null;
        }
    }
}
