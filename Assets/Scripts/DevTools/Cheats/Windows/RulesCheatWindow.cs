using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Day;
using MinefieldClerk.Rules;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// Live editing of the numbers and texts that normally live in JSON files: every rule (Resources/Rules) and every
    /// difficulty (Resources/Difficulties). Edits take effect at once; "Copy JSON" puts the result on the clipboard to paste into the file.
    /// </summary>
    public sealed class RulesCheatWindow : CheatWindow
    {
        private static readonly string[] CommonRuleFields = { "title", "text" };
        private static readonly string[] ProbeLimitFields = { "probeCapPerMine", "probeFine" };
        private static readonly string[] CollectMinesFields = { "defuseKits", "defuseSeconds", "collectSeconds", "collectPay", "uncollectedFine", "collectQuota" };
        private static readonly string[] DifficultyFields =
        {
            "mines", "batterySeconds", "payPerFlag", "wrongFlagPenalty", "quotaBonus", "medicineCost", "quotaPerDay",
            "radarBattery", "moneyEarn", "moneySpend", "encounterRate", "quotaBase", "quotaMaxShare",
        };

        private const string TokenHint = "Rule texts may use {surname} {probeCap} {probeFine} {defuseKits} {collectQuota} {defuseSeconds} {collectSeconds} {collectPay} {uncollectedFine}.";
        private const string DifficultyHint = "Money, battery and quota numbers act at once. Mines and field size apply to the next field.";
        private const string CopyText = "Copy JSON to clipboard";
        private const string SizesLabel = "Field sizes (edit in the JSON): ";

        private DayManager day;
        private Difficulty editing = Difficulty.Medium;

        public override string Id => "rules";
        public override string Title => "Rules & Economy";
        public override Key DefaultToggleKey => Key.F4;

        public override void OnUpdate()
        {
            day = DayManager.Instance;
        }

        protected override void Build(CheatList list)
        {
            Func<bool> has = () => day != null;
            list.Section("Apply");
            list.Button("Rebuild today's rules", () => day.CheatRebuildRules(), "Re-reads the edited numbers into today's rules and rulebook (the field stays)", has);
            list.Button("New field with these numbers", () => day.CheatPlayDay(day.Day), "Regenerates the field so mines count and sizes follow the edits", has);
            list.Button("Reload every JSON file", ReloadAll, "Throws away live edits and re-reads days, rules, rulebook and difficulties from disk", has);
        }

        protected override void DrawBody()
        {
            DrawCheatList();
            DrawRules();
            DrawDifficulties();
        }

        private void DrawRules()
        {
            CheatGui.Heading("Rule files");
            GUILayout.Label(TokenHint, CheatGui.Subtle);
            foreach (RuleDefinition rule in RuleTable.All)
            {
                if (!CheatGui.Foldout("rules/" + rule.id, rule.id)) continue;
                bool changed = false;
                CheatGui.Indent(() =>
                {
                    changed |= CheatGui.EditFields(rule, "rules." + rule.id, CommonRuleFields);
                    changed |= CheatGui.EditFields(rule, "rules." + rule.id, FieldsOf(rule.id));
                    if (GUILayout.Button(CopyText)) GUIUtility.systemCopyBuffer = JsonUtility.ToJson(rule, true);
                });
                if (changed && day != null) day.CheatRebuildRules();
            }
        }

        private static string[] FieldsOf(string ruleId)
        {
            if (ruleId == RuleIds.ProbeLimit) return ProbeLimitFields;
            if (ruleId == RuleIds.CollectMines) return CollectMinesFields;
            return Array.Empty<string>();
        }

        private void DrawDifficulties()
        {
            CheatGui.Heading("Difficulty");
            GUILayout.Label(DifficultyHint, CheatGui.Subtle);
            string[] names = Enum.GetNames(typeof(Difficulty));
            int picked = GUILayout.SelectionGrid((int)editing, names, names.Length);
            editing = (Difficulty)picked;

            DifficultySettings settings = DifficultyTable.Get(editing);
            CheatGui.EditFields(settings, "difficulty." + editing, DifficultyFields);
            GUILayout.Label(SizesLabel + DescribeSizes(settings), CheatGui.Subtle);
            if (GUILayout.Button(CopyText)) GUIUtility.systemCopyBuffer = JsonUtility.ToJson(settings, true);
        }

        private static string DescribeSizes(DifficultySettings settings)
        {
            var parts = new string[settings.sizes.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = settings.sizes[i].width + "x" + settings.sizes[i].height;
            return string.Join(", ", parts);
        }

        private void ReloadAll()
        {
            DaySchedule.Reload();
            RuleTable.Reload();
            BasicRules.Reload();
            DifficultyTable.Reload();
            day.CheatRebuildRules();
        }
    }
}
