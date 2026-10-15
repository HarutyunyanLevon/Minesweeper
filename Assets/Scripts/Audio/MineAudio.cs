using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Sound cues of the mine fuse: a click when a mine is found, beeps that speed up while it burns,
    /// then a boom (or a sad fizzle for a dud). Creates itself in any scene that has a MineField.
    /// </summary>
    public class MineAudio : MonoBehaviour
    {
        private MineField mines;
        private AudioSource sfx;
        private AudioSource beepSource;
        private AudioClip beep, clack, boom, dud;
        private readonly Dictionary<Vector3Int, Coroutine> beeps = new Dictionary<Vector3Int, Coroutine>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindAnyObjectByType<MineField>() == null || FindAnyObjectByType<MineAudio>() != null) return;
            new GameObject("MineAudio").AddComponent<MineAudio>();
        }

        private void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            beepSource = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0f;
            beepSource.spatialBlend = 0f;
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
            sfx.PlayOneShot(clack, 0.9f);
            beeps[cell] = StartCoroutine(Beeps(fuse));
        }

        private void OnTriggered(Vector3Int cell)
        {
            StopBeeps(cell);
            sfx.PlayOneShot(boom, 1f);
        }

        private void OnDud(Vector3Int cell)
        {
            StopBeeps(cell);
            sfx.PlayOneShot(dud, 0.8f);
        }

        private IEnumerator Beeps(float fuse)
        {
            float t = 0f;
            while (true)
            {
                float progress = Mathf.Clamp01(t / fuse);
                beepSource.pitch = Mathf.Lerp(1f, 1.6f, progress);
                beepSource.PlayOneShot(beep, 0.7f);
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
        }
    }
}
