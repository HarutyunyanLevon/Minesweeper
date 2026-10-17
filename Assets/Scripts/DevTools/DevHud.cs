using UnityEngine;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

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

        private static readonly Color TextColor = new Color(1f, 0.94f, 0.78f);
        private static readonly Rect ButtonRect = new Rect(Margin, ButtonTop, ButtonWidth, ButtonHeight);
        private static readonly Rect DifficultyBar = new Rect(Margin, DifficultyTop, DifficultyWidth, DifficultyHeight);
        private static readonly Rect HudArea = new Rect(Margin, ButtonTop, DifficultyWidth, HudAreaHeight);

        [SerializeField] private PlayerController player;
        [SerializeField] private MineField mines;
        [SerializeField] private PlayerHealth health;

        private GUIStyle style;
        private MineFieldView view;
        private FieldSession session;

        /// <summary>True when a screen position (origin bottom-left) is over the HUD buttons.</summary>
        public static bool IsOverUi(Vector2 screen)
        {
            float y = Screen.height - screen.y;
            return HudArea.Contains(new Vector2(screen.x, y));
        }

        private void Update()
        {
            if (view == null) view = FindAnyObjectByType<MineFieldView>();
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (view != null && kb != null && kb.nKey.wasPressedThisFrame)
            {
                view.ToggleAllNumbers();
            }
            if (view != null && kb != null && kb.gKey.wasPressedThisFrame)
            {
                view.ToggleGrid();
            }
        }

        private void OnGUI()
        {
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
                "WASD/LMB move   Space/RMB probe   F/MMB flag   hold E defuse/collect   B rulebook   R new field   F1 show mines   N show all numbers   G grid";

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
