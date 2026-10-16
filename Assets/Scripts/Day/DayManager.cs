using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

namespace MinefieldClerk.Day
{
    public enum DayPhase { Playing, Debrief }

    /// <summary>
    /// M3 day loop: detector battery, end of day, debrief with pay per correct flag, quota bonus, injury and medicine.
    /// Placeholder IMGUI screens until the real briefing/debrief art exists. Finds the scene objects by itself,
    /// so it needs no scene wiring (it spawns when a FieldSession exists).
    /// Battery drains with time (the "sun" dims) and every probe costs a little extra. At 0 the detector is dead:
    /// no more probing, but flagging from memory and ending the day still work.
    /// Pay: flags on real mines (and located duds) are paid, quota met gives a bonus, a first hit costs medicine.
    /// A second hit ends the day; money carries over and a relative takes over the next day (family economy: M6).
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Header("Battery")]
        [SerializeField] private float batterySeconds = 120f;
        [SerializeField] private float probeCostSeconds = 0f;
        [Header("Pay (placeholder numbers)")]
        [SerializeField] private int payPerFlag = 10;
        [SerializeField] private int wrongFlagPenalty = 10;
        [SerializeField] private int quotaBase = 10;
        [SerializeField] private int quotaPerDay = 1;
        [SerializeField] private int quotaBonus = 25;
        [SerializeField] private int medicineCost = 15;
        [SerializeField] private float deathDelay = 2.5f;

        private FieldSession session;
        private MineField mines;
        private PlayerHealth health;
        private PlayerController player;

        private float battery;
        private float deathTimer;
        private bool carryInjured;
        private readonly List<string> report = new List<string>();
        private string reportTitle = "";
        private string toast = "";
        private float toastUntil;
        private GUIStyle small, big, title, wrapped, toastStyle;

        public DayPhase Phase { get; private set; } = DayPhase.Playing;
        public int Day { get; private set; } = 1;
        public int Money { get; private set; }
        public bool Playing => Phase == DayPhase.Playing;
        public bool BatteryEmpty => battery <= 0f;
        public float Battery01 => Mathf.Clamp01(battery / batterySeconds);
        public int Quota => quotaBase + quotaPerDay * (Day - 1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<FieldSession>() != null && Object.FindAnyObjectByType<DayManager>() == null)
            {
                new GameObject("DayManager").AddComponent<DayManager>();
            }
        }

        /// <summary>Probing needs battery and an active day.</summary>
        public bool CanProbe => Playing && !BatteryEmpty;

        public void SpendProbe()
        {
            battery = Mathf.Max(0f, battery - probeCostSeconds);
        }

        public void Notify(string text)
        {
            toast = text;
            toastUntil = Time.time + 2.5f;
        }

        private void Awake()
        {
            Instance = this;
            session = FindAnyObjectByType<FieldSession>();
            mines = FindAnyObjectByType<MineField>();
            health = FindAnyObjectByType<PlayerHealth>();
            player = FindAnyObjectByType<PlayerController>();
        }

        private void OnEnable()
        {
            mines.Regenerated += OnRegenerated;
        }

        private void OnDisable()
        {
            if (mines != null) mines.Regenerated -= OnRegenerated;
            if (Instance == this) Instance = null;
        }

        private void OnRegenerated()
        {
            battery = batterySeconds;
            deathTimer = 0f;
            Phase = DayPhase.Playing;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            bool enter = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);

            if (Phase == DayPhase.Playing)
            {
                if (!BatteryEmpty) battery = Mathf.Max(0f, battery - Time.deltaTime);

                if (health.Dead)
                {
                    deathTimer += Time.deltaTime;
                    if (deathTimer >= deathDelay && !mines.HasArmed) EndDay();
                }
                else if (enter)
                {
                    if (mines.HasArmed) Notify("A fuse is still burning.");
                    else EndDay();
                }
            }
            else if (enter)
            {
                NextDay();
            }
        }

        public void EndDay()
        {
            mines.Tally(out int correct, out int wrong);
            int located = correct + mines.DudCount;
            int flagsPlaced = mines.FlagCount;
            bool quotaMet = located >= Quota;

            int pay = located * payPerFlag - wrong * wrongFlagPenalty;
            int bonus = quotaMet ? quotaBonus : 0;
            Money += pay + bonus;

            report.Clear();
            report.Add("Mines located (flagged or exposed duds): " + located + " x " + payPerFlag + " = " + (located * payPerFlag));
            if (wrongFlagPenalty > 0) report.Add("Wrong flags: " + wrong + " x -" + wrongFlagPenalty);
            else if (wrong > 0) report.Add("Flags on safe ground: " + wrong + " (unpaid)");
            report.Add("Quota " + Quota + ": " + (quotaMet ? "MET, bonus +" + quotaBonus : "NOT MET, no bonus"));
            report.Add("Mines exploded: " + mines.ExplodedCount + "    Tiles probed: " + mines.ProbedCount + "    Flags placed: " + flagsPlaced);

            carryInjured = false;
            if (health.Dead)
            {
                reportTitle = "DEMINER KILLED - DAY " + Day;
                report.Add("The deminer did not come back. A relative takes the job tomorrow.");
            }
            else
            {
                reportTitle = "DEBRIEF - DAY " + Day;
                if (health.Injured)
                {
                    if (Money >= medicineCost)
                    {
                        Money -= medicineCost;
                        report.Add("Medicine: -" + medicineCost + " (injury treated)");
                    }
                    else
                    {
                        carryInjured = true;
                        report.Add("Not enough money for medicine. Starting tomorrow injured.");
                    }
                }
            }
            report.Add("");
            report.Add("Money: " + Money);

            Phase = DayPhase.Debrief;
            player.Frozen = true;
        }

        public void NextDay()
        {
            Day++;
            session.NewDay();   // regenerates the field; OnRegenerated resets battery and phase
            if (carryInjured) health.Hit();
            carryInjured = false;
        }

        private void OnGUI()
        {
            if (small == null)
            {
                small = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
                small.normal.textColor = new Color(1f, 0.94f, 0.78f);
                big = new GUIStyle(small) { fontSize = 26 };
                title = new GUIStyle(small) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
                wrapped = new GUIStyle(big) { wordWrap = true };
                toastStyle = new GUIStyle(big) { alignment = TextAnchor.UpperCenter };
            }

            // The sun sinks as the battery drains.
            float dim = (1f - Battery01) * 0.35f;
            GUI.color = new Color(0.12f, 0.05f, 0.2f, dim);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Battery and day readout, top right.
            float w = 320f, x = Screen.width - w - 20f;
            GUI.Label(new Rect(x, 10, w, 30), "Day " + Day + "   Money " + Money, small);
            GUI.Label(new Rect(x, 38, w, 30), "Flags " + mines.FlagCount + " / quota " + Quota, small);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(x, 70, w, 20), Texture2D.whiteTexture);
            GUI.color = Battery01 > 0.25f ? new Color(0.5f, 0.9f, 0.4f) : new Color(0.95f, 0.35f, 0.25f);
            GUI.DrawTexture(new Rect(x + 2, 72, (w - 4) * Battery01, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, 92, w, 30), BatteryEmpty ? "DETECTOR DEAD" : "Detector battery", small);
            if (Playing)
            {
                GUI.Label(new Rect(x - 40, 118, w + 40, 30), "Enter: end the day", small);
            }

            if (Time.time < toastUntil)
            {
                GUI.Label(new Rect(Screen.width / 2f - 480, Screen.height - 80, 960, 40), toast, toastStyle);
            }

            if (Phase == DayPhase.Debrief)
            {
                Rect box = new Rect(Screen.width / 2f - 380, Screen.height / 2f - 230, 760, 460);
                GUI.color = new Color(0.1f, 0.08f, 0.06f, 0.95f);
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(box.x, box.y + 14, box.width, 50), reportTitle, title);
                float y = box.y + 80f;
                foreach (string line in report)
                {
                    float textWidth = box.width - 60f;
                    float lineHeight = wrapped.CalcHeight(new GUIContent(line), textWidth);
                    GUI.Label(new Rect(box.x + 30, y, textWidth, lineHeight), line, wrapped);
                    y += lineHeight + 6f;
                }
                if (GUI.Button(new Rect(box.center.x - 160, box.yMax - 70, 320, 50), "Next day (Enter)"))
                {
                    NextDay();
                }
            }
        }
    }
}
