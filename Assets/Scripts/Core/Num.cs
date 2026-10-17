using UnityEngine;

namespace MinefieldClerk
{
    /// <summary>Shared numeric constants so code never needs a bare literal for them.</summary>
    public static class Num
    {
        public const float Half = 0.5f;
        public const float Quarter = 0.25f;
        public const float Tau = Mathf.PI * 2f;
        public const int RightAngleDegrees = 90;
        public const int Octants = 8;
        public const float OctantRadians = Mathf.PI / 4f;
        public const float Epsilon = 1e-4f;
    }
}
