using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Draws the mine field state: dug craters on probed tiles, radar numbers, flags, mines (armed or dev-revealed),
    /// scorch marks and the blast flash. Art is wired in by the editor menu Minefield/Art/Apply To M2 Scene.
    /// </summary>
    public class MineFieldView : MonoBehaviour
    {
        private const int OrderProbed = PixelConstants.SortingOrderGround + 1;
        private const int OrderRadar = 50;
        private const int OrderBlast = 60;

        private static readonly Color[] DigitColors =
        {
            new Color32(255, 255, 255, 255),
            new Color32(240, 224, 150, 255),
            new Color32(240, 170, 90, 255),
            new Color32(232, 112, 84, 255),
            new Color32(206, 84, 110, 255),
            new Color32(176, 88, 164, 255),
            new Color32(124, 116, 204, 255),
            new Color32(104, 172, 204, 255),
            new Color32(255, 255, 255, 255),
        };

        [SerializeField] private FieldBuilder field;
        [SerializeField] private MineField mines;
        [Header("Grid lines")]
        [SerializeField] private Color gridSoft = new Color(1f, 1f, 1f, 0.55f);

        [SerializeField, HideInInspector] private Sprite[] digits;
        [SerializeField, HideInInspector] private Sprite flagSprite;
        [SerializeField, HideInInspector] private Sprite craterSprite;
        [SerializeField, HideInInspector] private Sprite[] blastFrames;
        [SerializeField, HideInInspector] private Sprite[] dugVariants;
        [SerializeField, HideInInspector] private Sprite[] mineSprites;

        // Everything spawned per cell, so it can be removed again.
        private readonly Dictionary<Vector3Int, GameObject> flags = new Dictionary<Vector3Int, GameObject>();
        private readonly Dictionary<Vector3Int, GameObject> armedMarkers = new Dictionary<Vector3Int, GameObject>();
        private readonly Dictionary<Vector3Int, GameObject> revealedMines = new Dictionary<Vector3Int, GameObject>();
        private readonly Dictionary<Vector3Int, GameObject> revealedNumbers = new Dictionary<Vector3Int, GameObject>();

        private bool gridVisible = true;
        private bool showMines;
        private GameObject gridRoot;
        private Sprite gridUpper, gridLowerLeft, gridLowerRight;

        public bool ShowingAllNumbers => revealedNumbers.Count > 0;

        public void Configure(Sprite[] digitSprites, Sprite flag, Sprite crater, Sprite[] blast, Sprite[] dug, Sprite[] mineArt)
        {
            digits = digitSprites;
            flagSprite = flag;
            craterSprite = crater;
            blastFrames = blast;
            dugVariants = dug;
            mineSprites = mineArt;
        }

        private void OnEnable()
        {
            mines.Regenerated += ClearAll;
            mines.Probed += OnProbed;
            mines.FlagChanged += OnFlagChanged;
            mines.MineTriggered += OnMineTriggered;
            mines.MineArmed += OnMineArmed;
            mines.MineDud += OnMineDud;
            mines.LayoutChanged += OnLayoutChanged;
        }

        private void OnDisable()
        {
            mines.Regenerated -= ClearAll;
            mines.Probed -= OnProbed;
            mines.FlagChanged -= OnFlagChanged;
            mines.MineTriggered -= OnMineTriggered;
            mines.MineArmed -= OnMineArmed;
            mines.MineDud -= OnMineDud;
            mines.LayoutChanged -= OnLayoutChanged;
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            flags.Clear();
            armedMarkers.Clear();
            revealedMines.Clear();
            revealedNumbers.Clear();
            showMines = false;
            BuildGrid();
        }

        // ---------------------------------------------------------------- probing, flags, mines

        private void OnProbed(Vector3Int cell, int number)
        {
            Release(revealedNumbers, cell);
            Spawn("Probed", PickFor(dugVariants, cell), Center(cell), OrderProbed);

            if (number > 0 && number < digits.Length)
            {
                SpriteRenderer digit = Spawn("Radar " + number, digits[number], Center(cell), OrderRadar);
                digit.color = DigitColors[Mathf.Clamp(number, 0, DigitColors.Length - 1)];
            }
        }

        private void OnFlagChanged(Vector3Int cell, bool placed)
        {
            if (!placed)
            {
                Release(flags, cell);
                return;
            }
            SpriteRenderer flag = Spawn("Flag", flagSprite, Center(cell), PixelConstants.SortingOrderDefault);
            flag.spriteSortPoint = SpriteSortPoint.Pivot;
            flags[cell.Flat()] = flag.gameObject;
        }

        private void OnMineArmed(Vector3Int cell, float fuse)
        {
            SpriteRenderer sr = SpawnMine("ArmedMine", cell);
            armedMarkers[cell.Flat()] = sr.gameObject;
            StartCoroutine(Pulse(cell.Flat(), sr, fuse));
        }

        private IEnumerator Pulse(Vector3Int key, SpriteRenderer sr, float fuse)
        {
            float t = 0f;
            while (sr != null && armedMarkers.ContainsKey(key))
            {
                float interval = FuseTiming.BeepInterval(t / fuse);
                sr.color = new Color(1f, 0.35f, 0.3f);
                yield return new WaitForSeconds(0.06f);
                if (sr == null || !armedMarkers.ContainsKey(key)) yield break;
                sr.color = Color.white;
                yield return new WaitForSeconds(Mathf.Max(0.02f, interval - 0.06f));
                t += interval;
            }
        }

        private void OnMineDud(Vector3Int cell)
        {
            Vector3Int key = cell.Flat();
            if (armedMarkers.TryGetValue(key, out GameObject go) && go != null)
            {
                go.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.55f, 0.6f);
            }
            armedMarkers.Remove(key);
        }

        private void OnMineTriggered(Vector3Int cell)
        {
            Release(armedMarkers, cell);
            Release(revealedMines, cell);   // a mine that exploded is gone
            Spawn("Scorch", craterSprite, Center(cell), OrderProbed);
            StartCoroutine(Blast(Center(cell)));
        }

        private IEnumerator Blast(Vector3 center)
        {
            SpriteRenderer sr = Spawn("Blast", blastFrames[0], center, OrderBlast);
            foreach (Sprite frame in blastFrames)
            {
                if (sr == null) yield break;
                sr.sprite = frame;
                yield return new WaitForSeconds(0.07f);
            }
            if (sr != null) Destroy(sr.gameObject);
        }

        // ---------------------------------------------------------------- dev reveals (F1 mines, N numbers)

        public void ToggleMines()
        {
            showMines = !showMines;
            if (!showMines)
            {
                ReleaseAll(revealedMines);
                return;
            }
            foreach (Vector3Int cell in mines.LiveMineCells())
            {
                revealedMines[cell.Flat()] = SpawnMine("MineMarker", cell).gameObject;
            }
        }

        /// <summary>Dev reveal: the radar number of every unprobed, mine-free cell, dimmed.</summary>
        public void ToggleAllNumbers()
        {
            if (ShowingAllNumbers)
            {
                ReleaseAll(revealedNumbers);
                return;
            }
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (mines.IsProbed(cell) || mines.IsExploded(cell) || mines.IsMine(cell)) continue;
                    int n = mines.CountAround(cell);
                    if (n <= 0 || n >= digits.Length) continue;
                    SpriteRenderer sr = Spawn("Hint " + n, digits[n], Center(cell), OrderRadar - 1);
                    Color c = DigitColors[Mathf.Clamp(n, 0, DigitColors.Length - 1)];
                    c.a = 0.65f;
                    sr.color = c;
                    revealedNumbers[cell] = sr.gameObject;
                }
            }
        }

        /// <summary>The safe first probe (or the fairness pass) moved mines: redraw the reveals that are on.</summary>
        private void OnLayoutChanged()
        {
            if (showMines) { ToggleMines(); ToggleMines(); }
            if (ShowingAllNumbers) { ToggleAllNumbers(); ToggleAllNumbers(); }
        }

        // ---------------------------------------------------------------- grid lines

        public void ToggleGrid()
        {
            gridVisible = !gridVisible;
            if (gridRoot != null) gridRoot.SetActive(gridVisible);
        }

        /// <summary>1px diamond outlines on every cell of the plot. Shared edges are drawn once.</summary>
        private void BuildGrid()
        {
            if (gridUpper == null)
            {
                gridUpper = MakeEdgeSprite(true, true, false, false);
                gridLowerLeft = MakeEdgeSprite(false, false, true, false);
                gridLowerRight = MakeEdgeSprite(false, false, false, true);
            }
            gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(transform, false);
            gridRoot.SetActive(gridVisible);
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    Vector3 pos = Center(new Vector3Int(x, y, 0)) + new Vector3(0f, 0.001f, 0f);
                    GridPiece(gridUpper, pos);
                    if (x == 0) GridPiece(gridLowerLeft, pos);
                    if (y == 0) GridPiece(gridLowerRight, pos);
                }
            }
        }

        private void GridPiece(Sprite sprite, Vector3 position)
        {
            SpriteRenderer sr = Spawn("g", sprite, position, OrderProbed, gridRoot.transform);
            sr.color = gridSoft;
        }

        private static Sprite MakeEdgeSprite(bool ul, bool ur, bool ll, bool lr)
        {
            const int W = 32, H = 16;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            var white = new Color32(255, 255, 255, 255);
            void Set(int x, int rowTopDown) { px[(H - 1 - rowTopDown) * W + x] = white; }
            for (int k = 0; k < H / 2; k++)
            {
                int x0 = 16 - 2 * (k + 1);
                int x1 = 16 + 2 * (k + 1) - 1;
                if (ul) { Set(x0, k); Set(x0 + 1, k); }
                if (ur) { Set(x1, k); Set(x1 - 1, k); }
                if (ll) { Set(x0, H - 1 - k); Set(x0 + 1, H - 1 - k); }
                if (lr) { Set(x1, H - 1 - k); Set(x1 - 1, H - 1 - k); }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PixelConstants.PixelsPerUnit);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Stable per-cell pick from a sprite set, so a cell always keeps the same look.</summary>
        private static Sprite PickFor(Sprite[] set, Vector3Int cell)
        {
            uint h = (uint)(cell.x * 73856093 ^ cell.y * 19349663);
            h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
            return set[h % (uint)set.Length];
        }

        /// <summary>Mine art has a bottom-center pivot; lift it so the mine body sits at the cell center.</summary>
        private SpriteRenderer SpawnMine(string objectName, Vector3Int cell)
        {
            Sprite sprite = PickFor(mineSprites, cell);
            return Spawn(objectName, sprite, Center(cell) + new Vector3(0f, -sprite.bounds.size.y * 0.4f, 0f), 0);
        }

        private static void Release(Dictionary<Vector3Int, GameObject> map, Vector3Int cell)
        {
            Vector3Int key = cell.Flat();
            if (!map.TryGetValue(key, out GameObject go)) return;
            if (go != null) Destroy(go);
            map.Remove(key);
        }

        private static void ReleaseAll(Dictionary<Vector3Int, GameObject> map)
        {
            foreach (GameObject go in map.Values)
            {
                if (go != null) Destroy(go);
            }
            map.Clear();
        }

        private Vector3 Center(Vector3Int cell)
        {
            return IsoMath.SnapToPixel(field.CellToWorld(cell));
        }

        private SpriteRenderer Spawn(string objectName, Sprite sprite, Vector3 position, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
