using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>Placeholder sound effects synthesised at runtime. Replaced by real recordings later.</summary>
    public static class ProceduralSfx
    {
        private const int Rate = 44100;

        private static AudioClip Make(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Short radar-style beep of a burning fuse.</summary>
        public static AudioClip Beep()
        {
            float[] d = new float[(int)(Rate * 0.07f)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 45f) * Mathf.Clamp01(t * 400f);
                d[i] = Mathf.Sin(2f * Mathf.PI * 1500f * t) * env * 0.6f;
            }
            return Make("MineBeep", d);
        }

        /// <summary>Metal click when the probe finds a mine.</summary>
        public static AudioClip Clack()
        {
            var rnd = new System.Random(11);
            float[] d = new float[(int)(Rate * 0.09f)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                d[i] = (noise * Mathf.Exp(-t * 90f) * 0.5f + Mathf.Sin(2f * Mathf.PI * 230f * t) * Mathf.Exp(-t * 45f) * 0.5f) * 0.9f;
            }
            return Make("MineClack", d);
        }

        /// <summary>Low explosion: filtered noise with a sub thump.</summary>
        public static AudioClip Boom()
        {
            var rnd = new System.Random(5);
            float len = 1.5f;
            float[] d = new float[(int)(Rate * len)];
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float k = Mathf.Lerp(0.55f, 0.015f, Mathf.Clamp01(t / 0.9f));
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * k;
                float env = Mathf.Exp(-t * 3.4f) * Mathf.Clamp01(t * 300f);
                float thump = Mathf.Sin(2f * Mathf.PI * (48f + 40f * Mathf.Exp(-t * 9f)) * t) * Mathf.Exp(-t * 4.5f);
                d[i] = Mathf.Clamp((lp * 2.2f * env) + thump * 0.8f, -1f, 1f) * 0.9f;
            }
            return Make("MineBoom", d);
        }

        /// <summary>Descending fizzle: the mine was a dud.</summary>
        public static AudioClip Dud()
        {
            var rnd = new System.Random(3);
            float[] d = new float[(int)(Rate * 0.5f)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(520f, 90f, Mathf.Clamp01(t / 0.45f));
                phase += 2f * Mathf.PI * f / Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                d[i] = (Mathf.Sin(phase) * 0.55f + noise * 0.18f * Mathf.Exp(-t * 10f)) * Mathf.Exp(-t * 5f);
            }
            return Make("MineDud", d);
        }
    }
}
