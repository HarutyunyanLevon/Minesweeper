using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>Shared beat of a burning fuse so the blinking mine and the beeps stay in step: slow at first, faster near the end.</summary>
    public static class FuseTiming
    {
        public static float BeepInterval(float progress01)
        {
            return Mathf.Lerp(0.5f, 0.12f, Mathf.Clamp01(progress01));
        }
    }
}
