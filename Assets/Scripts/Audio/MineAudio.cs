using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinefieldClerk.Sound;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Sound cues of the mine fuse: a click when a mine is found, then a slow clockwork wind-up that lasts the whole fuse
    /// and ends in a boom (or an anticlimactic fizzle for a dud). Creates itself in any scene that has a MineField.
    /// The wind-up, boom and fizzle use the recorded variants from the audio library. Without a wind-up recording the
    /// fuse falls back to rising beeps, and the synthesised sounds below are the last fallback.
    /// </summary>
    public class MineAudio : MonoBehaviour
    {
        private const string ObjectName = "MineAudio";

        [Header("Volumes")]
        [SerializeField] private float clackVolume = 0.9f;
        [SerializeField] private float boomVolume = 1f;
        [SerializeField] private float dudVolume = 0.8f;
        [SerializeField] private float beepVolume = 0.7f;
        [Header("Beep pitch rises from start to end as the fuse burns")]
        [SerializeField] private float beepPitchStart = 1f;
        [SerializeField] private float beepPitchEnd = 1.6f;

        private const float Flat2D = 0f;   // AudioSource.spatialBlend: fully 2D

        private MineField mines;
        private AudioSource sfx;
        private AudioSource beepSource;
        private AudioClip beep, clack, boom, dud;
        private readonly Dictionary<Vector3Int, Coroutine> beeps = new Dictionary<Vector3Int, Coroutine>();
        private readonly Dictionary<Vector3Int, AudioSource> windups = new Dictionary<Vector3Int, AudioSource>();
        private readonly Stack<AudioSource> idleWindups = new Stack<AudioSource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindAnyObjectByType<MineField>() == null || FindAnyObjectByType<MineAudio>() != null) return;
            new GameObject(ObjectName).AddComponent<MineAudio>();
        }

        private void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            beepSource = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = Flat2D;
            beepSource.spatialBlend = Flat2D;
            beep = ProceduralSfx.Beep();
            clack = ProceduralSfx.Clack();
            boom = ProceduralSfx.Boom();
            dud = ProceduralSfx.Dud();
        }

        private void Start()
        {
            mines = FindAnyObjectByType<MineField>();
            if (mines == null) return;
            mines.MineArmed += OnArmed;
            mines.MineTriggered += OnTriggered;
            mines.MineDud += OnDud;
            mines.Regenerated += StopAllBeeps;
        }

        private void OnDestroy()
        {
            if (mines == null) return;
            mines.MineArmed -= OnArmed;
            mines.MineTriggered -= OnTriggered;
            mines.MineDud -= OnDud;
            mines.Regenerated -= StopAllBeeps;
        }

        private void OnArmed(Vector3Int cell, float fuse)
        {
            sfx.PlayOneShot(clack, clackVolume);
            if (StartWindup(cell)) return;

            // No wind-up recording: one recorded beep variant per fuse, so a burning mine keeps a steady voice.
            AudioClip clip = beep;
            float volume = beepVolume;
            if (Sfx.TryPick(SfxEvent.MineArmBeep, out AudioClip picked, out float pickedVolume))
            {
                clip = picked;
                volume = pickedVolume * beepVolume;
            }
            beeps[cell] = StartCoroutine(Beeps(fuse, clip, volume));
        }

        private void OnTriggered(Vector3Int cell)
        {
            StopBeeps(cell);
            StopWindup(cell);
            if (!Sfx.Play(SfxEvent.MineExplosion)) sfx.PlayOneShot(boom, boomVolume);
        }

        private void OnDud(Vector3Int cell)
        {
            StopBeeps(cell);
            StopWindup(cell);
            if (!Sfx.Play(SfxEvent.DudFizzle)) sfx.PlayOneShot(dud, dudVolume);
        }

        /// <summary>Starts the clockwork wind-up at the library's playback speed. False when there is no recording.</summary>
        private bool StartWindup(Vector3Int cell)
        {
            if (!Sfx.TryPick(SfxEvent.MineWindup, out AudioClip clip, out float volume)) return false;

            StopWindup(cell);
            AudioSource source = idleWindups.Count > 0 ? idleWindups.Pop() : CreateWindupSource();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = SoundDirector.CurrentLibrary.windupPitch;   // never squeezed into the fuse: a short fuse just cuts it off at the boom
            source.Play();
            windups[cell] = source;
            return true;
        }

        private AudioSource CreateWindupSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = Flat2D;
            return source;
        }

        private void StopWindup(Vector3Int cell)
        {
            if (!windups.TryGetValue(cell, out AudioSource source)) return;
            source.Stop();
            windups.Remove(cell);
            idleWindups.Push(source);
        }

        private IEnumerator Beeps(float fuse, AudioClip clip, float volume)
        {
            float t = 0f;
            while (true)
            {
                float progress = Mathf.Clamp01(t / fuse);
                beepSource.pitch = Mathf.Lerp(beepPitchStart, beepPitchEnd, progress);
                beepSource.PlayOneShot(clip, volume);
                float interval = FuseTiming.BeepInterval(progress);
                yield return new WaitForSeconds(interval);
                t += interval;
            }
        }

        private void StopBeeps(Vector3Int cell)
        {
            if (beeps.TryGetValue(cell, out Coroutine c))
            {
                if (c != null) StopCoroutine(c);
                beeps.Remove(cell);
            }
        }

        private void StopAllBeeps()
        {
            StopAllCoroutines();
            beeps.Clear();
            foreach (AudioSource source in windups.Values)
            {
                source.Stop();
                idleWindups.Push(source);
            }
            windups.Clear();
        }
    }
}
