using UnityEngine;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

namespace MinefieldClerk.DevTools
{
    /// <summary>Dev-only IMGUI readout. Replaced by the real UI later. Remove before release.</summary>
    public class DevHud : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private MineField mines;
        [SerializeField] private PlayerHealth health;

        private GUIStyle style;
        private MineFieldView view;

        private static readonly Rect ButtonRect = new Rect(14, 150, 260, 44);

        /// <summary>True when a screen position (origin bottom-left) is over the HUD button.</summary>
        public static bool IsOverUi(Vector2 screen)
        {
            float y = Screen.height - screen.y;
            return ButtonRect.Contains(new Vector2(screen.x, y));
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
                style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                style.normal.textColor = new Color(1f, 0.94f, 0.78f);
            }

            string state = health.Dead ? "DECEASED (press R)" : (health.Injured ? "INJURED (slow)" : "OK");
            Vector3Int t = player.TargetCell;
            string text =
                "State: " + state + "\n" +
                "Probed " + mines.ProbedCount + "   Flags " + mines.FlagCount + "   Burning " + mines.ArmedCount + "   Exploded " + mines.ExplodedCount + "   Duds " + mines.DudCount + "   (mines in field: " + mines.MineCount + ")\n" +
                "Target cell " + t.x + ", " + t.y + "\n" +
                "WASD/LMB move   Space/RMB probe   F/MMB flag   R new field   F1 show mines   N show all numbers   G grid";

            GUI.Label(new Rect(14, 10, 1400, 140), text, style);

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
