using UnityEngine;
using MinefieldClerk.Day;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// Background sound beds: wind over the field during the shift, the hum of the ministry office during the briefing
    /// and the state report, silence during the family evening. Leaves rustle now and then in the field.
    /// A random variant of the bed is picked each time it starts.
    /// The cheat windows can force a bed, silence it, pause it, restart it, take another variant or switch the leaf rustles off.
    /// </summary>
    public class AmbienceDirector : MonoBehaviour
    {
        public static AmbienceDirector Instance { get; private set; }

        private CrossfadePlayer bed;
        private string currentBed;
        private string forcedBed = "";
        private bool retake;
        private float leavesTimer;

        public PlaybackMode Mode { get; set; } = PlaybackMode.Auto;
        public bool Paused { get; set; }
        public bool LeavesEnabled { get; set; } = true;
        /// <summary>The bed the day would play right now (empty when none).</summary>
        public string AutoBed { get; private set; } = "";
        /// <summary>The bed that is current, or null.</summary>
        public string PlayingBed => bed != null ? bed.ActiveKey : null;
        public AudioSource Source => bed != null ? bed.ActiveSource : null;

        /// <summary>Plays this bed on a loop until the mode goes back to Auto.</summary>
        public void Force(string bedName)
        {
            forcedBed = bedName;
            Mode = PlaybackMode.Forced;
            Paused = false;
        }

        public void Restart()
        {
            if (bed != null) bed.Restart();
        }

        /// <summary>Starts the current bed again with a freshly picked variant.</summary>
        public void NewVariant()
        {
            retake = true;
        }

        private void Awake()
        {
            Instance = this;
            bed = new CrossfadePlayer(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            AudioLibrary library = SoundDirector.CurrentLibrary;
            if (library == null) return;

            string auto = AutoBedFor();
            AutoBed = auto ?? "";

            bed.SetPaused(Paused);
            if (Paused) return;

            string wanted = Mode == PlaybackMode.Silent ? null : (Mode == PlaybackMode.Forced ? forcedBed : auto);
            if (wanted != currentBed || retake)
            {
                if (retake) bed.Stop();   // the same bed name would otherwise keep its current take
                retake = false;
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

        private static string AutoBedFor()
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
            if (currentBed != SfxEvent.AmbWindField || !LeavesEnabled) return;
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
