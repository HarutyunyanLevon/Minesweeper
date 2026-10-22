using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinefieldClerk.Sound;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Sound cues of the mine fuse: the plate click when a mine is found, then a slow clockwork wind-up that lasts the whole
    /// fuse and ends in an explosion (or an anticlimactic fizzle for a dud). Creates itself in any scene that has a MineField.
    /// Every sound is a recording from the audio library (mine_plate, mine_windup, mine_explosion, dud_fizzle); an event
    /// without recordings is silent and warns once in the Console.
    /// </summary>
    public class MineAudio : MonoBehaviour
    {
        private const string ObjectName = "MineAudio";
        private const float Flat2D = 0f;   // AudioSource.spatialBlend: fully 2D

        private MineField mines;
        private readonly Dictionary<Vector3Int, AudioSource> windups = new Dictionary<Vector3Int, AudioSource>();
        private readonly Stack<AudioSource> idleWindups = new Stack<AudioSource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindAnyObjectByType<MineField>() == null || FindAnyObjectByType<MineAudio>() != null) return;
            new GameObject(ObjectName).AddComponent<MineAudio>();
        }

        private void Start()
        {
            mines = FindAnyObjectByType<MineField>();
            if (mines == null) return;
            mines.MineArmed += OnArmed;
            mines.MineTriggered += OnTriggered;
            mines.MineDud += OnDud;
            mines.Regenerated += StopAllFuses;
        }

        private void OnDestroy()
        {
            if (mines == null) return;
            mines.MineArmed -= OnArmed;
            mines.MineTriggered -= OnTriggered;
            mines.MineDud -= OnDud;
            mines.Regenerated -= StopAllFuses;
        }

        private void OnArmed(Vector3Int cell, float fuse)
        {
            Sfx.Play(SfxEvent.MinePlate);
            StartWindup(cell);
        }

        private void OnTriggered(Vector3Int cell)
        {
            StopWindup(cell);
            Sfx.Play(SfxEvent.MineExplosion);
        }

        private void OnDud(Vector3Int cell)
        {
            StopWindup(cell);
            Sfx.Play(SfxEvent.DudFizzle);
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
            AudioLog.Add(SfxEvent.MineWindup, clip, source.volume, source.pitch);
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

        // ------------------------------------------------------------------ cheat tools

        /// <summary>A pretend mine that is not on the field, so cheat sequences use exactly the sounds and code of a real one.</summary>
        private static readonly Vector3Int CheatCell = new Vector3Int(int.MinValue, int.MinValue, 0);
        private Coroutine cheatSequence;

        /// <summary>True while a cheat mine sequence is still running.</summary>
        public bool CheatSequenceRunning => cheatSequence != null;

        /// <summary>Plays a whole mine as the field does: the plate click, the wind-up for the fuse length, then the blast or the fizzle.</summary>
        public void CheatSequence(float fuseSeconds, bool dud)
        {
            CheatStop();
            AudioLog.Mark("mine sequence: fuse " + fuseSeconds.ToString("0.0") + " s, ends in " + (dud ? "a dud" : "an explosion"));
            cheatSequence = StartCoroutine(SequenceRoutine(fuseSeconds, dud));
        }

        /// <summary>Only the plate click.</summary>
        public void CheatTrigger() { Sfx.Play(SfxEvent.MinePlate); }

        /// <summary>Only the wind-up; it keeps going until the explosion, the dud or Stop.</summary>
        public void CheatWindup()
        {
            CheatStop();
            StartWindup(CheatCell);
        }

        /// <summary>The blast; it also ends a wind-up started by <see cref="CheatWindup"/>.</summary>
        public void CheatExplosion()
        {
            CheatStop();
            OnTriggered(CheatCell);
        }

        /// <summary>The anticlimactic fizzle; it also ends a wind-up started by <see cref="CheatWindup"/>.</summary>
        public void CheatDud()
        {
            CheatStop();
            OnDud(CheatCell);
        }

        /// <summary>Ends a running sequence and its wind-up without a blast or fizzle.</summary>
        public void CheatStop()
        {
            if (cheatSequence != null)
            {
                StopCoroutine(cheatSequence);
                cheatSequence = null;
            }
            StopWindup(CheatCell);
        }

        private IEnumerator SequenceRoutine(float fuseSeconds, bool dud)
        {
            OnArmed(CheatCell, fuseSeconds);
            yield return new WaitForSeconds(fuseSeconds);
            cheatSequence = null;
            if (dud) OnDud(CheatCell);
            else OnTriggered(CheatCell);
        }

        /// <summary>Cheat tools: silences every fuse sound at once.</summary>
        public void StopAllSounds()
        {
            StopAllFuses();
        }

        private void StopAllFuses()
        {
            StopAllCoroutines();
            cheatSequence = null;
            foreach (AudioSource source in windups.Values)
            {
                source.Stop();
                idleWindups.Push(source);
            }
            windups.Clear();
        }
    }
}
