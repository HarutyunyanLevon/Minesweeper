using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Day;
using MinefieldClerk.Field;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Days with the collect-mines rule: hold E (gamepad Y) facing a cell to work on it.
    /// A flagged cell is defused (one defuse kit per attempt); a defused mine, or a mine that has already been
    /// made harmless some other way, is collected. Each job takes the time the rule says. The deminer stands still while working, and letting go of the
    /// button cancels the job and loses its progress. A probed mine is armed and never offers a job.
    /// Added to the player by PlayerProbe, so the scene needs no wiring.
    /// </summary>
    public class MineWork : MonoBehaviour
    {
        // Progress bar (IMGUI placeholder), drawn above the deminer's head.
        private const float BarWidth = 140f, BarHeight = 14f, BarInset = 2f, LabelHeight = 24f, LabelGap = 4f;
        private const float BarLiftWorld = 1f;          // world units above the feet
        private const int LabelFont = 18;
        private const float ActionRepeatSeconds = 0.5f;  // the flag animation replays this often while working
        private const string NoKits = "No defuse kits left.";

        private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color BarFill = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color LabelColor = new Color(1f, 0.94f, 0.78f);

        private PlayerController player;
        private MineField mines;
        private PlayerHealth health;
        private DeminerSprites sprites;
        private GUIStyle label;

        private Vector3Int workCell;
        private CellWork kind;
        private float progress;
        private float duration;
        private float repeatTimer;

        /// <summary>True while a job is running (the deminer is rooted).</summary>
        public bool Busy { get; private set; }

        public void Setup(PlayerController playerController, MineField field, PlayerHealth playerHealth)
        {
            player = playerController;
            mines = field;
            health = playerHealth;
            sprites = player.GetComponent<DeminerSprites>();
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void Update()
        {
            if (player == null || mines == null || health == null) return;

            DayManager day = DayManager.Instance;
            RuleSet rules = day != null ? day.Rules : null;
            if (rules == null || !rules.CollectMines || !day.CanAct || health.Dead)
            {
                Cancel();
                return;
            }

            bool held = ReadButton(out bool pressed);
            if (!held)
            {
                Cancel();
                return;
            }

            Vector3Int cell = player.TargetCell.Flat();
            CellWork work = mines.WorkAt(cell);
            if (work != CellWork.Defuse && work != CellWork.Collect)
            {
                Cancel();
                if (pressed) day.Notify(Reason(work));
                return;
            }

            if (work == CellWork.Defuse && day.KitsLeft <= 0)
            {
                Cancel();
                if (pressed) day.Notify(NoKits);
                return;
            }

            if (!Busy || cell != workCell || work != kind) Begin(cell, work, rules);

            progress += Time.deltaTime;
            repeatTimer += Time.deltaTime;
            if (repeatTimer >= ActionRepeatSeconds)
            {
                repeatTimer = 0f;
                if (sprites != null) sprites.PlayAction("flag");
            }
            if (progress >= duration) Finish(day, cell, work);
        }

        private void Begin(Vector3Int cell, CellWork work, RuleSet rules)
        {
            workCell = cell;
            kind = work;
            progress = 0f;
            repeatTimer = 0f;
            duration = work == CellWork.Defuse ? rules.DefuseSeconds : rules.CollectSeconds;
            Busy = true;
            player.Busy = true;
            player.FaceCell(cell);
            if (sprites != null) sprites.PlayAction("flag");
        }

        private void Finish(DayManager day, Vector3Int cell, CellWork work)
        {
            if (work == CellWork.Defuse)
            {
                day.SpendKit();   // the attempt costs a kit whatever is under the flag
                DefuseOutcome result = mines.Defuse(cell);
                day.Notify(result == DefuseOutcome.Defused
                    ? "Mine defused. Kits left: " + day.KitsLeft + ". Hold E to collect it."
                    : "Nothing buried here. The kit is wasted and the flag is removed. Kits left: " + day.KitsLeft + ".");
            }
            else if (mines.Collect(cell))
            {
                day.Notify("Mine collected (" + mines.CollectedCount + ").");
            }
            // The next frame starts the follow-up job (defused -> collect) if the button is still held and one exists.
            Busy = false;
            player.Busy = false;
            progress = 0f;
        }

        private void Cancel()
        {
            progress = 0f;
            if (!Busy) return;
            Busy = false;
            if (player != null) player.Busy = false;
        }

        private static string Reason(CellWork work)
        {
            switch (work)
            {
                case CellWork.NeedsFlag: return "Flag it first.";
                case CellWork.Armed: return "It is armed. Too late to defuse.";
                default: return "Nothing to do here.";
            }
        }

        private static bool ReadButton(out bool pressedThisFrame)
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            pressedThisFrame = (kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            return (kb != null && kb.eKey.isPressed) || (pad != null && pad.buttonNorth.isPressed);
        }

        private void OnGUI()
        {
            if (!Busy) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = LabelFont, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label.normal.textColor = LabelColor;
            }

            Vector3 screen = cam.WorldToScreenPoint(player.transform.position + Vector3.up * BarLiftWorld);
            float x = screen.x - BarWidth * Num.Half;
            float y = Screen.height - screen.y;

            GUI.color = BarBack;
            GUI.DrawTexture(new Rect(x, y, BarWidth, BarHeight), Texture2D.whiteTexture);
            GUI.color = BarFill;
            GUI.DrawTexture(new Rect(x + BarInset, y + BarInset, (BarWidth - BarInset * 2f) * Mathf.Clamp01(progress / duration), BarHeight - BarInset * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y - LabelHeight - LabelGap, BarWidth, LabelHeight), kind == CellWork.Defuse ? "Defusing" : "Collecting", label);
        }
    }
}
