using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>Placeholder sound effects synthesised at runtime. Replaced by real recordings later.</summary>
    public static class ProceduralSfx
    {
        private const int Rate = 44100;
        private const int Mono = 1;
        private const float NoiseScale = 2f;   // maps NextDouble() 0..1 to -1..1 (with the -1 offset)

        // Beep: short radar-style sine with a fast decay.
        private const float BeepSeconds = 0.07f, BeepDecay = 45f, BeepAttack = 400f, BeepHz = 1500f, BeepGain = 0.6f;

        // Clack: noise burst plus a low sine.
        private const int ClackSeed = 11;
        private const float ClackSeconds = 0.09f, ClackNoiseDecay = 90f, ClackNoiseGain = 0.5f;
        private const float ClackHz = 230f, ClackToneDecay = 45f, ClackToneGain = 0.5f, ClackGain = 0.9f;

        // Boom: low-passed noise with a pitch-dropping sub thump.
        private const int BoomSeed = 5;
        private const float BoomSeconds = 1.5f;
        private const float BoomFilterStart = 0.55f, BoomFilterEnd = 0.015f, BoomFilterSeconds = 0.9f;
        private const float BoomDecay = 3.4f, BoomAttack = 300f, BoomNoiseGain = 2.2f;
        private const float BoomThumpBaseHz = 48f, BoomThumpSweepHz = 40f, BoomThumpSweepDecay = 9f, BoomThumpDecay = 4.5f, BoomThumpGain = 0.8f;
        private const float BoomGain = 0.9f;

        // Dud: falling tone with a little hiss.
        private const int DudSeed = 3;
        private const float DudSeconds = 0.5f, DudStartHz = 520f, DudEndHz = 90f, DudSweepSeconds = 0.45f;
        private const float DudToneGain = 0.55f, DudNoiseGain = 0.18f, DudNoiseDecay = 10f, DudDecay = 5f;

        private static AudioClip Make(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, Mono, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Noise(System.Random rnd)
        {
            return (float)(rnd.NextDouble() * NoiseScale - 1.0);
        }

        /// <summary>Short radar-style beep of a burning fuse.</summary>
        public static AudioClip Beep()
        {
            float[] d = new float[(int)(Rate * BeepSeconds)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * BeepDecay) * Mathf.Clamp01(t * BeepAttack);
                d[i] = Mathf.Sin(Num.Tau * BeepHz * t) * env * BeepGain;
            }
            return Make("MineBeep", d);
        }

        /// <summary>Metal click when the probe finds a mine.</summary>
        public static AudioClip Clack()
        {
            var rnd = new System.Random(ClackSeed);
            float[] d = new float[(int)(Rate * ClackSeconds)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = Noise(rnd) * Mathf.Exp(-t * ClackNoiseDecay) * ClackNoiseGain;
                float tone = Mathf.Sin(Num.Tau * ClackHz * t) * Mathf.Exp(-t * ClackToneDecay) * ClackToneGain;
                d[i] = (noise + tone) * ClackGain;
            }
            return Make("MineClack", d);
        }

        /// <summary>Low explosion: filtered noise with a sub thump.</summary>
        public static AudioClip Boom()
        {
            var rnd = new System.Random(BoomSeed);
            float[] d = new float[(int)(Rate * BoomSeconds)];
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float k = Mathf.Lerp(BoomFilterStart, BoomFilterEnd, Mathf.Clamp01(t / BoomFilterSeconds));
                lp += (Noise(rnd) - lp) * k;
                float env = Mathf.Exp(-t * BoomDecay) * Mathf.Clamp01(t * BoomAttack);
                float thumpHz = BoomThumpBaseHz + BoomThumpSweepHz * Mathf.Exp(-t * BoomThumpSweepDecay);
                float thump = Mathf.Sin(Num.Tau * thumpHz * t) * Mathf.Exp(-t * BoomThumpDecay);
                d[i] = Mathf.Clamp(lp * BoomNoiseGain * env + thump * BoomThumpGain, -1f, 1f) * BoomGain;
            }
            return Make("MineBoom", d);
        }

        /// <summary>Descending fizzle: the mine was a dud.</summary>
        public static AudioClip Dud()
        {
            var rnd = new System.Random(DudSeed);
            float[] d = new float[(int)(Rate * DudSeconds)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(DudStartHz, DudEndHz, Mathf.Clamp01(t / DudSweepSeconds));
                phase += Num.Tau * f / Rate;
                float hiss = Noise(rnd) * DudNoiseGain * Mathf.Exp(-t * DudNoiseDecay);
                d[i] = (Mathf.Sin(phase) * DudToneGain + hiss) * Mathf.Exp(-t * DudDecay);
            }
            return Make("MineDud", d);
        }
    }
}
