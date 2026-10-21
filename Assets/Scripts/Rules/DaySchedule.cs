using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Rules
{
    /// <summary>The rule list of one campaign day.</summary>
    [Serializable]
    public class DayRules
    {
        public int day;
        /// <summary>Rule ids in page order: "basic" for the basic decrees, any other id is a file in Resources/Rules.</summary>
        public string[] rules;
    }

    [Serializable]
    public class DayScheduleFile
    {
        /// <summary>Used for every day that has no entry of its own.</summary>
        public string[] defaultRules;
        public DayRules[] days;
    }

    /// <summary>
    /// Which rules are in force on which day, written in Resources/Days/days.json:
    /// every day has an array of rule ids ("basic" is one entry of that array), and days without an entry use defaultRules.
    /// </summary>
    public static class DaySchedule
    {
        public const string ResourceFile = "Days/days";
        private static readonly string[] BasicOnly = { RuleIds.Basic };

        private static DayScheduleFile cache;
        private static readonly Dictionary<int, string[]> overrides = new Dictionary<int, string[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache = null;
            overrides.Clear();
        }

        /// <summary>The rule ids for a campaign day, in page order.</summary>
        public static IReadOnlyList<string> RulesFor(int day)
        {
            if (overrides.TryGetValue(day, out string[] forced)) return forced;
            if (cache == null) cache = Load();
            if (cache.days != null)
            {
                foreach (DayRules entry in cache.days)
                {
                    if (entry != null && entry.day == day && entry.rules != null) return entry.rules;
                }
            }
            return cache.defaultRules ?? BasicOnly;
        }

        // ---- cheat tools: try other rule lists without editing days.json

        /// <summary>Forgets the loaded days.json so it is read again.</summary>
        public static void Reload() { cache = null; }
        public static bool HasOverride(int day) { return overrides.ContainsKey(day); }
        public static void SetOverride(int day, string[] rules) { overrides[day] = rules; }
        public static void ClearOverride(int day) { overrides.Remove(day); }

        private static DayScheduleFile Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourceFile);
            if (asset == null)
            {
                Debug.LogError("[Rules] Missing Assets/Resources/" + ResourceFile + ".json, every day gets the basic rules only.");
                return new DayScheduleFile { defaultRules = BasicOnly };
            }
            try
            {
                DayScheduleFile file = JsonUtility.FromJson<DayScheduleFile>(asset.text);
                if (file != null) return file;
            }
            catch (ArgumentException e)
            {
                Debug.LogError("[Rules] " + ResourceFile + ".json is not valid JSON: " + e.Message);
            }
            return new DayScheduleFile { defaultRules = BasicOnly };
        }
    }
}
