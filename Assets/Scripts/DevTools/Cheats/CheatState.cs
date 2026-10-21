using UnityEngine;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// The switches the game code itself listens to. The cheat windows write the raw fields; the game reads the
    /// *Active / *Factor members, which fall back to normal play whenever dev mode (F10) is off.
    /// </summary>
    public static class CheatState
    {
        public const float NormalFactor = 1f;

        // ---- raw settings, written by the cheat windows
        public static bool GodMode;
        public static bool FreezeBattery;
        public static bool UnlimitedKits;
        public static float MoveSpeed = NormalFactor;
        public static float WorkSpeed = NormalFactor;
        public static float GameSpeed = NormalFactor;
        /// <summary>True once the game speed was changed, so the cheat system owns Time.timeScale from then on.</summary>
        public static bool GameSpeedTouched;

        // ---- what the game reads
        public static bool GodActive => DevMode.Enabled && GodMode;
        public static bool BatteryFrozenActive => DevMode.Enabled && FreezeBattery;
        public static bool UnlimitedKitsActive => DevMode.Enabled && UnlimitedKits;
        public static float MoveSpeedFactor => DevMode.Enabled ? MoveSpeed : NormalFactor;
        public static float WorkSpeedFactor => DevMode.Enabled ? WorkSpeed : NormalFactor;

        public static void SetGameSpeed(float value)
        {
            GameSpeed = value;
            GameSpeedTouched = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            GodMode = false;
            FreezeBattery = false;
            UnlimitedKits = false;
            MoveSpeed = NormalFactor;
            WorkSpeed = NormalFactor;
            GameSpeed = NormalFactor;
            GameSpeedTouched = false;
        }
    }
}
