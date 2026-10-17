using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MinefieldClerk.Rules
{
    /// <summary>One page of text as it appears on the briefing and in the rulebook (numbers already filled in). Id is empty for basic pages.</summary>
    public readonly struct RuleEntry
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Text;

        public RuleEntry(string id, string title, string text)
        {
            Id = id;
            Title = title;
            Text = text;
        }
    }

    /// <summary>
    /// The rules in force on one day, with their numbers resolved for the current difficulty.
    /// Built by DayManager when a field is generated; the same day always gives the same rules.
    /// Rules never touch the mine layout itself, they change the actions, the pay and the fines.
    /// </summary>
    public class RuleSet
    {
        private const int DaySeedStride = 7919;     // spreads consecutive day numbers over the random seeds
        private const int MinProbeCap = 1;
        private const string SecondsFormat = "0.#";

        private readonly List<RuleEntry> entries = new List<RuleEntry>();

        /// <summary>No rules at all (before the first field exists).</summary>
        public static readonly RuleSet Empty = new RuleSet();

        public IReadOnlyList<RuleEntry> Entries => entries;
        public int Count => entries.Count;

        // ---- probe_limit
        public bool HasProbeLimit { get; private set; }
        public int ProbeCap { get; private set; }
        public int ProbeFine { get; private set; }

        // ---- collect_mines
        public bool CollectMines { get; private set; }
        public int DefuseKits { get; private set; }
        public int CollectQuota { get; private set; }
        public float DefuseSeconds { get; private set; }
        public float CollectSeconds { get; private set; }
        public int CollectPay { get; private set; }
        public int UncollectedFine { get; private set; }

        /// <summary>Picks the rules for a campaign day: a rule is in force when it is unlocked and its daily chance rolls true.</summary>
        public static RuleSet ForDay(int day, DifficultySettings settings)
        {
            var set = new RuleSet();
            var rng = new System.Random(day * DaySeedStride);
            foreach (RuleDefinition rule in RuleTable.All)
            {
                // Always roll, so one rule's chance never changes what another rule gets.
                bool rolled = rng.NextDouble() < rule.chance;
                if (day >= rule.firstDay && rolled) set.Add(rule, settings);
            }
            return set;
        }

        /// <summary>Probes over the limit for a count of probes made today (0 when there is no limit).</summary>
        public int OverLimit(int probes)
        {
            return HasProbeLimit ? Mathf.Max(0, probes - ProbeCap) : 0;
        }

        private void Add(RuleDefinition rule, DifficultySettings settings)
        {
            switch (rule.id)
            {
                case RuleIds.ProbeLimit:
                    HasProbeLimit = true;
                    ProbeCap = Mathf.Max(MinProbeCap, Mathf.RoundToInt(rule.probeCapPerMine * settings.mines));
                    ProbeFine = settings.Spent(rule.probeFine);
                    break;
                case RuleIds.CollectMines:
                    CollectMines = true;
                    DefuseKits = rule.defuseKits;
                    CollectQuota = rule.collectQuota;
                    DefuseSeconds = rule.defuseSeconds;
                    CollectSeconds = rule.collectSeconds;
                    CollectPay = settings.Earned(rule.collectPay);
                    UncollectedFine = settings.Spent(rule.uncollectedFine);
                    break;
                default:
                    return;
            }
            entries.Add(new RuleEntry(rule.id, rule.title, Describe(rule)));
        }

        private string Describe(RuleDefinition rule)
        {
            string text = rule.text ?? "";
            text = text.Replace("{surname}", BasicRules.Surname);
            text = text.Replace("{probeCap}", ProbeCap.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("{probeFine}", ProbeFine.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("{defuseKits}", DefuseKits.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("{collectQuota}", CollectQuota.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("{defuseSeconds}", DefuseSeconds.ToString(SecondsFormat, CultureInfo.InvariantCulture));
            text = text.Replace("{collectSeconds}", CollectSeconds.ToString(SecondsFormat, CultureInfo.InvariantCulture));
            text = text.Replace("{collectPay}", CollectPay.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("{uncollectedFine}", UncollectedFine.ToString(CultureInfo.InvariantCulture));
            return text;
        }
    }
}
