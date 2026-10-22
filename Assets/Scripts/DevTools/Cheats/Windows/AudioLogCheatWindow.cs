using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Sound;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// Shows every sound effect the game played and which recording it picked (mine sequences, footsteps, flags, clicks...).
    /// Music and ambience beds are not listed: they play continuously and have their own controls in the Audio window.
    /// </summary>
    public sealed class AudioLogCheatWindow : CheatWindow
    {
        private const int MinLines = 5, DefaultLines = 40, LineStep = 5;
        private const string FilterControl = "audio-log-filter";
        private const string FootstepPrefix = "step_";
        private const string TimeFormat = "0.00", LevelFormat = "0.00", LengthFormat = "0.0";
        private const string NoClip = "(no recording)";
        private const string EmptyText = "Nothing played yet. Play a mine sequence or a walk in the Audio window, or just play the game.";
        private const string Hint = "Each line: time, sound event, the recording that was picked, its level, pitch and length. A line with no recording is an event that has none in the audio library.";

        private bool newestFirst = true;
        private bool hideSteps;
        private int shown = DefaultLines;
        private string filter = "";

        private readonly List<string> lines = new List<string>();
        private int builtSerial = -1, builtShown, builtMatches, builtKept;
        private bool builtNewestFirst, builtHideSteps;
        private string builtFilter;

        public override string Id => "audiolog";
        public override string Title => "Audio Log";
        public override Key DefaultToggleKey => Key.F9;

        protected override void Build(CheatList list)
        {
            list.Section("Log");
            list.Button("Clear log", AudioLog.Clear);
            list.Button("Copy log to clipboard", CopyAll, "Every kept line, oldest first");
            list.Toggle("Newest first", () => newestFirst, v => newestFirst = v, null, true).Saved();
            list.Toggle("Hide footsteps", () => hideSteps, v => hideSteps = v, "Footsteps would fill the log while walking").Saved();
            list.Int("Lines shown", () => shown, v => shown = v, MinLines, AudioLog.Capacity, LineStep, DefaultLines).Saved();
        }

        /// <summary>Rebuilds the text lines only when the log or a view option changed, so drawing never allocates.</summary>
        public override void OnUpdate()
        {
            if (!IsOpen) return;
            if (builtSerial == AudioLog.Serial && builtShown == shown && builtNewestFirst == newestFirst &&
                builtHideSteps == hideSteps && builtFilter == filter) return;

            builtSerial = AudioLog.Serial;
            builtShown = shown;
            builtNewestFirst = newestFirst;
            builtHideSteps = hideSteps;
            builtFilter = filter;

            lines.Clear();
            builtKept = AudioLog.Count;
            for (int age = 0; age < AudioLog.Count && lines.Count < shown; age++)
            {
                AudioLogEntry entry = AudioLog.Get(age);
                if (!Visible(entry)) continue;
                lines.Add(Format(entry));
            }
            builtMatches = lines.Count;
            if (!newestFirst) lines.Reverse();
        }

        protected override void DrawBody()
        {
            GUILayout.Label(Hint, CheatGui.Subtle);
            DrawCheatList();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter", GUILayout.Width(CheatLayout.FieldWidth));
            GUI.SetNextControlName(FilterControl);
            filter = GUILayout.TextField(filter);
            GUILayout.EndHorizontal();

            CheatGui.Heading("Played (" + builtKept + " kept, " + builtMatches + " shown)");
            if (lines.Count == 0)
            {
                GUILayout.Label(EmptyText, CheatGui.Subtle);
                return;
            }
            foreach (string line in lines) GUILayout.Label(line, CheatGui.Wrap);
        }

        private bool Visible(AudioLogEntry entry)
        {
            if (hideSteps && !entry.Marker && entry.Name != null && entry.Name.StartsWith(FootstepPrefix, System.StringComparison.Ordinal)) return false;
            if (string.IsNullOrEmpty(filter)) return true;
            if (entry.Name != null && entry.Name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return entry.Clip != null && entry.Clip.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Format(AudioLogEntry entry)
        {
            string time = entry.Time.ToString(TimeFormat);
            if (entry.Marker) return time + "   ---- " + entry.Name + " ----";

            string clip = entry.Clip != null ? entry.Clip.name + " (" + entry.Clip.length.ToString(LengthFormat) + " s)" : NoClip;
            string text = time + "   " + entry.Name + "   " + clip + "   level " + entry.Volume.ToString(LevelFormat) + "   pitch " + entry.Pitch.ToString(LevelFormat);
            return string.IsNullOrEmpty(entry.Note) ? text : text + "   " + entry.Note;
        }

        private static void CopyAll()
        {
            var text = new StringBuilder();
            for (int age = AudioLog.Count - 1; age >= 0; age--) text.AppendLine(Format(AudioLog.Get(age)));
            GUIUtility.systemCopyBuffer = text.ToString();
        }
    }
}
