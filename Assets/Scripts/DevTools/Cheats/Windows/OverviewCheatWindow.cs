using UnityEngine;
using UnityEngine.InputSystem;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// The control room of the cheat system: open or close any window (with its hotkey), search every cheat of every window,
    /// and adjust the cheat UI itself (scale, opacity, layout, saved settings).
    /// </summary>
    public sealed class OverviewCheatWindow : CheatWindow
    {
        private const string SearchControl = "cheat-search";
        private const string SearchLabel = "Search all cheats";
        private const string HelpText = "Every cheat lives in a window. Drag a title bar to move a window, drag the :: corner to resize it, _ folds it up. " +
            "Buttons and switches with a [key] box can be bound to any key: click the box, then press the key (Esc clears it). F10 turns all cheats on or off.";

        private string filter = "";

        public override string Id => "overview";
        public override string Title => "Cheats";
        public override Key DefaultToggleKey => Key.F7;
        public override Vector2 DefaultSize => new Vector2(CheatLayout.DefaultWindowWidth, CheatLayout.DefaultWindowHeight);

        protected override void Build(CheatList list)
        {
            list.Section("Windows");
            CheatSystem system = CheatSystem.Instance;
            if (system == null) return;
            foreach (CheatWindow window in system.Windows)
            {
                CheatWindow captured = window;
                list.Toggle("Show " + captured.Title, () => captured.IsOpen, captured.SetOpen).Hot(captured.DefaultToggleKey);
            }
        }

        protected override void DrawBody()
        {
            CheatSystem system = CheatSystem.Instance;
            GUILayout.Label(HelpText, CheatGui.Subtle);

            CheatGui.Heading("Cheat interface");
            float scale = CheatGui.FloatRow("ui-scale", "UI scale", system.UiScale, CheatLayout.MinUiScale, CheatLayout.MaxUiScale, CheatLimits.UnitStep);
            if (!Mathf.Approximately(scale, system.UiScale)) system.UiScale = scale;
            float opacity = CheatGui.FloatRow("ui-opacity", "Window opacity", system.Opacity, CheatLayout.MinOpacity, 1f, CheatLimits.UnitStep);
            if (!Mathf.Approximately(opacity, system.Opacity)) system.Opacity = opacity;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset window layout")) system.ResetLayouts();
            if (GUILayout.Button("Forget saved cheat settings")) system.ForgetEverything();
            GUILayout.EndHorizontal();

            CheatGui.Heading("Windows and hotkeys");
            DrawCheatList();

            CheatGui.Heading(SearchLabel);
            GUI.SetNextControlName(SearchControl);
            filter = GUILayout.TextField(filter);
            if (string.IsNullOrEmpty(filter)) return;

            foreach (CheatWindow window in system.Windows)
            {
                if (window == this) continue;
                bool any = false;
                foreach (Cheat cheat in window.Cheats.Items)
                {
                    if (!cheat.Matches(filter)) continue;
                    if (!any)
                    {
                        CheatGui.Heading(window.Title);
                        any = true;
                    }
                    CheatGui.DrawCheat(cheat);
                }
            }
        }
    }
}
