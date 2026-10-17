using UnityEngine;

namespace MinefieldClerk.Ambience
{
    /// <summary>Procedural war damage on the ground: scorch marks, mud patches and old shell craters (flat 2:1 ellipses).</summary>
    public static class GroundDecals
    {
        public enum Kind { Scorch, Mud, Crater }

        // Wobbly outline: this many radius samples around the ellipse, each between WobbleMin and WobbleMin + WobbleRange.
        private const int WobblePoints = 8;
        private const float WobbleMin = 0.78f;
        private const float WobbleRange = 0.28f;

        // Scruffy rim: beyond RimStart (fraction of the radius) pixels are skipped with RimSkipChance.
        private const float RimStart = 0.78f;
        private const double RimSkipChance = 0.4;

        private const double MudShineChance = 0.06;

        // Colour bands: (fraction of radius below which the colour applies, colour).
        private const float ScorchCore = 0.5f, ScorchMid = 0.8f;
        private const float MudCore = 0.7f;
        private const float CraterCore = 0.45f, CraterMid = 0.72f;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 ScorchA = new Color32(22, 16, 11, 205);
        private static readonly Color32 ScorchB = new Color32(40, 29, 18, 150);
        private static readonly Color32 ScorchC = new Color32(58, 42, 25, 90);
        private static readonly Color32 MudA = new Color32(54, 38, 22, 210);
        private static readonly Color32 MudB = new Color32(74, 54, 30, 140);
        private static readonly Color32 MudShine = new Color32(104, 84, 52, 220);
        private static readonly Color32 CraterA = new Color32(26, 18, 12, 235);
        private static readonly Color32 CraterB = new Color32(48, 34, 20, 215);
        private static readonly Color32 CraterC = new Color32(112, 86, 50, 190);

        public static Sprite Make(System.Random rng, Kind kind, int width)
        {
            int w = width, h = width / 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            var wob = new float[WobblePoints];
            for (int i = 0; i < WobblePoints; i++) wob[i] = WobbleMin + (float)rng.NextDouble() * WobbleRange;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + Num.Half - w * Num.Half) / (w * Num.Half);
                    float ny = (y + Num.Half - h * Num.Half) / (h * Num.Half);
                    float angle = Mathf.Atan2(ny, nx) + Mathf.PI;
                    float f = angle / Num.Tau * WobblePoints;
                    int i0 = Mathf.FloorToInt(f) % WobblePoints, i1 = (i0 + 1) % WobblePoints;
                    float edge = Mathf.Lerp(wob[i0], wob[i1], f - Mathf.Floor(f));
                    float t = Mathf.Sqrt(nx * nx + ny * ny) / edge;
                    if (t >= 1f) continue;
                    if (t > RimStart && rng.NextDouble() < RimSkipChance) continue;

                    Color32 col;
                    switch (kind)
                    {
                        case Kind.Scorch:
                            col = t < ScorchCore ? ScorchA : t < ScorchMid ? ScorchB : ScorchC;
                            break;
                        case Kind.Mud:
                            col = t < MudCore ? MudA : MudB;
                            if (rng.NextDouble() < MudShineChance) col = MudShine;
                            break;
                        default:
                            col = t < CraterCore ? CraterA : t < CraterMid ? CraterB : CraterC;
                            break;
                    }
                    px[y * w + x] = col;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(Num.Half, Num.Half), PixelConstants.PixelsPerUnit);
        }
    }
}
