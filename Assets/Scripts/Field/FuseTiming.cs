using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>Shared beat of a burning fuse so the blinking mine and the beeps stay in step: slow at first, faster near the end.</summary>
    public static class FuseTiming
    {
        public const float SlowInterval = 0.5f;   // seconds between beeps when the fuse was just lit
        public const float FastInterval = 0.12f;  // seconds between beeps right before the blast

        public static float BeepInterval(float progress01)
        {
            return Mathf.Lerp(SlowInterval, FastInterval, Mathf.Clamp01(progress01));
        }
    }
}
