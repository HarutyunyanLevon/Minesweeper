using UnityEngine;
using MinefieldClerk.Day;

namespace MinefieldClerk.Sound
{
    /// <summary>How a sound director picks what to play: follow the day, play one forced item, or stay silent (the cheat windows use the last two).</summary>
    public enum PlaybackMode { Auto, Forced, Silent }

    /// <summary>
    /// Chooses the music from the state of the day and crossfades between tracks:
    /// briefing paper -> briefing, shift -> field, state report -> report, family evening -> expenses
    /// (falls back to the report track while no expenses music exists). Track volumes and the fade length live in the AudioLibrary asset.
    /// The cheat windows can force a track, silence the music, pause it or restart it.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Instance { get; private set; }

        private CrossfadePlayer player;
        private string forcedTrack = "";

        public PlaybackMode Mode { get; set; } = PlaybackMode.Auto;
        public bool Paused { get; set; }
        /// <summary>The track the day would play right now (empty when none).</summary>
        public string AutoTrack { get; private set; } = "";
        /// <summary>The track that is current, or null.</summary>
        public string PlayingTrack => player != null ? player.ActiveKey : null;
        public AudioSource Source => player != null ? player.ActiveSource : null;

        /// <summary>Plays this track on a loop until the mode goes back to Auto.</summary>
        public void Force(string track)
        {
            forcedTrack = track;
            Mode = PlaybackMode.Forced;
            Paused = false;
        }

        public void Restart()
        {
            if (player != null) player.Restart();
        }

        private void Awake()
        {
            Instance = this;
            player = new CrossfadePlayer(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            AudioLibrary library = SoundDirector.CurrentLibrary;
            if (library == null) return;

            string auto = Wanted(library);
            AutoTrack = auto ?? "";

            player.SetPaused(Paused);
            if (Paused) return;

            string wanted = Mode == PlaybackMode.Silent ? null : (Mode == PlaybackMode.Forced ? forcedTrack : auto);
            MusicTrack track = null;
            if (wanted != null) track = library.FindMusic(wanted);

            if (track != null && track.clip != null) player.Play(track.name, track.clip, track.volume);
            else player.Stop();

            player.Tick(Time.unscaledDeltaTime, library.musicFadeSeconds, library.musicMaster * SoundDirector.Instance.Duck);
        }

        private static string Wanted(AudioLibrary library)
        {
            DayManager day = DayManager.Instance;
            if (day == null) return null;

            switch (day.Phase)
            {
                case DayPhase.Briefing:
                    return MusicName.Briefing;
                case DayPhase.Playing:
                    return MusicName.Field;
                default:
                    return day.ExpensesOpen && library.HasMusic(MusicName.Expenses) ? MusicName.Expenses : MusicName.Report;
            }
        }
    }
}
