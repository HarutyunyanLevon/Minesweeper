using UnityEngine;

namespace MinefieldClerk.Ambience
{
    /// <summary>
    /// A flag drawn pixel by pixel. The pole and the mound never move; only the cloth waves, and the waving grows
    /// toward the free end, like a real flag: a quiet breeze barely stirs it and lets the tip droop, a gust lifts it
    /// and ripples it. The sprite is rebuilt only when a cloth column changes row, so it stays crisp pixel art.
    /// </summary>
    public class FlagCloth : MonoBehaviour
    {
        // 12 x 14 source, y = 0 is the top. P pole, R cloth light, D cloth dark, a/b/c mound (light to dark).
        private static readonly string[] Map =
        {
            "............",
            ".....PRRRRRR",
            ".....PRRRRR.",
            ".....PRRRR..",
            ".....PDDDD..",
            ".....PDDD...",
            ".....P......",
            ".....P......",
            ".....P......",
            ".....P......",
            "....aPaa....",
            "...aaabbb...",
            "..bbbbbbbb..",
            ".cccccccccc.",
        };

        private const int SrcW = 12, SrcH = 14;
        private const int TexW = 16, TexH = 18;
        private const int PadX = 2;
        private const int ClothX = 6, ClothCols = 6;

        // Wave: amplitude = CalmBase + calm * CalmGain + gust * GustGain; speed = SpeedBase + gust * SpeedGust.
        private const float AmpBase = 0.35f, AmpCalmGain = 0.6f, AmpGustGain = 1.7f;
        private const float SpeedBase = 2.2f, SpeedGustGain = 7f;
        private const float TipExponent = 1.3f;     // how strongly waving grows toward the free end
        private const float PhasePerColumn = 1.1f;
        private const float Droop = 0.9f;           // tip sags in still air
        private const float Lift = 1.2f;            // tip rises in a gust
        private const int HashBase = 7;
        private const int HashOffset = 3;           // keeps the (possibly negative) row shift positive in the hash
        private static readonly Vector2 Pivot = new Vector2(Num.Half, 0f);

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Pole = new Color32(0x46, 0x34, 0x24, 255);
        private static readonly Color32 Light = new Color32(0xcc, 0x30, 0x28, 255);
        private static readonly Color32 Dark = new Color32(0x96, 0x20, 0x1e, 255);
        private static readonly Color32 MoundA = new Color32(0x7a, 0x5c, 0x3a, 255);
        private static readonly Color32 MoundB = new Color32(0x5c, 0x42, 0x2a, 255);
        private static readonly Color32 MoundC = new Color32(0x3e, 0x2c, 0x1e, 255);

        private SpriteRenderer sr;
        private Texture2D tex;
        private Sprite sprite;
        private readonly Color32[] buf = new Color32[TexW * TexH];
        private readonly int[] dy = new int[ClothCols];
        private int key = int.MinValue;
        private float phase;

        public static FlagCloth Create(SpriteRenderer renderer)
        {
            FlagCloth f = renderer.gameObject.AddComponent<FlagCloth>();
            f.sr = renderer;
            f.phase = Random.value * Num.Tau;
            f.tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FlagCloth",
            };
            f.sprite = Sprite.Create(f.tex, new Rect(0, 0, TexW, TexH), Pivot, PixelConstants.PixelsPerUnit);
            renderer.sprite = f.sprite;
            f.Rebuild();
            return f;
        }

        private void Update()
        {
            Vector3 p = transform.position;
            float calm = Wind.Breeze(p);
            float gust = Wind.Gust(p);
            float t = Time.time + phase;
            float amp = AmpBase + calm * AmpCalmGain + gust * AmpGustGain;
            float speed = SpeedBase + gust * SpeedGustGain;

            int k = 0;
            for (int c = 0; c < ClothCols; c++)
            {
                float w = Mathf.Pow(c / (ClothCols - 1f), TipExponent);
                float wave = amp * Mathf.Sin(t * speed - c * PhasePerColumn) * w;
                float droop = (1f - gust) * Droop * w;
                float lift = gust * Lift * w;
                dy[c] = Mathf.RoundToInt(wave + lift - droop);
                k = k * HashBase + (dy[c] + HashOffset);
            }

            if (k != key)
            {
                key = k;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            for (int i = 0; i < buf.Length; i++) buf[i] = Clear;

            for (int y = 0; y < SrcH; y++)
            {
                string row = Map[y];
                for (int x = 0; x < SrcW; x++)
                {
                    char ch = row[x];
                    if (ch == '.') continue;

                    int shift = 0;
                    if ((ch == 'R' || ch == 'D') && x >= ClothX) shift = dy[Mathf.Min(x - ClothX, ClothCols - 1)];

                    int tx = x + PadX;
                    int ty = Mathf.Clamp((SrcH - 1 - y) + shift, 0, TexH - 1);
                    buf[ty * TexW + tx] = ColorOf(ch);
                }
            }

            tex.SetPixels32(buf);
            tex.Apply(false);
        }

        private static Color32 ColorOf(char ch)
        {
            switch (ch)
            {
                case 'P': return Pole;
                case 'R': return Light;
                case 'D': return Dark;
                case 'a': return MoundA;
                case 'b': return MoundB;
                default: return MoundC;
            }
        }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (tex != null) Destroy(tex);
        }
    }
}
