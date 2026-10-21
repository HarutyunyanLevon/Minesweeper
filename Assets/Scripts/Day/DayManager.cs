using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Family;
using MinefieldClerk.Field;
using MinefieldClerk.Player;
using MinefieldClerk.Rules;
using MinefieldClerk.Sound;

namespace MinefieldClerk.Day
{
    public enum DayPhase { Briefing, Playing, Debrief }

    /// <summary>
    /// M3 day loop: detector battery, end of day, debrief with pay per correct flag, quota bonus, injury and medicine.
    /// Its screens are uGUI prefabs (HUD, briefing book, debrief report, expenses). Finds the scene objects by itself,
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

        private const string ObjectName = "DayManager";
        private const string HudPrefab = "UI/Hud/HudWindow", BookPrefab = "UI/Windows/RulebookWindow", DebriefPrefab = "UI/Windows/DebriefWindow", ExpensesPrefab = "UI/Windows/ExpensesWindow";

        // Battery length, pay, quota and medicine numbers live in the difficulty JSON files
        // (Assets/Resources/Difficulties), see DifficultySettings.
        [SerializeField] private float probeCostSeconds = 0f;
        [Tooltip("Seconds after a fatal blast before the debrief opens")]
        [SerializeField] private float deathDelay = 2.5f;

        private const string BookStamp = "AMENDED";
        private const string BookFooter = "A / D or arrow keys: turn the page      B: close the Book";
        private const string BriefingFooterPrefix = "A / D: turn the page      Enter or B: begin the shift      The Republic is counting on you, citizen ";
        private const string EndBlockedText = "The quota is not met. The shift continues.";
        private const string WorkHint = "Hold E: disarm / lift ordnance";
        private const string HudKeyHint = "Enter: end the day   B: rulebook";
        private const string HudKeyHintLocked = "B: rulebook";
        private const float BatteryLowShare = 0.2f;        // the low-battery warning sounds at this share of the battery left
        private const float StampDelaySeconds = 0.8f;      // the approval stamp lands this long after the report opens

        private RulebookWindow bookWin;
        private DebriefWindow debriefWin;
        private ExpensesWindow expensesWin;
        private HudView hudView;
        private List<ExpenseLine> expenseLines = new List<ExpenseLine>();
        private int bookShown;   // 0 none, 1 briefing, 2 opened in the field
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
        private bool batteryWarned;
        private float deathTimer;
        private bool carryInjured;
        private string reportTitle = "";

        /// <summary>Ids of optional expenses skipped last evening (the family system reacts to these later).</summary>
        public readonly List<string> UnpaidExpenses = new List<string>();

        public DayPhase Phase { get; private set; } = DayPhase.Playing;
        public int Day { get; private set; } = 1;
        public int Money { get; private set; }
        public bool Playing => Phase == DayPhase.Playing;
        /// <summary>The shift is running and the rulebook is shut: the deminer may act.</summary>
        public bool CanAct => Playing && !showRulebook;
        /// <summary>The Book of Regulations is open over the field.</summary>
        public bool BookOpen => showRulebook;
        /// <summary>The family page (the evening expenses) is on screen.</summary>
        public bool ExpensesOpen => expensesWin != null && expensesWin.Visible;
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
            if (hudView != null) hudView.ShowToast(text);
        }

        private void Awake()
        {
            Instance = this;
            session = FindAnyObjectByType<FieldSession>();
            mines = FindAnyObjectByType<MineField>();
            health = FindAnyObjectByType<PlayerHealth>();
            player = FindAnyObjectByType<PlayerController>();
            hudView = UiCanvas.Spawn<HudView>(HudPrefab, UiCanvas.Layer.Hud);
            bookWin = UiCanvas.Spawn<RulebookWindow>(BookPrefab);
            debriefWin = UiCanvas.Spawn<DebriefWindow>(DebriefPrefab);
            expensesWin = UiCanvas.Spawn<ExpensesWindow>(ExpensesPrefab);
            bookWin.Hide(); bookWin.Closed += OnBookClosed;
            debriefWin.Hide(); debriefWin.Finished += OnReportDone;
            expensesWin.Hide(); expensesWin.Finished += NextDay;
        }

        private void LateUpdate()
        {
            SyncBook();
            UpdateHud();
        }

        private void UpdateHud()
        {
            bool show = Phase == DayPhase.Playing && !showRulebook;
            hudView.SetContentVisible(show);
            hudView.Apply(new HudState
            {
                Day = Day, Difficulty = DifficultyName, Money = Money,
                CollectMode = rules.CollectMines, Collected = mines.CollectedCount, Flags = mines.FlagCount, Quota = Quota,
                Battery01 = Battery01, BatteryEmpty = BatteryEmpty,
                HasProbeLimit = rules.HasProbeLimit, Probes = probesToday, ProbeCap = rules.ProbeCap,
                HasKits = rules.CollectMines, KitsLeft = KitsLeft, KitsTotal = rules.DefuseKits,
                KeyHint = (BatteryEmpty || QuotaMet) ? HudKeyHint : HudKeyHintLocked, WorkHint = WorkHint,
            });
        }

        /// <summary>Shows or hides the book window to match the phase.</summary>
        private void SyncBook()
        {
                        int want = Phase == DayPhase.Briefing ? 1 : (Phase == DayPhase.Playing && showRulebook ? 2 : 0);
            if (want == bookShown) return;
            bookShown = want;
            if (want == 0)
            {
                bookWin.Hide();
                return;
            }
            bookWin.Show(rulebook, BookStamp, want == 1 ? BriefingFooterPrefix + BasicRules.Surname : BookFooter);
            Sfx.Play(SfxEvent.BookOpen);
        }

        private void OnBookClosed()
        {
            if (Phase == DayPhase.Briefing) StartShift();
            else ToggleRulebook();
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
            bookShown = 0;   // the window is shown again for the new briefing
            debriefWin.Hide();
            expensesWin.Hide();
            rulebook.Rebuild(Day, rules);
            battery = MaxBattery;
            batteryWarned = false;
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
                TurnPagesByKey(kb);
                if (enter || (kb != null && kb.bKey.wasPressedThisFrame)) StartShift();   // B closes the book it opened with
                return;
            }

            if (Phase == DayPhase.Playing)
            {
                if (kb != null && kb.bKey.wasPressedThisFrame) ToggleRulebook();
                if (showRulebook)
                {
                    // Reading is free: the deminer stands still and the detector clock stops.
                    player.Frozen = true;
                    TurnPagesByKey(kb);
                    return;
                }
                if (DevTools.DevMode.Enabled && kb != null && kb.f9Key.wasPressedThisFrame) { EndDay(); return; }   // dev: skip the quota gate
                if (!BatteryEmpty) battery = Mathf.Max(0f, battery - Time.deltaTime);
                WarnLowBattery();

                if (health.Dead)
                {
                    deathTimer += Time.deltaTime;
                    if (deathTimer >= deathDelay && !mines.HasArmed) EndDay();
                }
                else if (enter)
                {
                    if (mines.HasArmed) Notify("A fuse is still burning.");
                    else if (!BatteryEmpty && !QuotaMet) Notify(EndBlockedText);
                    else EndDay();
                }
            }
            else if (enter)
            {
                if (expensesWin.Visible) expensesWin.Confirm();
                else if (debriefWin.Visible) debriefWin.Confirm();
            }
        }

        /// <summary>One warning when the detector battery runs low (the wind-down of the shift).</summary>
        private void WarnLowBattery()
        {
            if (batteryWarned || BatteryEmpty || battery > MaxBattery * BatteryLowShare) return;
            batteryWarned = true;
            Sfx.Play(SfxEvent.BatteryLow);
        }

        private void TurnPagesByKey(Keyboard kb)
        {
            if (kb == null) return;
            int step = 0;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) step--;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) step++;
            if (step == 0) return;
            int spread = rulebook.SpreadIndex;
            rulebook.Turn(step);
            if (rulebook.SpreadIndex != spread) Sfx.Play(SfxEvent.PageTurn);
            bookWin.Refresh();
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

        /// <summary>Quota reached: flags and located duds, or mines collected under the collect rule.</summary>
        private bool QuotaMet
        {
            get
            {
                mines.Tally(out int correct, out int wrong);
                return (rules.CollectMines ? mines.CollectedCount : correct + mines.DudCount) >= Quota;
            }
        }

        public void EndDay()
        {
            mines.Tally(out int correct, out int wrong);
            int located = correct + mines.DudCount;                  // flagged mines and exposed duds
            int inGround = correct + mines.UncollectedDudCount;      // located but not collected
            bool collecting = rules.CollectMines;
            bool quotaMet = QuotaMet;

            DifficultySettings cfg = session.Settings;
            int flagPay = Earned(cfg.payPerFlag);
            int penalty = Spent(cfg.wrongFlagPenalty);
            int bonusPay = Earned(cfg.quotaBonus);
            int bonus = quotaMet ? bonusPay : 0;
            int probesOver = rules.OverLimit(probesToday);
            int probeFines = probesOver * rules.ProbeFine;

            List<ReportRow> rows = new List<ReportRow>();
            int pay;
            if (collecting)
            {
                int collectedPay = mines.CollectedCount * rules.CollectPay;
                int groundFines = inGround * rules.UncollectedFine;
                pay = collectedPay - groundFines - wrong * penalty;
                rows.Add(Row("Mines collected  " + mines.CollectedCount + " x " + rules.CollectPay, "+" + collectedPay));
                rows.Add(Row("Defuse kits used", kitsUsed + " of " + rules.DefuseKits));
                if (inGround > 0) rows.Add(Row("Ordnance left in the field  " + inGround + " x " + rules.UncollectedFine, "-" + groundFines, true));
            }
            else
            {
                pay = located * flagPay - wrong * penalty;
                rows.Add(Row("Mines located  " + located + " x " + flagPay, "+" + located * flagPay));
            }
            Money += pay + bonus - probeFines;

            if (penalty > 0 && wrong > 0) rows.Add(Row("Specialists sent in vain  " + wrong + " x " + penalty, "-" + wrong * penalty, true));
            else if (wrong > 0) rows.Add(Row("Specialists sent in vain", wrong.ToString(), true));
            if (rules.HasProbeLimit)
            {
                rows.Add(Row("Probe limit " + rules.ProbeCap + "  used " + probesToday, probesOver > 0 ? "-" + probeFines : "within allowance", probesOver > 0));
            }
            rows.Add(quotaMet ? Row("Quota " + Quota + " fulfilled", "+" + bonusPay) : Row("Quota " + Quota + " not fulfilled", "noted", true));
            rows.Add(Row("Exploded " + mines.ExplodedCount + "   Probed " + mines.ProbedCount + "   Flags " + mines.FlagCount, ""));

            carryInjured = false;
            List<ExpenseLine> lines = new List<ExpenseLine>();
            if (health.Dead)
            {
                reportTitle = "REGISTER ENTRY CLOSED - DAY " + Day;
                rows.Add(Row("Citizen " + BasicRules.Surname + " is struck from the register.", "", true));
                rows.Add(Row("A brother takes your boots and your place at the gate.", ""));
            }
            else
            {
                reportTitle = "STATE REPORT - DAY " + Day;
                lines = BuildExpenses();
            }
            rows.Add(Row("Balance", Money.ToString()));

            expenseLines = lines;
            debriefWin.Open(reportTitle, rows, lines.Count == 0);
            Phase = DayPhase.Debrief;
            player.Frozen = true;

            Sfx.Play(SfxEvent.DayEndBell);
            if (quotaMet && !health.Dead) Sfx.PlayDelayed(SfxEvent.StampApproved, StampDelaySeconds);
        }

        private static ReportRow Row(string label, string value, bool bad = false)
        {
            return new ReportRow { Label = label, Value = value, Bad = bad };
        }

        /// <summary>Optional expenses for the family page; all start ticked while the money lasts.</summary>
        private List<ExpenseLine> BuildExpenses()
        {
            List<ExpenseLine> result = new List<ExpenseLine>();
            int left = Money;
            foreach (ExpenseDef def in ExpenseTable.Load())
            {
                bool medicine = def.kind == ExpenseTable.KindMedicine;
                if (medicine && !health.Injured) continue;
                int cost = Spent(medicine ? session.Settings.medicineCost : def.cost);
                bool afford = cost <= left;
                if (afford) left -= cost;
                result.Add(new ExpenseLine { Def = def, Cost = cost, Checked = afford });
            }
            return result;
        }

        /// <summary>The report was read: the family page follows when there is anything to pay, otherwise the next day starts.</summary>
        private void OnReportDone()
        {
            debriefWin.Hide();
            if (expenseLines.Count > 0) expensesWin.Open(expenseLines, Money);
            else NextDay();
        }

        public void NextDay()
        {
            carryInjured = false;
            UnpaidExpenses.Clear();
            debriefWin.Hide();
            expensesWin.Hide();
            foreach (ExpenseLine l in expenseLines)
            {
                if (l.Checked) Money -= l.Cost;
                else
                {
                    UnpaidExpenses.Add(l.Def.id);   // placeholder: the family system will react to these
                    if (l.Def.kind == ExpenseTable.KindMedicine) carryInjured = true;
                }
            }
            Day++;
            session.NewDay();   // regenerates the field; OnRegenerated resets battery and phase
            if (carryInjured) health.Hit();
            carryInjured = false;
        }
    }
}
