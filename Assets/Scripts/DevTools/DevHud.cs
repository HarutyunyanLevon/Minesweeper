using UnityEngine;
using MinefieldClerk.Field;
using MinefieldClerk.Player;
using MinefieldClerk.DevTools.Cheats;

namespace MinefieldClerk.DevTools
{
    /// <summary>Dev-only IMGUI readout. Replaced by the real UI later. Remove before release.</summary>
    public class DevHud : MonoBehaviour
    {
        private const int FontSize = 22;
        private const string UnknownField = "?";

        // Layout in IMGUI pixels.
        private const float Margin = 14f;
        private const float TextTop = 10f, TextWidth = 1400f, TextHeight = 140f;
        private const float ButtonTop = 150f, ButtonWidth = 260f, ButtonHeight = 44f;
        private const float DifficultyTop = 198f, DifficultyWidth = 400f, DifficultyHeight = 44f;
        private const float DifficultyGap = 8f;
        private const float HudAreaHeight = 92f;
        private const float SwitchWidth = 110f, SwitchHeight = 34f, SwitchMargin = 10f;
        private const string SwitchOn = "DEV: ON (F10)", SwitchOff = "DEV (F10)";

        private static readonly Color TextColor = new Color(1f, 0.94f, 0.78f);
        private static readonly Rect ButtonRect = new Rect(Margin, ButtonTop, ButtonWidth, ButtonHeight);
        private static readonly Rect DifficultyBar = new Rect(Margin, DifficultyTop, DifficultyWidth, DifficultyHeight);
        private static readonly Rect HudArea = new Rect(Margin, ButtonTop, DifficultyWidth, HudAreaHeight);

        [SerializeField] private PlayerController player;
        [SerializeField] private MineField mines;
        [SerializeField] private PlayerHealth health;

        private GUIStyle style;
        private MineFieldView view;
        private Day.DayManager day;
        private FieldSession session;

        /// <summary>True when a screen position (origin bottom-left) is over the HUD buttons.</summary>
        public static bool IsOverUi(Vector2 screen)
        {
            if (CheatSystem.IsOverUi(screen)) return true;
            float y = Screen.height - screen.y;
            var point = new Vector2(screen.x, y);
            if (SwitchRect().Contains(point)) return true;
            return DevMode.Enabled && HudArea.Contains(point);
        }

        private static Rect SwitchRect()
        {
            return new Rect(SwitchMargin, Screen.height - SwitchHeight - SwitchMargin, SwitchWidth, SwitchHeight);
        }

        private void Update()
        {
            DevMode.PollKey();
            if (!DevMode.Enabled) return;
            if (view == null) view = FindAnyObjectByType<MineFieldView>();
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (view != null && kb != null && !CheatSystem.Typing && kb.nKey.wasPressedThisFrame)
            {
                view.ToggleAllNumbers();
            }
            if (view != null && kb != null && !CheatSystem.Typing && kb.gKey.wasPressedThisFrame)
            {
                view.ToggleGrid();
            }
        }

        private void OnGUI()
        {
            if (day == null) day = FindAnyObjectByType<Day.DayManager>();
            if (day != null && day.BookOpen) return;   // the Book covers the readout
            if (GUI.Button(SwitchRect(), DevMode.Enabled ? SwitchOn : SwitchOff)) DevMode.Toggle();
            if (!DevMode.Enabled) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = FontSize, fontStyle = FontStyle.Bold };
                style.normal.textColor = TextColor;
            }

            string state = health.Dead ? "DECEASED (press R)" : (health.Injured ? "INJURED (slow)" : "OK");
            Vector3Int t = player.TargetCell;
            string text =
                "State: " + state + "\n" +
                "Probed " + mines.ProbedCount + "   Flags " + mines.FlagCount + "   Burning " + mines.ArmedCount + "   Exploded " + mines.ExplodedCount + "   Duds " + mines.DudCount + "   Collected " + mines.CollectedCount + "   (mines in field: " + mines.MineCount + ")\n" +
                "Target cell " + t.x + ", " + t.y + "   Field " + (session != null ? session.Settings.name : UnknownField) + "\n" +
                "WASD/LMB move   Space/RMB probe   F/MMB flag   hold E defuse/collect   B rulebook   R new field   F1 show mines   N show all numbers   G grid   F2-F7 cheat windows   F10 dev on/off";

            GUI.Label(new Rect(Margin, TextTop, TextWidth, TextHeight), text, style);

            if (session == null) session = FindAnyObjectByType<FieldSession>();
            if (session != null)
            {
                int count = System.Enum.GetValues(typeof(Difficulty)).Length;
                float w = (DifficultyBar.width - DifficultyGap * (count - 1)) / count;
                for (int i = 0; i < count; i++)
                {
                    var d = (Difficulty)i;
                    string name = DifficultyTable.Get(d).name;
                    string label = session.Difficulty == d ? "[ " + name + " ]" : name;
                    var rect = new Rect(DifficultyBar.x + i * (w + DifficultyGap), DifficultyBar.y, w, DifficultyBar.height);
                    if (GUI.Button(rect, label)) session.SetDifficulty(d);
                }
            }

            if (view != null)
            {
                string label = view.ShowingAllNumbers ? "Hide all numbers (N)" : "Show all numbers (N)";
                if (GUI.Button(ButtonRect, label))
                {
                    view.ToggleAllNumbers();
                }
            }
        }
    }
}
