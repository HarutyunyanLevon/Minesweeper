using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Scenery around the minefield plot: a broken post-and-rail fence along the outer edge of the dirt border,
    /// the farmhouse and the tractor-tank. Rebuilt whenever a new field is generated.
    /// </summary>
    public class FieldDecor : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField] private MineField mines;
        [SerializeField, HideInInspector] private Sprite fenceX;
        [SerializeField, HideInInspector] private Sprite fenceXBroken;
        [SerializeField, HideInInspector] private Sprite fenceY;
        [SerializeField, HideInInspector] private Sprite fenceYBroken;
        [SerializeField, HideInInspector] private Sprite farmhouse;
        [SerializeField, HideInInspector] private Sprite tank;
        [SerializeField, HideInInspector] private Sprite[] scatterTrees;
        [SerializeField, HideInInspector] private Sprite[] scatterBushes;
        [SerializeField, HideInInspector] private Sprite[] scatterTufts;
        [SerializeField, HideInInspector] private Sprite[] scatterRubble;
        [SerializeField] private int scatterCount = 46;
        [SerializeField] private int seed = 7;
        [SerializeField, Range(0f, 1f)] private float brokenChance = 0.22f;

        // Pixel height between the fence sprite's bottom edge and the ground at its middle.
        private const float FenceBaseOffsetPx = 6f;

        public void Configure(FieldBuilder fieldBuilder, MineField mineField, Sprite fx, Sprite fxb, Sprite fy, Sprite fyb, Sprite house, Sprite tankSprite)
        {
            field = fieldBuilder;
            mines = mineField;
            fenceX = fx; fenceXBroken = fxb; fenceY = fy; fenceYBroken = fyb;
            farmhouse = house; tank = tankSprite;
        }

        public void ConfigureScatter(Sprite[] trees, Sprite[] bushes, Sprite[] tufts, Sprite[] rubble)
        {
            scatterTrees = trees; scatterBushes = bushes; scatterTufts = tufts; scatterRubble = rubble;
        }

        private void OnEnable()
        {
            if (mines != null) mines.Regenerated += Build;
        }

        private void OnDisable()
        {
            if (mines != null) mines.Regenerated -= Build;
        }

        /// <summary>World position of a point given in cell-edge coordinates (u along cell x, v along cell y).</summary>
        private static Vector3 P(float u, float v)
        {
            return new Vector3((u - v) * 0.5f, (u + v) * 0.25f, 0f);
        }

        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            int w = field.Width, h = field.Height;
            var rng = new System.Random(seed);
            int gap = w / 2;

            // Outer fence line: one cell outside the dirt border ring.
            for (int i = -1; i <= w; i++)
            {
                if (i >= gap - 1 && i <= gap + 1) continue; // entrance on the near side
                Segment(true, i + 0.5f, -1f, rng);
            }
            for (int i = -1; i <= w; i++) Segment(true, i + 0.5f, h + 1f, rng);
            for (int j = -1; j <= h; j++) Segment(false, -1f, j + 0.5f, rng);
            for (int j = -1; j <= h; j++) Segment(false, w + 1f, j + 0.5f, rng);

            Prop("Farmhouse", farmhouse, P(4f, h + 7f));
            Prop("Tank", tank, P(w + 6f, h - 2f));

            Scatter(w, h, new System.Random(seed + 1));
        }

        /// <summary>Trees, bushes, grass tufts and rubble in the meadow around the plot (never inside the fence).</summary>
        private void Scatter(int w, int h, System.Random rng)
        {
            int placed = 0, tries = 0;
            while (placed < scatterCount && tries < scatterCount * 40)
            {
                tries++;
                float u = -8f + (float)rng.NextDouble() * (w + 15f);
                float v = -8f + (float)rng.NextDouble() * (h + 15f);
                // keep the plot, the fence ring and the entrance lane clear
                if (u > -3f && u < w + 3f && v > -3f && v < h + 3f) continue;
                if (v < 0f && Mathf.Abs(u - w / 2f) < 3.5f) continue;
                // keep clear of the big props
                if (Near(u, v, 4f, h + 7f, 5f) || Near(u, v, w + 6f, h - 2f, 5f)) continue;

                double roll = rng.NextDouble();
                Sprite[] set = roll < 0.45 ? scatterTufts : roll < 0.70 ? scatterBushes : roll < 0.82 ? scatterTrees : scatterRubble;
                if (set == null || set.Length == 0) continue;
                Sprite s = set[rng.Next(set.Length)];
                Prop("Scatter", s, P(u, v) - new Vector3(0f, 2f / PixelConstants.PixelsPerUnit, 0f));
                placed++;
            }
        }

        private static bool Near(float u, float v, float cu, float cv, float radius)
        {
            return (u - cu) * (u - cu) + (v - cv) * (v - cv) < radius * radius;
        }

        private void Segment(bool alongU, float u, float v, System.Random rng)
        {
            bool broken = rng.NextDouble() < brokenChance;
            Sprite s = alongU ? (broken ? fenceXBroken : fenceX) : (broken ? fenceYBroken : fenceY);
            if (s == null) return;
            Vector3 pos = P(u, v) - new Vector3(0f, FenceBaseOffsetPx / PixelConstants.PixelsPerUnit, 0f);
            Prop(alongU ? "FenceX" : "FenceY", s, pos);
        }

        private void Prop(string objectName, Sprite sprite, Vector3 position)
        {
            if (sprite == null) return;
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = IsoMath.SnapToPixel(position);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = PixelConstants.SortingOrderDefault;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
        }
    }
}
