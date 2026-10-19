using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Rules
{
    /// <summary>The effect a rule file has. The id inside each JSON file must be one of these.</summary>
    public static class RuleIds
    {
        public const string ProbeLimit = "probe_limit";
        public const string CollectMines = "collect_mines";
        /// <summary>Not a rule file: the entry in a day's rule list that stands for the always-valid decrees of basic_rules.json.</summary>
        public const string Basic = "basic";

        public static bool IsKnown(string id)
        {
            return id == ProbeLimit || id == CollectMines;
        }
    }

    /// <summary>
    /// One rule as written in Assets/Resources/Rules/*.json (field names are the JSON keys).
    /// Fields a rule does not use are left out of its file. The text may use the tokens
    /// {probeCap} {probeFine} {defuseKits} {defuseSeconds} {collectSeconds} {collectQuota} {collectPay} {uncollectedFine}; they are
    /// replaced with the day's real numbers (money already scaled by the difficulty) when the paper is shown.
    /// </summary>
    [Serializable]
    public class RuleDefinition
    {
        public string id;
        /// <summary>Heading on the briefing and in the rulebook.</summary>
        public string title;
        /// <summary>Body text of the rule, with optional number tokens.</summary>
        public string text;
        /// <summary>Poster shown beside the rule in the rulebook (file name in Resources/Rulebook/Posters, no extension).</summary>
        public string poster;

        // ---- probe_limit
        /// <summary>Probes allowed per day for every mine on the field.</summary>
        public float probeCapPerMine;
        /// <summary>Fine for every probe over the limit.</summary>
        public int probeFine;

        // ---- collect_mines
        /// <summary>Defuse kits issued each day. Every defuse attempt spends one, even on safe ground.</summary>
        public int defuseKits;
        /// <summary>Seconds of holding the action to defuse a flagged mine.</summary>
        public float defuseSeconds;
        /// <summary>Seconds of holding the action to collect a mine that is safe to lift.</summary>
        public float collectSeconds;
        /// <summary>Pay per collected mine. Flags alone are not paid on days with this rule.</summary>
        public int collectPay;
        /// <summary>Fine for every located mine (flagged or exposed dud) still in the ground at the end of the day.</summary>
        public int uncollectedFine;
        /// <summary>Mines to collect today to meet the quota (replaces the flag quota on these days).</summary>
        public int collectQuota;

        public bool IsValid(out string problem)
        {
            problem = null;
            if (!RuleIds.IsKnown(id)) problem = "unknown id '" + id + "'";
            else if (string.IsNullOrEmpty(title)) problem = "no title";
            else if (id == RuleIds.ProbeLimit && probeCapPerMine <= 0f) problem = "probeCapPerMine must be above 0";
            else if (id == RuleIds.CollectMines && (defuseSeconds <= 0f || collectSeconds <= 0f)) problem = "defuseSeconds and collectSeconds must be above 0";
            else if (id == RuleIds.CollectMines && defuseKits <= 0) problem = "defuseKits must be above 0";
            else if (id == RuleIds.CollectMines && collectQuota <= 0) problem = "collectQuota must be above 0";
            return problem == null;
        }
    }

    /// <summary>All rule files, loaded once from Assets/Resources/Rules. Which of them are in force on a day is decided by Resources/Days/days.json (DaySchedule).</summary>
    public static class RuleTable
    {
        public const string ResourceFolder = "Rules";

        private static List<RuleDefinition> cache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache = null;
        }

        public static IReadOnlyList<RuleDefinition> All
        {
            get
            {
                if (cache == null) cache = LoadAll();
                return cache;
            }
        }

        private static List<RuleDefinition> LoadAll()
        {
            var rules = new List<RuleDefinition>();
            foreach (TextAsset asset in Resources.LoadAll<TextAsset>(ResourceFolder))
            {
                RuleDefinition rule;
                try
                {
                    rule = JsonUtility.FromJson<RuleDefinition>(asset.text);
                }
                catch (ArgumentException e)
                {
                    Debug.LogError("[Rules] " + asset.name + ".json is not valid JSON: " + e.Message + " Skipped.");
                    continue;
                }

                string problem = "empty file";
                if (rule == null || !rule.IsValid(out problem))
                {
                    Debug.LogError("[Rules] " + asset.name + ".json is invalid (" + problem + "). Skipped.");
                    continue;
                }
                rules.Add(rule);
            }
            return rules;
        }

        /// <summary>The rule with this id, or null when there is no valid file for it.</summary>
        public static RuleDefinition Find(string id)
        {
            foreach (RuleDefinition rule in All)
            {
                if (rule.id == id) return rule;
            }
            return null;
        }
    }
}
