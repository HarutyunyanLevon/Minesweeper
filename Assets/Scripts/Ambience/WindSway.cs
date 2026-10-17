using UnityEngine;

namespace MinefieldClerk.Ambience
{
    /// <summary>
    /// Makes a prop move with the wind. Foliage (wheat, grass, bushes) leans and shivers a little all the time and
    /// bends more when a gust passes. A flag hangs quiet until a gust reaches it, then it lifts and flutters.
    /// </summary>
    public class WindSway : MonoBehaviour
    {
        public enum Kind { Foliage, Flag }

        // Flag: lift toward the wind, slow idle swing, fast gust ripple, scale flutter (all in degrees / scale fractions).
        private const float FlagGustLiftDegrees = 11f;
        private const float FlagCalmSwingDegrees = 1.2f;
        private const float FlagCalmSwingSpeed = 1.7f;
        private const float FlagGustRippleDegrees = 3f;
        private const float FlagGustRippleSpeed = 9f;
        private const float FlagFlutterScale = 0.18f;
        private const float FlagFlutterSpeed = 13f;
        private const float FlagGustNarrowing = 0.06f;

        // Foliage: lean from calm + gust, shiver from calm + gust.
        private const float FoliageCalmLeanDegrees = 2.4f;
        private const float FoliageGustLeanDegrees = 6f;
        private const float FoliageCalmShiverDegrees = 1f;
        private const float FoliageCalmShiverSpeed = 1.9f;
        private const float FoliageGustShiverDegrees = 2.2f;
        private const float FoliageGustShiverSpeed = 5.5f;

        [SerializeField] private Kind kind;
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float phase;
        [SerializeField] private Vector3 baseScale;

        public static WindSway Add(GameObject go, Kind kind, float sensitivity = 1f)
        {
            WindSway s = go.AddComponent<WindSway>();
            s.kind = kind;
            s.sensitivity = sensitivity;
            s.phase = Random.value * Num.Tau;
            s.baseScale = go.transform.localScale;
            return s;
        }

        private void Update()
        {
            Vector3 p = transform.position;
            float calm = Wind.Breeze(p);
            float gust = Wind.Gust(p);
            float t = Time.time + phase;
            float angle;
            float scaleX = 1f;

            if (kind == Kind.Flag)
            {
                // quiet: hangs almost still. gust: lifts toward the wind and ripples quickly.
                angle = -(gust * FlagGustLiftDegrees)
                        + calm * FlagCalmSwingDegrees * Mathf.Sin(t * FlagCalmSwingSpeed)
                        + gust * FlagGustRippleDegrees * Mathf.Sin(t * FlagGustRippleSpeed);
                scaleX = 1f + gust * FlagFlutterScale * Mathf.Sin(t * FlagFlutterSpeed) - gust * FlagGustNarrowing;
            }
            else
            {
                angle = -(calm * FoliageCalmLeanDegrees + gust * FoliageGustLeanDegrees) * sensitivity
                        + (calm * FoliageCalmShiverDegrees * Mathf.Sin(t * FoliageCalmShiverSpeed)
                           + gust * FoliageGustShiverDegrees * Mathf.Sin(t * FoliageGustShiverSpeed)) * sensitivity;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y, baseScale.z);
        }
    }
}
