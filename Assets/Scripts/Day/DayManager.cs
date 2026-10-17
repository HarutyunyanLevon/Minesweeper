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

        // IMGUI placeholder layout (pixels) and look.
        private const int SmallFont = 20, BigFont = 26, TitleFont = 34;
        private const float DimMax = 0.35f;                     // darkest overlay when the battery is empty
        private const float LowBatteryShare = 0.25f;            // below this the bar turns red
        private const float PanelWidth = 320f, PanelMargin = 20f, PanelTop = 10f, RowHeight = 30f;
        private const float Row2Top = 38f, BarTop = 70f, BarHeight = 20f, BarInset = 2f, LabelTop = 92f, HintTop = 118f, HintExtra = 40f;
        private const float ToastWidth = 960f, ToastHeight = 40f, ToastBottomGap = 80f;
        private const float ToastSeconds = 2.5f;
        private const float DebriefWidth = 760f, DebriefHeight = 460f;
        private const float TitleTop = 14f, TitleHeight = 50f, LinesTop = 80f, LineSidePad = 30f, LineGap = 6f;
        private const float ButtonWidth = 320f, ButtonHeight = 50f, ButtonBottomGap = 70f;
        private const string ObjectName = "DayManager";

        private static readonly Color TextColor = new Color(1f, 0.94f, 0.78f);
        private static readonly Color DimColor = new Color(0.12f, 0.05f, 0.2f);
        private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color BarGood = new Color(0.5f, 0.9f, 0.4f);
        private static readonly Color BarLow = new Color(0.95f, 0.35f, 0.25f);
        private static readonly Color PanelColor = new Color(0.1f, 0.08f, 0.06f, 0.95f);

        // Battery length, pay, quota and medicine numbers live in the difficulty JSON files
        // (Assets/Resources/Difficulties), see DifficultySettings.
        [SerializeField] private float probeCostSeconds = 0f;
        [Tooltip("Seconds after a fatal blast before the debrief opens")]
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
        /// <summary>Radar: detector battery length for the current difficulty.</summary>
        private float MaxBattery => session.Settings.batterySeconds * session.Settings.radarBattery;
        public float Battery01 => Mathf.Clamp01(battery / MaxBattery);
        public string DifficultyName => session.Settings.name;

        /// <summary>Flags needed today: the difficulty's base plus a little per day, never more than 80% of the mines.</summary>
        public int Quota
        {
            get
            {
                DifficultySettings s = session.Settings;
                int wanted = s.quotaBase + s.quotaPerDay * (Day - 1);
                return Mathf.Min(wanted, Mathf.RoundToInt(s.mines * s.quotaMaxShare));
            }
        }

        private int Earned(int amount) { return Mathf.RoundToInt(amount * session.Settings.moneyEarn); }
        private int Spent(int amount) { return Mathf.RoundToInt(amount * session.Settings.moneySpend); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<FieldSession>() != null && Object.FindAnyObjectByType<DayManager>() == null)
            {
                new GameObject(ObjectName).AddComponent<DayManager>();
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
            toastUntil = Time.time + ToastSeconds;
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
            battery = MaxBattery;
            deathTimer = 0f;
            Phase = DayPhase.Playing;
            player.Frozen = false;   // a new field can start straight from a debrief (difficulty change, R)
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

            DifficultySettings cfg = session.Settings;
            int flagPay = Earned(cfg.payPerFlag);
            int penalty = Spent(cfg.wrongFlagPenalty);
            int bonusPay = Earned(cfg.quotaBonus);
            int pay = located * flagPay - wrong * penalty;
            int bonus = quotaMet ? bonusPay : 0;
            Money += pay + bonus;

            report.Clear();
            report.Add("Mines located (flagged or exposed duds): " + located + " x " + flagPay + " = " + (located * flagPay));
            if (penalty > 0) report.Add("Wrong flags: " + wrong + " x -" + penalty);
            else if (wrong > 0) report.Add("Flags on safe ground: " + wrong + " (unpaid)");
            report.Add("Quota " + Quota + ": " + (quotaMet ? "MET, bonus +" + bonusPay : "NOT MET, no bonus"));
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
                    int medicine = Spent(session.Settings.medicineCost);
                    if (Money >= medicine)
                    {
                        Money -= medicine;
                        report.Add("Medicine: -" + medicine + " (injury treated)");
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
                small = new GUIStyle(GUI.skin.label) { fontSize = SmallFont, fontStyle = FontStyle.Bold };
                small.normal.textColor = TextColor;
                big = new GUIStyle(small) { fontSize = BigFont };
                title = new GUIStyle(small) { fontSize = TitleFont, alignment = TextAnchor.MiddleCenter };
                wrapped = new GUIStyle(big) { wordWrap = true };
                toastStyle = new GUIStyle(big) { alignment = TextAnchor.UpperCenter };
            }

            // The sun sinks as the battery drains.
            Color dim = DimColor;
            dim.a = (1f - Battery01) * DimMax;
            GUI.color = dim;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Battery and day readout, top right.
            float w = PanelWidth, x = Screen.width - w - PanelMargin;
            GUI.Label(new Rect(x, PanelTop, w, RowHeight), "Day " + Day + " (" + DifficultyName + ")   Money " + Money, small);
            GUI.Label(new Rect(x, Row2Top, w, RowHeight), "Flags " + mines.FlagCount + " / quota " + Quota, small);
            GUI.color = BarBack;
            GUI.DrawTexture(new Rect(x, BarTop, w, BarHeight), Texture2D.whiteTexture);
            GUI.color = Battery01 > LowBatteryShare ? BarGood : BarLow;
            GUI.DrawTexture(new Rect(x + BarInset, BarTop + BarInset, (w - BarInset * 2f) * Battery01, BarHeight - BarInset * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, LabelTop, w, RowHeight), BatteryEmpty ? "DETECTOR DEAD" : "Detector battery", small);
            if (Playing)
            {
                GUI.Label(new Rect(x - HintExtra, HintTop, w + HintExtra, RowHeight), "Enter: end the day", small);
            }

            if (Time.time < toastUntil)
            {
                GUI.Label(new Rect(Screen.width * Num.Half - ToastWidth * Num.Half, Screen.height - ToastBottomGap, ToastWidth, ToastHeight), toast, toastStyle);
            }

            if (Phase == DayPhase.Debrief)
            {
                Rect box = new Rect(Screen.width * Num.Half - DebriefWidth * Num.Half, Screen.height * Num.Half - DebriefHeight * Num.Half, DebriefWidth, DebriefHeight);
                GUI.color = PanelColor;
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(box.x, box.y + TitleTop, box.width, TitleHeight), reportTitle, title);
                float y = box.y + LinesTop;
                foreach (string line in report)
                {
                    float textWidth = box.width - LineSidePad * 2f;
                    float lineHeight = wrapped.CalcHeight(new GUIContent(line), textWidth);
                    GUI.Label(new Rect(box.x + LineSidePad, y, textWidth, lineHeight), line, wrapped);
                    y += lineHeight + LineGap;
                }
                if (GUI.Button(new Rect(box.center.x - ButtonWidth * Num.Half, box.yMax - ButtonBottomGap, ButtonWidth, ButtonHeight), "Next day (Enter)"))
                {
                    NextDay();
                }
            }
        }
    }
}
