using System;
using UnityEngine;

namespace MinefieldClerk.Ambience
{
    /// <summary>
    /// Slow autumn leaves and a few specks of dust drifting through the light. They follow the camera and are pushed by
    /// the wind: a gust sweeping across the scene carries the leaves it passes faster and brings a short flurry of new ones.
    /// Particles are emitted and steered from code (no velocity or force curves), so every leaf reads the wind at its own position.
    /// </summary>
    public class AutumnLeaves : MonoBehaviour
    {
        /// <summary>Everything that describes one kind of drifting particle.</summary>
        [Serializable]
        public class Layer
        {
            public int maxParticles = 400;
            public int sortingOrder = 320;
            [Tooltip("Particles emitted at start so the scene is never empty")]
            public int primeCount = 45;
            [Tooltip("Particles per second in calm air")]
            public float emitRate = 2f;
            [Tooltip("Extra particles per second during a gust")]
            public float gustEmitRate = 16f;
            [Tooltip("Spawn box around the camera: min and max offset")]
            public Vector2 spawnX = new Vector2(-15f, 9f);
            public Vector2 spawnY = new Vector2(-3f, 9f);
            public Vector2 initialVelocity = new Vector2(0.5f, -0.4f);
            public Vector2 lifetime = new Vector2(9f, 14f);
            public Vector2 size = new Vector2(0.09f, 0.15f);
            [Tooltip("Degrees")]
            public Vector2 startRotation = new Vector2(0f, 360f);
            [Tooltip("Degrees per second")]
            public Vector2 spin = new Vector2(-120f, 120f);
            [Tooltip("Multiplier on all wind effects")]
            public float windStrength = 1f;
        }

        // Fade in/out so particles never pop: alpha is 0 at birth, full by FadeIn, full until FadeOutStart, 0 at death.
        private const float FadeIn = 0.12f;
        private const float FadeOutStart = 0.8f;

        // Wind steering: drift = (DriftX + breeze + gust * GustPush) and (DriftY - gust * GustSink); plus flutter.
        private const float DriftX = 0.45f;
        private const float DriftY = -0.38f;
        private const float GustPush = 3.2f;
        private const float GustSink = 0.5f;
        private const float FlutterXAmplitude = 0.18f;
        private const float FlutterXSpeed = 1.6f;
        private const float FlutterYAmplitude = 0.14f;
        private const float FlutterYSpeed = 1.1f;
        private const float FlutterYSeedScale = 1.7f;
        private const float SeedScale = 0.0001f;

        // The 4x3 leaf texture.
        private const int LeafTexW = 4, LeafTexH = 3;
        private const string ParticleShader = "Sprites/Default";
        private const int BufferSize = 512;

        private static readonly Color[] LeafColors =
        {
            new Color(0.78f, 0.34f, 0.10f), new Color(0.88f, 0.62f, 0.18f),
            new Color(0.60f, 0.26f, 0.10f), new Color(0.72f, 0.52f, 0.20f),
        };

        private static readonly Color32 Opaque = new Color32(255, 255, 255, 255);
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color DustBase = new Color(1f, 0.86f, 0.55f);

        [SerializeField] private Layer leafSettings = new Layer();
        [SerializeField] private Layer dustSettings = new Layer
        {
            maxParticles = 120,
            sortingOrder = 90,
            primeCount = 25,
            emitRate = 3f,
            gustEmitRate = 0f,
            spawnX = new Vector2(-12f, 12f),
            spawnY = new Vector2(-7f, 7f),
            initialVelocity = new Vector2(0.12f, 0.01f),
            lifetime = new Vector2(10f, 16f),
            size = new Vector2(0.035f, 0.06f),
            startRotation = Vector2.zero,
            spin = Vector2.zero,
            windStrength = 0.25f,
        };
        [Tooltip("Dust alpha range")]
        [SerializeField] private Vector2 dustAlpha = new Vector2(0.18f, 0.34f);

        private ParticleSystem leaves;
        private ParticleSystem dust;
        private Transform cam;
        private Material material;
        private readonly ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[BufferSize];
        private float leafDebt;
        private float dustDebt;
        private bool primed;

        private void Awake()
        {
            var tex = new Texture2D(LeafTexW, LeafTexH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color32 o = Opaque, c = Transparent;
            tex.SetPixels32(new[]
            {
                c, o, o, c,
                o, o, o, o,
                c, o, o, c,
            });
            tex.Apply();
            material = new Material(Shader.Find(ParticleShader)) { mainTexture = tex };

            leaves = Make("Leaves", leafSettings);
            dust = Make("Dust", dustSettings);
        }

        private ParticleSystem Make(string objectName, Layer layer)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.maxParticles = layer.maxParticles;

            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, FadeIn), new GradientAlphaKey(1f, FadeOutStart), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(g);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = layer.sortingOrder;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            ps.Play();
            return ps;
        }

        private static float Between(Vector2 range)
        {
            return UnityEngine.Random.Range(range.x, range.y);
        }

        private void Emit(ParticleSystem system, Layer layer, Color color, Vector3 camPos)
        {
            var e = new ParticleSystem.EmitParams
            {
                position = new Vector3(camPos.x + Between(layer.spawnX), camPos.y + Between(layer.spawnY), 0f),
                velocity = new Vector3(layer.initialVelocity.x, layer.initialVelocity.y, 0f),
                startLifetime = Between(layer.lifetime),
                startSize = Between(layer.size),
                startColor = color,
                rotation = Between(layer.startRotation),
                angularVelocity = Between(layer.spin),
            };
            system.Emit(e, 1);
        }

        private void EmitLeaf(Vector3 camPos)
        {
            Emit(leaves, leafSettings, LeafColors[UnityEngine.Random.Range(0, LeafColors.Length)], camPos);
        }

        private void EmitDust(Vector3 camPos)
        {
            Color c = DustBase;
            c.a = Between(dustAlpha);
            Emit(dust, dustSettings, c, camPos);
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                Camera c = Camera.main;
                if (c == null) return;
                cam = c.transform;
            }

            Vector3 p = cam.position;
            float dt = Time.deltaTime;

            if (!primed)
            {
                primed = true;
                for (int i = 0; i < leafSettings.primeCount; i++) EmitLeaf(p);
                for (int i = 0; i < dustSettings.primeCount; i++) EmitDust(p);
            }

            float gust = Wind.Gust(p);
            leafDebt += (leafSettings.emitRate + gust * leafSettings.gustEmitRate) * dt;
            while (leafDebt >= 1f) { leafDebt -= 1f; EmitLeaf(p); }
            dustDebt += (dustSettings.emitRate + gust * dustSettings.gustEmitRate) * dt;
            while (dustDebt >= 1f) { dustDebt -= 1f; EmitDust(p); }

            Steer(leaves, leafSettings.windStrength);
            Steer(dust, dustSettings.windStrength);
        }

        /// <summary>Each particle drifts with the breeze, is shoved by a passing gust, and flutters a little.</summary>
        private void Steer(ParticleSystem ps, float strength)
        {
            int n = ps.GetParticles(buffer);
            float t = Time.time;
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = buffer[i].position;
                float g = Wind.Gust(pos);
                float seed = buffer[i].randomSeed * SeedScale;
                float flutter = Mathf.Sin(t * FlutterXSpeed + seed) * FlutterXAmplitude * strength;
                float vx = (DriftX + Wind.Breeze(pos) + g * GustPush) * strength + flutter;
                float vy = (DriftY - g * GustSink) * strength + Mathf.Cos(t * FlutterYSpeed + seed * FlutterYSeedScale) * FlutterYAmplitude * strength;
                buffer[i].velocity = new Vector3(vx, vy, 0f);
            }
            ps.SetParticles(buffer, n);
        }
    }
}
