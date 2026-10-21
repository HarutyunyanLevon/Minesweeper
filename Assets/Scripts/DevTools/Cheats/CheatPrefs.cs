using UnityEngine;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>Remembers cheat settings (window layout, hotkeys, sticky cheats) between play sessions in PlayerPrefs.</summary>
    public static class CheatPrefs
    {
        private const string Prefix = "cheat.";

        public static bool Has(string key) { return PlayerPrefs.HasKey(Prefix + key); }
        public static void Delete(string key) { PlayerPrefs.DeleteKey(Prefix + key); }

        public static int GetInt(string key, int fallback) { return PlayerPrefs.GetInt(Prefix + key, fallback); }
        public static void SetInt(string key, int value) { PlayerPrefs.SetInt(Prefix + key, value); }

        public static float GetFloat(string key, float fallback) { return PlayerPrefs.GetFloat(Prefix + key, fallback); }
        public static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(Prefix + key, value); }

        public static bool GetBool(string key, bool fallback) { return PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0; }
        public static void SetBool(string key, bool value) { PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0); }
    }
}
