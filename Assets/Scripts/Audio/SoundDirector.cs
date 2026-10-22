using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinefieldClerk.Field;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// Owns the audio library and a small pool of sound voices. Creates itself, the music, the ambience and the footsteps
    /// in any scene that has a FieldSession. Use <see cref="Sfx"/> to play sounds.
    /// </summary>
    public class SoundDirector : MonoBehaviour
    {
        private const string ObjectName = "Sound";
        private const int VoiceCount = 12;     // sounds that may overlap before the oldest is cut
        private const float Flat2D = 0f;       // AudioSource.spatialBlend: fully 2D
        private const string VariantNote = "picked by hand: variant ";

        public static SoundDirector Instance { get; private set; }

        public AudioLibrary Library { get; private set; }

        /// <summary>The audio library, or null while the sound system is not up (what every audio script asks first).</summary>
        public static AudioLibrary CurrentLibrary => Instance != null ? Instance.Library : null;

        /// <summary>Level multiplier for music and ambience: drops while a mine fuse burns, 1 otherwise.</summary>
        public float Duck { get; private set; } = 1f;

        private MineField mines;
        private AudioSource[] voices;
        private int nextVoice;
        private readonly Dictionary<string, int> lastPicked = new Dictionary<string, int>();
        private readonly HashSet<string> warned = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Instance != null || FindAnyObjectByType<FieldSession>() == null) return;
            var go = new GameObject(ObjectName);
            go.AddComponent<SoundDirector>();
            go.AddComponent<MusicDirector>();
            go.AddComponent<AmbienceDirector>();
            go.AddComponent<FootstepAudio>();
        }

        private void Awake()
        {
            Instance = this;
            Library = Resources.Load<AudioLibrary>(AudioLibrary.ResourcePath);
            if (Library == null)
            {
                Debug.LogWarning("[Sound] No audio library found. In Unity run Minefield > Audio > Rebuild Library.");
            }

            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = Flat2D;
                voices[i] = voice;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Library == null) return;
            if (mines == null) mines = FindAnyObjectByType<MineField>();

            float target = mines != null && mines.HasArmed ? Library.fuseDuckLevel : 1f;
            float step = Library.duckFadeSeconds > 0f ? Time.unscaledDeltaTime / Library.duckFadeSeconds : 1f;
            Duck = Mathf.MoveTowards(Duck, target, step);
        }

        public bool PlayEvent(string eventName, float volumeScale)
        {
            if (!TryPick(eventName, out AudioClip clip, out float volume, out float jitter)) return false;
            PlayOnVoice(eventName, clip, volume * volumeScale, jitter, null);
            return true;
        }

        /// <summary>Plays one chosen recording of an event (cheat tools). False when the event or that variant does not exist.</summary>
        public bool PlayVariant(string eventName, int index, float volumeScale)
        {
            SoundEvent sound = Library != null ? Library.Find(eventName) : null;
            if (sound == null || sound.clips == null || index < 0 || index >= sound.clips.Length || sound.clips[index] == null) return false;
            PlayOnVoice(eventName, sound.clips[index], sound.volume * Library.sfxMaster * volumeScale, sound.pitchJitter, VariantNote + (index + 1));
            return true;
        }

        /// <summary>Cuts every sound effect that is playing or waiting for its delay.</summary>
        public void StopAllVoices()
        {
            StopAllCoroutines();
            foreach (AudioSource voice in voices) voice.Stop();
        }

        /// <summary>How many effect voices are sounding right now.</summary>
        public int PlayingVoices
        {
            get
            {
                int count = 0;
                foreach (AudioSource voice in voices)
                {
                    if (voice.isPlaying) count++;
                }
                return count;
            }
        }

        private void PlayOnVoice(string eventName, AudioClip clip, float volume, float jitter, string note)
        {
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume);
            voice.pitch = 1f + Random.Range(-jitter, jitter);
            voice.Play();
            AudioLog.Add(eventName, clip, voice.volume, voice.pitch, note);
        }

        public bool HasEvent(string eventName)
        {
            SoundEvent sound = Library != null ? Library.Find(eventName) : null;
            return sound != null && sound.clips != null && sound.clips.Length > 0;
        }

        public void PlayEventDelayed(string eventName, float delaySeconds)
        {
            StartCoroutine(DelayedRoutine(eventName, delaySeconds));
        }

        private IEnumerator DelayedRoutine(string eventName, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            PlayEvent(eventName, 1f);
        }

        public bool TryPick(string eventName, out AudioClip clip, out float volume)
        {
            return TryPick(eventName, out clip, out volume, out _);
        }

        private bool TryPick(string eventName, out AudioClip clip, out float volume, out float jitter)
        {
            clip = null;
            volume = 0f;
            jitter = 0f;

            SoundEvent sound = Library != null ? Library.Find(eventName) : null;
            if (sound == null || sound.clips == null || sound.clips.Length == 0)
            {
                WarnOnce(eventName);
                return false;
            }

            int count = sound.clips.Length;
            int index;
            if (count > 1 && lastPicked.TryGetValue(eventName, out int last))
            {
                index = (last + 1 + Random.Range(0, count - 1)) % count;   // any variant except the one played last
            }
            else
            {
                index = Random.Range(0, count);
            }
            lastPicked[eventName] = index;

            clip = sound.clips[index];
            if (clip == null)
            {
                WarnOnce(eventName);
                return false;
            }
            volume = sound.volume * Library.sfxMaster;
            jitter = sound.pitchJitter;
            return true;
        }

        private void WarnOnce(string eventName)
        {
            if (!warned.Add(eventName)) return;
            Debug.LogWarning("[Sound] No clips for '" + eventName + "'. Check Assets/Audio and run Minefield > Audio > Rebuild Library.");
        }
    }
}
