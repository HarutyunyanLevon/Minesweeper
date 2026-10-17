using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;
using MinefieldClerk.Player;
using MinefieldClerk.Rules;

namespace MinefieldClerk.Day
{
    public enum DayPhase { Briefing, Playing, Debrief }

    /// <summary>
    /// M3 day loop: detector battery, end of day, debrief with pay per correct flag, quota bonus, injury and medicine.
    /// Placeholder IMGUI screens until the real briefing/debrief art exists. Finds the scene objects by itself,
    /// so it needs no scene wiring (it spawns when a FieldSession exists).
    /// Battery drains with time (the "sun" dims) and every probe costs a little extra. At 0 the detector is dead:
    /// no more probing, but flagging from memory and ending the day still work.
    /// Pay: flags on real mines (and located duds) are paid, quota met gives a bonus, a first hit costs medicine.
    /// A second hit ends the day; money carries over and a relative takes over the next day (family economy: M6).
    /// M4 rules: every new field picks that day's rules (RuleSet). A briefing paper lists them before the shift
    /// (Enter starts it), B opens the rulebook during the shift. The probe limit fines extra probes; the collect-mines
    /// rule pays for collected mines instead of flags and fines located mines left in the ground. Fines are applied in the debrief.
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
        private const float RuleRowTop = HintTop + RowHeight;
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

        private const string BookHeading = "BOOK OF REGULATIONS";
        private const string BookBasicSection = "Basic decree of the Ministry";
        private const string BookSpecialSection = "Directive of the day";
        private const string BookStamp = "AMENDED";
        private const string BookFooter = "A / D or arrow keys: turn the page      B: close the Book";
        private const string BriefingHeading = "DAILY DIRECTIVE - DAY ";
        private const string BriefingFooterPrefix = "Enter: begin the shift      The Republic is counting on you, citizen ";
        private const string WorkHint = "Hold E: disarm / lift ordnance";

        private readonly RulePaper paper = new RulePaper();
        private readonly Rulebook rulebook = new Rulebook();
        private RuleSet rules = RuleSet.Empty;
        private int probesToday;
        private int kitsUsed;
        private bool showRulebook;

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
        /// <summary>The shift is running and the rulebook is shut: the deminer may act.</summary>
        public bool CanAct => Playing && !showRulebook;
        /// <summary>The rules in force today.</summary>
        public RuleSet Rules => rules;
        /// <summary>Defuse kits still in the bag (0 on days without the collect-mines rule).</summary>
        public int KitsLeft => rules.CollectMines ? Mathf.Max(0, rules.DefuseKits - kitsUsed) : 0;
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
                int maxMines = Mathf.RoundToInt(s.mines * s.quotaMaxShare);
                // The collect-mines rule sets its own quota (mines to deliver) instead of the flag quota.
                return rules.CollectMines ? Mathf.Min(rules.CollectQuota, maxMines) : Mathf.Min(wanted, maxMines);
            }
        }

        private int Earned(int amount) { return session.Settings.Earned(amount); }
        private int Spent(int amount) { return session.Settings.Spent(amount); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<FieldSession>() != null && Object.FindAnyObjectByType<DayManager>() == null)
            {
                new GameObject(ObjectName).AddComponent<DayManager>();
            }
        }

        /// <summary>Probing needs battery and an active day.</summary>
        public bool CanProbe => CanAct && !BatteryEmpty;

        /// <summary>A defuse attempt was made: one kit is used up whatever the flag turned out to cover.</summary>
        public void SpendKit()
        {
            kitsUsed++;
        }

        /// <summary>A probe was made: costs battery and counts towards the probe limit.</summary>
        public void RegisterProbe()
        {
            battery = Mathf.Max(0f, battery - probeCostSeconds);
            probesToday++;
            if (rules.HasProbeLimit && probesToday > rules.ProbeCap)
            {
                Notify("Over the probe limit. Fined " + rules.ProbeFine + " per extra probe at the end of the shift.");
            }
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
            rules = RuleSet.ForDay(Day, session.Settings);
            probesToday = 0;
            kitsUsed = 0;
            showRulebook = false;
            rulebook.Rebuild(Day, rules, session.Settings);
            battery = MaxBattery;
            deathTimer = 0f;
            // A new field can start straight from a debrief (difficulty change, R). With rules in force the shift
            // starts with the briefing; Update keeps the deminer frozen until it is dismissed.
            Phase = rules.Count > 0 ? DayPhase.Briefing : DayPhase.Playing;
            player.Frozen = Phase == DayPhase.Briefing;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            bool enter = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);

            if (Phase == DayPhase.Briefing)
            {
                player.Frozen = true;   // the health reset runs after the field is generated and would unfreeze the deminer
                if (enter) StartShift();
                return;
            }

            if (Phase == DayPhase.Playing)
            {
                if (kb != null && kb.bKey.wasPressedThisFrame) ToggleRulebook();
                if (showRulebook)
                {
                    // Reading is free: the deminer stands still and the detector clock stops.
                    player.Frozen = true;
                    if (kb != null)
                    {
                        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) rulebook.Turn(-1);
                        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) rulebook.Turn(1);
                    }
                    return;
                }
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

        private void ToggleRulebook()
        {
            if (showRulebook)
            {
                showRulebook = false;
                player.Frozen = health.Dead;
                return;
            }
            if (health.Dead) return;
            if (mines.HasArmed)
            {
                Notify("A fuse is still burning.");
                return;
            }
            showRulebook = true;
        }

        private void StartShift()
        {
            Phase = DayPhase.Playing;
            player.Frozen = health.Dead;
        }

        public void EndDay()
        {
            mines.Tally(out int correct, out int wrong);
            int located = correct + mines.DudCount;                  // flagged mines and exposed duds
            int inGround = correct + mines.UncollectedDudCount;      // located but not collected
            int flagsPlaced = mines.FlagCount;
            bool collecting = rules.CollectMines;
            int counted = collecting ? mines.CollectedCount : located;   // what the quota counts
            bool quotaMet = counted >= Quota;

            DifficultySettings cfg = session.Settings;
            int flagPay = Earned(cfg.payPerFlag);
            int penalty = Spent(cfg.wrongFlagPenalty);
            int bonusPay = Earned(cfg.quotaBonus);
            int bonus = quotaMet ? bonusPay : 0;
            int probesOver = rules.OverLimit(probesToday);
            int probeFines = probesOver * rules.ProbeFine;

            report.Clear();
            int pay;
            if (collecting)
            {
                int collectedPay = mines.CollectedCount * rules.CollectPay;
                int groundFines = inGround * rules.UncollectedFine;
                pay = collectedPay - groundFines - wrong * penalty;
                report.Add("Mines collected: " + mines.CollectedCount + " x " + rules.CollectPay + " = " + collectedPay);
                report.Add("Defuse kits used: " + kitsUsed + " of " + rules.DefuseKits);
                if (inGround > 0) report.Add("Ordnance abandoned in the field: " + inGround + " x -" + rules.UncollectedFine);
            }
            else
            {
                pay = located * flagPay - wrong * penalty;
                report.Add("Mines located (flagged or exposed): " + located + " x " + flagPay + " = " + (located * flagPay));
            }
            Money += pay + bonus - probeFines;

            if (penalty > 0) report.Add("Specialists sent in vain: " + wrong + " x -" + penalty);
            else if (wrong > 0) report.Add("Specialists sent in vain: " + wrong + " (noted)");
            if (rules.HasProbeLimit)
            {
                report.Add("Probe limit " + rules.ProbeCap + ": " + probesToday + " used" +
                           (probesOver > 0 ? ", " + probesOver + " beyond allowance x -" + rules.ProbeFine : ", within allowance"));
            }
            report.Add("Quota " + Quota + (collecting ? " collected" : "") + ": " + (quotaMet ? "MET, bonus +" + bonusPay : "NOT MET. The shortfall is noted."));
            report.Add("Mines exploded: " + mines.ExplodedCount + "    Tiles probed: " + mines.ProbedCount + "    Flags placed: " + flagsPlaced);

            carryInjured = false;
            if (health.Dead)
            {
                reportTitle = "REGISTER ENTRY CLOSED - DAY " + Day;
                report.Add("Citizen " + BasicRules.Surname + " is struck from the register.");
                report.Add("A brother takes your boots and your place at the gate. The family still has to eat.");
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
                        report.Add("Treatment declined: insufficient funds. Report for duty as you are.");
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

        private void DrawRulebook()
        {
            int turn = paper.DrawPage(BookHeading, rulebook.IsSpecialPage ? BookSpecialSection : BookBasicSection,
                rulebook.Current, rulebook.Index, rulebook.PageCount, rulebook.Slogan, rulebook.IsChangedPage ? BookStamp : "", BookFooter);
            rulebook.Turn(turn);
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
            GUI.Label(new Rect(x, Row2Top, w, RowHeight),
                rules.CollectMines ? "Collected " + mines.CollectedCount + " / quota " + Quota : "Flags " + mines.FlagCount + " / quota " + Quota, small);
            GUI.color = BarBack;
            GUI.DrawTexture(new Rect(x, BarTop, w, BarHeight), Texture2D.whiteTexture);
            GUI.color = Battery01 > LowBatteryShare ? BarGood : BarLow;
            GUI.DrawTexture(new Rect(x + BarInset, BarTop + BarInset, (w - BarInset * 2f) * Battery01, BarHeight - BarInset * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, LabelTop, w, RowHeight), BatteryEmpty ? "DETECTOR DEAD" : "Detector battery", small);
            if (Playing)
            {
                GUI.Label(new Rect(x - HintExtra, HintTop, w + HintExtra, RowHeight), "Enter: end the day   B: rulebook", small);
                float ruleRow = RuleRowTop;
                if (rules.HasProbeLimit)
                {
                    GUI.contentColor = probesToday > rules.ProbeCap ? BarLow : Color.white;
                    GUI.Label(new Rect(x, ruleRow, w, RowHeight), "Probes " + probesToday + " / " + rules.ProbeCap, small);
                    GUI.contentColor = Color.white;
                    ruleRow += RowHeight;
                }
                if (rules.CollectMines)
                {
                    GUI.contentColor = KitsLeft == 0 ? BarLow : Color.white;
                    GUI.Label(new Rect(x, ruleRow, w, RowHeight), "Defuse kits " + KitsLeft + " / " + rules.DefuseKits, small);
                    GUI.contentColor = Color.white;
                    ruleRow += RowHeight;
                    GUI.Label(new Rect(x - HintExtra, ruleRow, w + HintExtra, RowHeight), WorkHint, small);
                }
            }

            if (Time.time < toastUntil)
            {
                GUI.Label(new Rect(Screen.width * Num.Half - ToastWidth * Num.Half, Screen.height - ToastBottomGap, ToastWidth, ToastHeight), toast, toastStyle);
            }

            if (Phase == DayPhase.Briefing) paper.Draw(BriefingHeading + Day, BriefingFooterPrefix + BasicRules.Surname, BasicRules.SloganFor(Day), rules);
            else if (Phase == DayPhase.Playing && showRulebook) DrawRulebook();

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
