using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Day;
using MinefieldClerk.Rules;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>Play or inspect any campaign day: jump to it, read its rules, open the day's screens, change the shift (battery, money, kits) and override which rules a day uses.</summary>
    public sealed class DayCheatWindow : CheatWindow
    {
        private const string SchedulePrefix = "days.json order: ";
        private const string OverrideNote = "  (cheat override active)";
        private const string RulesChangedNote = "   - new or changed";
        private const string NoDay = "The day system is not up in this scene.";
        private const string ApplyHint = "Rebuilds today's rules and rulebook without touching the field. Pages and fines follow the new list; a briefing already read is not repeated.";
        private const string PreviewHint = "Opens the Book of Regulations on the chosen day's rules (as the briefing would show them) without changing the day.";

        private DayManager day;

        private int targetDay = 1;
        private bool targetInitialised;
        private int revision;
        private string previewKey = "";

        private readonly List<RuleEntry> entries = new List<RuleEntry>();
        private readonly List<bool> entryChanged = new List<bool>();
        private readonly List<string> optionIds = new List<string>();
        private readonly List<bool> optionOn = new List<bool>();
        private string scheduleLine = "";
        private string status = "";

        public override string Id => "days";
        public override string Title => "Days & Rules";
        public override Key DefaultToggleKey => Key.F3;

        public override void OnUpdate()
        {
            day = DayManager.Instance;
            if (day == null)
            {
                status = NoDay;
                return;
            }
            if (!targetInitialised)
            {
                targetInitialised = true;
                targetDay = day.Day;
            }
            status = "Day " + day.Day + "   " + day.Phase + "   " + day.DifficultyName + "   Money " + day.Money + "   Quota " + day.Quota +
                "   Probes " + day.ProbesToday + "   Kits " + day.KitsLeft + "   Battery " + Mathf.RoundToInt(day.BatterySeconds) + "/" + Mathf.RoundToInt(day.MaxBatterySeconds) + " s";
            RefreshPreview();
        }

        /// <summary>Rebuilds the rule preview and the rule checkboxes only when the target day, difficulty or rule files change.</summary>
        private void RefreshPreview()
        {
            string key = targetDay + "|" + day.DifficultyName + "|" + revision;
            if (key == previewKey) return;
            previewKey = key;

            entries.Clear();
            entryChanged.Clear();
            RuleSet today = RuleSet.ForDay(targetDay, day.Settings);
            RuleSet before = RuleSet.ForDay(Mathf.Max(1, targetDay - 1), day.Settings);
            var yesterdayText = new Dictionary<string, string>();
            foreach (RuleEntry entry in before.Entries) yesterdayText[entry.Id] = entry.Text;
            foreach (RuleEntry entry in today.Entries)
            {
                entries.Add(entry);
                bool changed = entry.Id != RuleIds.Basic && (targetDay == 1 || !yesterdayText.TryGetValue(entry.Id, out string old) || old != entry.Text);
                entryChanged.Add(changed);
            }

            IReadOnlyList<string> ids = DaySchedule.RulesFor(targetDay);
            scheduleLine = SchedulePrefix + string.Join(", ", ids) + (DaySchedule.HasOverride(targetDay) ? OverrideNote : "");

            optionIds.Clear();
            optionOn.Clear();
            optionIds.Add(RuleIds.Basic);
            foreach (RuleDefinition rule in RuleTable.All) optionIds.Add(rule.id);
            foreach (string id in optionIds)
            {
                bool on = false;
                foreach (string scheduled in ids) on |= scheduled == id;
                optionOn.Add(on);
            }
        }

        protected override void Build(CheatList list)
        {
            Func<bool> has = () => day != null;
            Func<bool> hasCollect = () => day != null && day.Rules.CollectMines;

            list.Section("Jump to a day");
            list.Int("Target day", () => targetDay, v => targetDay = v, 1, CheatLimits.CampaignDays, 1, 1, "The day the buttons below and the rule list refer to");
            list.Button("Play target day", () => day.CheatPlayDay(targetDay), "New field for that day with that day's rules and its briefing", has);
            list.Button("Previous day", () => { targetDay = Mathf.Max(1, day.Day - 1); day.CheatPlayDay(targetDay); }, null, has);
            list.Button("Next day", () => { targetDay = Mathf.Min(CheatLimits.CampaignDays, day.Day + 1); day.CheatPlayDay(targetDay); }, null, has);
            list.Button("Restart this day", () => day.CheatPlayDay(day.Day), "New field, same day", has);
            list.Button("Skip briefing", () => day.CheatSkipBriefing(), "Starts the shift at once", has);

            list.Section("Day screens");
            list.Button("Show rules of target day", () => day.CheatPreviewRules(targetDay), PreviewHint, has);
            list.Button("Show rules of current day", () => day.CheatPreviewRules(day.Day), null, has);
            list.Button("End shift and show report", () => day.CheatShowReport(), "The state report, as after pressing Enter", has);
            list.Button("Show family expenses page", () => day.CheatShowExpenses(), null, has);
            list.Button("Show a toast", () => day.Notify("Cheat toast: the Ministry is watching."), null, has);

            list.Section("Shift");
            list.Float("Battery seconds", () => day != null ? day.BatterySeconds : 0f, v => { if (day != null) day.BatterySeconds = v; }, 0f, CheatLimits.MaxBatterySeconds, CheatLimits.BatteryStep, 0f, "Seconds of detector battery left", () => day != null ? day.MaxBatterySeconds : 0f);
            list.Toggle("Freeze battery", () => CheatState.FreezeBattery, v => CheatState.FreezeBattery = v, "The detector never drains").Saved();
            list.Button("Refill battery", () => day.BatterySeconds = day.MaxBatterySeconds, null, has);
            list.Button("Empty battery", () => day.BatterySeconds = 0f, null, has);
            list.Int("Money", () => day != null ? day.Money : 0, v => { if (day != null) day.CheatSetMoney(v); }, 0, CheatLimits.MaxMoney, CheatLimits.MoneySmall);
            list.Button("Add money +100", () => day.CheatSetMoney(day.Money + CheatLimits.MoneyMedium), null, has);
            list.Button("Add money +1000", () => day.CheatSetMoney(day.Money + CheatLimits.MoneyLarge), null, has);
            list.Button("Take money -100", () => day.CheatSetMoney(Mathf.Max(0, day.Money - CheatLimits.MoneyMedium)), null, has);
            list.Int("Probes used", () => day != null ? day.ProbesToday : 0, v => { if (day != null) day.CheatSetProbes(v); }, 0, CheatLimits.MaxProbes);
            list.Int("Defuse kits left", () => day != null ? day.KitsLeft : 0, v => { if (day != null) day.CheatSetKitsLeft(v); }, 0, CheatLimits.MaxKits, 1, 0, "Only on days with the collect-mines rule");
            list.Toggle("Unlimited kits", () => CheatState.UnlimitedKits, v => CheatState.UnlimitedKits = v, "Defusing never uses a kit up").Saved();
            list.Button("Meet today's quota", () => day.CheatMeetQuota(), "Flags (or flags, defuses and collects) mines until the quota is reached", has);
        }

        protected override void DrawBody()
        {
            GUILayout.Label(status, CheatGui.Wrap);
            if (day == null) return;
            DrawCheatList();

            CheatGui.Heading("Rules of day " + targetDay);
            GUILayout.Label(scheduleLine, CheatGui.Subtle);
            for (int i = 0; i < entries.Count; i++)
            {
                RuleEntry entry = entries[i];
                string title = entry.Title + (entryChanged[i] ? RulesChangedNote : "");
                if (!CheatGui.Foldout("days/rule/" + entry.Id + i, title)) continue;
                CheatGui.Indent(() => GUILayout.Label(entry.Text, CheatGui.Wrap));
            }

            CheatGui.Heading("Choose the rules of day " + targetDay);
            GUILayout.Label(ApplyHint, CheatGui.Subtle);
            for (int i = 0; i < optionIds.Count; i++)
            {
                optionOn[i] = GUILayout.Toggle(optionOn[i], optionIds[i]);
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Use this list")) ApplyOverride();
            if (GUILayout.Button("Back to days.json")) ClearOverride();
            if (GUILayout.Button("Reload JSON files")) ReloadFiles();
            GUILayout.EndHorizontal();
        }

        private void ApplyOverride()
        {
            var chosen = new List<string>();
            for (int i = 0; i < optionIds.Count; i++)
            {
                if (optionOn[i]) chosen.Add(optionIds[i]);
            }
            DaySchedule.SetOverride(targetDay, chosen.ToArray());
            AfterRuleChange();
        }

        private void ClearOverride()
        {
            DaySchedule.ClearOverride(targetDay);
            AfterRuleChange();
        }

        /// <summary>Re-reads days.json, the rule files and the basic rulebook pages (after editing them by hand).</summary>
        private void ReloadFiles()
        {
            DaySchedule.Reload();
            RuleTable.Reload();
            BasicRules.Reload();
            AfterRuleChange();
        }

        private void AfterRuleChange()
        {
            revision++;
            if (day != null && targetDay == day.Day) day.CheatRebuildRules();
        }
    }
}
