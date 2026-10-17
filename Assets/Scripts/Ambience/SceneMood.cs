using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MinefieldClerk.Ambience
{
    /// <summary>
    /// Keeps the scene calm and warm: the camera background matches the grass (so no black ever shows past the ground),
    /// and the global light slowly swells and cools over a minute or two, like thin clouds passing the evening sun.
    /// </summary>
    public class SceneMood : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new Color(0.26f, 0.21f, 0.10f);
        [Tooltip("Radians per second of the slow light swell")]
        [SerializeField] private float swellSpeed = 0.07f;
        [Tooltip("Intensity multiplier at the dimmest point")]
        [SerializeField] private float minIntensity = 0.94f;
        [Tooltip("Extra intensity multiplier added at the brightest point")]
        [SerializeField] private float intensitySwing = 0.08f;
        [Tooltip("Colour multiplier the light drifts toward at the warmest point")]
        [SerializeField] private Color warmTint = new Color(1.05f, 0.96f, 0.86f, 1f);

        private Light2D globalLight;
        private float baseIntensity;
        private Color baseColor;

        private void Start()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundColor;
            }

            foreach (Light2D l in FindObjectsByType<Light2D>())
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    globalLight = l;
                    baseIntensity = l.intensity;
                    baseColor = l.color;
                    break;
                }
            }
        }

        private void Update()
        {
            if (globalLight == null) return;
            float slow = Mathf.Sin(Time.time * swellSpeed) * Num.Half + Num.Half;
            globalLight.intensity = baseIntensity * (minIntensity + intensitySwing * slow);
            globalLight.color = Color.Lerp(baseColor, baseColor * warmTint, slow);
        }
    }
}
