using UnityEngine;
using MinefieldClerk.Day;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// Background sound beds: wind over the field during the shift, the hum of the ministry office during the briefing
    /// and the state report, silence during the family evening. Leaves rustle now and then in the field.
    /// A random variant of the bed is picked each time it starts.
    /// </summary>
    public class AmbienceDirector : MonoBehaviour
    {
        private CrossfadePlayer bed;
        private string currentBed;
        private float leavesTimer;

        private void Awake()
        {
            bed = new CrossfadePlayer(gameObject);
        }

        private void Update()
        {
            AudioLibrary library = SoundDirector.CurrentLibrary;
            if (library == null) return;

            string wanted = WantedBed();
            if (wanted != currentBed)
            {
                currentBed = wanted;
                StartBed(wanted);
                ResetLeaves(library);
            }

            UpdateLeaves(library);
            bed.Tick(Time.unscaledDeltaTime, library.musicFadeSeconds, SoundDirector.Instance.Duck);
        }

        private void StartBed(string bedName)
        {
            if (bedName != null && Sfx.TryPick(bedName, out AudioClip clip, out float volume)) bed.Play(bedName, clip, volume);
            else bed.Stop();
        }

        private static string WantedBed()
        {
            DayManager day = DayManager.Instance;
            if (day == null) return null;

            switch (day.Phase)
            {
                case DayPhase.Playing:
                    return SfxEvent.AmbWindField;
                case DayPhase.Briefing:
                    return SfxEvent.AmbOfficeHum;
                default:
                    return day.ExpensesOpen ? null : SfxEvent.AmbOfficeHum;
            }
        }

        private void UpdateLeaves(AudioLibrary library)
        {
            if (currentBed != SfxEvent.AmbWindField) return;
            leavesTimer -= Time.deltaTime;
            if (leavesTimer > 0f) return;
            Sfx.Play(SfxEvent.AmbLeaves);
            ResetLeaves(library);
        }

        private void ResetLeaves(AudioLibrary library)
        {
            leavesTimer = Random.Range(library.leavesGapSeconds.x, library.leavesGapSeconds.y);
        }
    }
}
