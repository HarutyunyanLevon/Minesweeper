using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk
{
    public enum Difficulty { Easy, Medium, Hard }

    [Serializable]
    public class FieldSize
    {
        public int width;
        public int height;
    }

    /// <summary>
    /// Everything a difficulty changes. Loaded from Assets/Resources/Difficulties/{easy,medium,hard}.json
    /// (field names below are the JSON keys). Sizes come from the classic minesweeper chart, wide side along x.
    /// A field picks one of its sizes at random every time it is generated.
    /// </summary>
    [Serializable]
    public class DifficultySettings
    {
        /// <summary>Smallest allowed field side, in cells. Anything smaller leaves no room to walk.</summary>
        public const int MinFieldSide = 5;

        public string name = "Medium";
        /// <summary>Allowed sizes; one is picked per field.</summary>
        public FieldSize[] sizes = { new FieldSize { width = 16, height = 16 } };
        public int mines = 40;
        // ---- base economy (before the multipliers below are applied)
        /// <summary>Detector battery length in seconds.</summary>
        public float batterySeconds = 120f;
        /// <summary>Pay per located mine.</summary>
        public int payPerFlag = 10;
        /// <summary>Money taken per wrong flag.</summary>
        public int wrongFlagPenalty = 10;
        /// <summary>Bonus when the day's quota is met.</summary>
        public int quotaBonus = 25;
        /// <summary>Cost of treating an injury.</summary>
        public int medicineCost = 15;
        /// <summary>Flags added to the quota for every day after the first.</summary>
        public int quotaPerDay = 1;

        // ---- multipliers
        /// <summary>Radar: the detector battery length is multiplied by this.</summary>
        public float radarBattery = 1f;
        /// <summary>Money received (pay per flag, quota bonus) is multiplied by this.</summary>
        public float moneyEarn = 1f;
        /// <summary>Money spent (wrong flag penalty, medicine) is multiplied by this.</summary>
        public float moneySpend = 1f;
        /// <summary>Encounter frequency multiplier. Unused until the encounter milestone (M5).</summary>
        public float encounterRate = 1f;
        /// <summary>Flags needed on day 1 (grows by the day, see DayManager).</summary>
        public int quotaBase = 12;
        /// <summary>The daily quota never exceeds this share of the mines.</summary>
        public float quotaMaxShare = 0.8f;

        /// <summary>A base amount the player receives, scaled by <see cref="moneyEarn"/>.</summary>
        public int Earned(int amount) { return Mathf.RoundToInt(amount * moneyEarn); }

        /// <summary>A base amount the player pays (fines, medicine), scaled by <see cref="moneySpend"/>.</summary>
        public int Spent(int amount) { return Mathf.RoundToInt(amount * moneySpend); }

        public FieldSize PickSize(System.Random rng)
        {
            return sizes[rng.Next(sizes.Length)];
        }

        public bool IsValid(out string problem)
        {
            problem = null;
            if (sizes == null || sizes.Length == 0) problem = "no sizes";
            else if (mines <= 0) problem = "mines must be above 0";
            else if (batterySeconds <= 0f) problem = "batterySeconds must be above 0";
            else
            {
                foreach (FieldSize s in sizes)
                {
                    if (s == null || s.width < MinFieldSide || s.height < MinFieldSide) { problem = "every size needs width and height of at least " + MinFieldSide; break; }
                }
            }
            return problem == null;
        }
    }

    public static class DifficultyTable
    {
        public const string ResourceFolder = "Difficulties/";

        private static readonly Dictionary<Difficulty, DifficultySettings> cache = new Dictionary<Difficulty, DifficultySettings>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache.Clear();
        }

        public static DifficultySettings Get(Difficulty difficulty)
        {
            if (cache.TryGetValue(difficulty, out DifficultySettings cached)) return cached;
            DifficultySettings loaded = Load(difficulty);
            cache[difficulty] = loaded;
            return loaded;
        }

        private static DifficultySettings Load(Difficulty difficulty)
        {
            string file = ResourceFolder + difficulty.ToString().ToLowerInvariant();
            TextAsset asset = Resources.Load<TextAsset>(file);
            if (asset == null)
            {
                Debug.LogError("[Difficulty] Missing settings file Assets/Resources/" + file + ".json, using built-in defaults.");
                return new DifficultySettings { name = difficulty.ToString() };
            }

            DifficultySettings settings;
            try
            {
                settings = JsonUtility.FromJson<DifficultySettings>(asset.text);
            }
            catch (ArgumentException e)
            {
                Debug.LogError("[Difficulty] " + file + ".json is not valid JSON: " + e.Message + " Using built-in defaults.");
                return new DifficultySettings { name = difficulty.ToString() };
            }

            string problem = "empty file";
            if (settings == null || !settings.IsValid(out problem))
            {
                Debug.LogError("[Difficulty] " + file + ".json is invalid (" + problem + "). Using built-in defaults.");
                return new DifficultySettings { name = difficulty.ToString() };
            }
            return settings;
        }
    }
}
