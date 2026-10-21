using UnityEngine;
using MinefieldClerk.Day;

namespace MinefieldClerk.Sound
{
    /// <summary>
    /// Chooses the music from the state of the day and crossfades between tracks:
    /// briefing paper -> briefing, shift -> field, state report -> report, family evening -> expenses
    /// (falls back to the report track while no expenses music exists). Track volumes and the fade length live in the AudioLibrary asset.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        private CrossfadePlayer player;

        private void Awake()
        {
            player = new CrossfadePlayer(gameObject);
        }

        private void Update()
        {
            AudioLibrary library = SoundDirector.CurrentLibrary;
            if (library == null) return;

            MusicTrack track = null;
            string wanted = Wanted(library);
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
