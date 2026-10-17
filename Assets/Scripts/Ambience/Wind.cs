using UnityEngine;
using MinefieldClerk.Field;

namespace MinefieldClerk.Ambience
{
    /// <summary>
    /// Calm wind. A light breeze always blows, and every so often a gust front sweeps across the scene
    /// from left to right. Flags, grass, wheat and the autumn leaves ask it how hard the wind blows at their position.
    /// Spawns itself (with the leaves and the scene mood) in any scene that has a FieldSession.
    /// </summary>
    public class Wind : MonoBehaviour
    {
        // Breeze: base + amplitude * sin(time * speed + position * spatialFrequency)
        private const float BreezeBase = 0.10f;
        private const float BreezeAmplitude = 0.06f;
        private const float BreezeSpeed = 0.6f;
        private const float BreezeSpatialFrequency = 0.35f;

        private const float DirectionX = 0.94f;
        private const float DirectionY = -0.34f;
        private const string AmbienceObjectName = "Ambience";

        public static Wind Instance { get; private set; }

        /// <summary>Direction the wind blows on screen: mostly right, slightly down.</summary>
        public static readonly Vector2 Direction = new Vector2(DirectionX, DirectionY).normalized;

        [SerializeField] private float gustSpeed = 5f;
        [SerializeField] private float gustWidth = 5f;
        [SerializeField] private Vector2 secondsBetweenGusts = new Vector2(8f, 15f);
        [Tooltip("Seconds before the first gust")]
        [SerializeField] private float firstGustDelay = 3f;
        [Tooltip("How far (world units) ahead of and behind the camera a gust starts and ends")]
        [SerializeField] private float gustTravelMargin = 12f;
        [Tooltip("Extra widths of travel after the camera before the gust ends")]
        [SerializeField] private float gustTailWidths = 2f;

        private float front;
        private float end;
        private bool active;
        private float nextGust;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<FieldSession>() == null || Object.FindAnyObjectByType<Wind>() != null) return;
            var go = new GameObject(AmbienceObjectName);
            go.AddComponent<Wind>();
            go.AddComponent<AutumnLeaves>();
            go.AddComponent<SceneMood>();
        }

        private void Awake()
        {
            Instance = this;
            nextGust = firstGustDelay;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static float Along(Vector3 p)
        {
            return p.x * Direction.x + p.y * Direction.y;
        }

        private void Update()
        {
            if (!active)
            {
                nextGust -= Time.deltaTime;
                if (nextGust <= 0f) StartGust();
            }
            else
            {
                front += gustSpeed * Time.deltaTime;
                if (front > end)
                {
                    active = false;
                    nextGust = Random.Range(secondsBetweenGusts.x, secondsBetweenGusts.y);
                }
            }
        }

        private void StartGust()
        {
            Camera cam = Camera.main;
            float a = cam != null ? Along(cam.transform.position) : 0f;
            front = a - gustTravelMargin - gustWidth;
            end = a + gustTravelMargin + gustWidth * gustTailWidths;
            active = true;
        }

        /// <summary>The always-present light breeze at a position, 0.04 to 0.16.</summary>
        public static float Breeze(Vector3 pos)
        {
            return BreezeBase + BreezeAmplitude * Mathf.Sin(Time.time * BreezeSpeed + Along(pos) * BreezeSpatialFrequency);
        }

        /// <summary>How strongly the gust front is passing over a position, 0 to 1.</summary>
        public static float Gust(Vector3 pos)
        {
            if (Instance == null || !Instance.active) return 0f;
            float d = (Along(pos) - Instance.front) / Instance.gustWidth;
            return Mathf.Exp(-d * d);
        }
    }
}
