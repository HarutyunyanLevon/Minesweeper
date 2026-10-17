using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MinefieldClerk.Rules
{
    [Serializable]
    public class BasicRulePage
    {
        public string title;
        public string text;
    }

    [Serializable]
    public class BasicRulesFile
    {
        public BasicRulePage[] pages;
        /// <summary>Slogans printed on the slogan band of the rulebook and the briefing, picked by page or day number.</summary>
        public string[] slogans;
        /// <summary>The deminer's family name, the only thing the Ministry calls him by. Fills the {surname} token (placeholder until the family system, M6).</summary>
        public string surname;
    }

    /// <summary>
    /// The always-valid decrees of the Ministry (the lore side of the job), written in Resources/Rulebook/basic_rules.json.
    /// Page text may use the tokens {surname} {flagPay} {wrongFlagPenalty} {quotaBonus} {medicineCost} {batterySeconds};
    /// they are filled with the current difficulty's real numbers (money already scaled).
    /// </summary>
    public static class BasicRules
    {
        public const string ResourceFile = "Rulebook/basic_rules";
        private const string FallbackSurname = "Unknown";

        private static BasicRulesFile cache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache = null;
        }

        /// <summary>The family name the Ministry uses for the deminer.</summary>
        public static string Surname
        {
            get
            {
                if (cache == null) cache = Load();
                return cache.surname;
            }
        }

        public static void AddPages(List<RuleEntry> into, DifficultySettings settings)
        {
            if (cache == null) cache = Load();
            foreach (BasicRulePage page in cache.pages)
            {
                into.Add(new RuleEntry("", page.title, Fill(page.text, settings)));
            }
        }

        /// <summary>A slogan for a page or day number (wraps around). Empty when the file has none.</summary>
        public static string SloganFor(int number)
        {
            if (cache == null) cache = Load();
            int count = cache.slogans.Length;
            if (count == 0) return "";
            return cache.slogans[((number % count) + count) % count];
        }

        private static BasicRulesFile Load()
        {
            var result = new BasicRulesFile { pages = new BasicRulePage[0], slogans = new string[0], surname = FallbackSurname };

            TextAsset asset = Resources.Load<TextAsset>(ResourceFile);
            if (asset == null)
            {
                Debug.LogError("[Rulebook] Missing Assets/Resources/" + ResourceFile + ".json, the rulebook has no basic pages.");
                return result;
            }

            BasicRulesFile file;
            try
            {
                file = JsonUtility.FromJson<BasicRulesFile>(asset.text);
            }
            catch (ArgumentException e)
            {
                Debug.LogError("[Rulebook] " + ResourceFile + ".json is not valid JSON: " + e.Message);
                return result;
            }
            if (file == null) return result;

            var pages = new List<BasicRulePage>();
            if (file.pages != null)
            {
                foreach (BasicRulePage page in file.pages)
                {
                    if (page != null && !string.IsNullOrEmpty(page.title) && !string.IsNullOrEmpty(page.text)) pages.Add(page);
                }
            }
            result.pages = pages.ToArray();
            if (file.slogans != null) result.slogans = file.slogans;
            if (!string.IsNullOrEmpty(file.surname)) result.surname = file.surname;
            return result;
        }

        private static string Fill(string text, DifficultySettings settings)
        {
            text = text.Replace("{surname}", Surname);
            text = text.Replace("{flagPay}", Number(settings.Earned(settings.payPerFlag)));
            text = text.Replace("{wrongFlagPenalty}", Number(settings.Spent(settings.wrongFlagPenalty)));
            text = text.Replace("{quotaBonus}", Number(settings.Earned(settings.quotaBonus)));
            text = text.Replace("{medicineCost}", Number(settings.Spent(settings.medicineCost)));
            text = text.Replace("{batterySeconds}", Number(Mathf.RoundToInt(settings.batterySeconds * settings.radarBattery)));
            return text;
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
