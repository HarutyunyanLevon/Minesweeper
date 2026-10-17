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
        // Numbers are painted on the ground: above the dug tile and the flag diamond, below flags, props and the player.
        private const int OrderRadar = OrderProbed + 3;
        private const int OrderBlast = 60;
        private const int OrderFlagCell = OrderProbed + 1;   // glowing diamond under a flag
        private const int OrderMine = OrderProbed + 2;       // flat mine markers: below standing things
        private const int OrderHint = OrderRadar - 1;        // dev hint numbers sit just under real ones
        private const int OrderGrid = OrderProbed;

        private const float FlagCellOffsetY = -PixelConstants.CellHeight * Num.Half;   // diamond sits at the cell's feet
        private const float GridLift = 0.001f;           // avoids z-fighting between grid pieces
        private const float MineLiftShare = 0.4f;        // share of the mine sprite height lowered under the cell centre
        private const float BlastFrameSeconds = 0.07f;
        private const float PulseFlashSeconds = 0.06f;
        private const float PulseMinGap = 0.02f;
        private const float HintAlpha = 0.65f;

        private static readonly Color ArmedFlash = new Color(1f, 0.35f, 0.3f);
        private static readonly Color DudColor = new Color(0.55f, 0.55f, 0.6f);
        private static readonly Color32 EdgeColor = new Color32(255, 255, 255, 255);
        private static readonly Color FlagCellOutline = new Color(1f, 0.84f, 0.25f, 0.92f);
        private static readonly Color FlagCellFill = new Color(1f, 0.84f, 0.25f, 0.24f);

        // Diamond drawing: the edge climbs DiamondSlope pixels sideways per pixel up; outlines are LineWidth pixels wide.
        private const int TileW = PixelConstants.TileWidthPx;
        private const int TileH = PixelConstants.TileHeightPx;
        private const int DiamondSlope = 2;
        private const int OutlineWidth = 2;

        // Stable per-cell hash for choosing sprite variants.
        private const int HashPrimeX = 73856093;
        private const int HashPrimeY = 19349663;
        private const int HashShiftA = 13;
        private const int HashShiftB = 16;
        private const uint HashMultiplier = 1274126177u;

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
            // a glowing diamond on the ground marks exactly which cell is flagged; the flag with its small mound stands on it
            var root = new GameObject("FlagRoot");
            root.transform.SetParent(transform, false);
            SpriteRenderer flag = Spawn("Flag", flagSprite, Center(cell), PixelConstants.SortingOrderDefault, root.transform);
            flag.spriteSortPoint = SpriteSortPoint.Pivot;
            Spawn("FlagCell", FlagCellSprite(), Center(cell) + new Vector3(0f, FlagCellOffsetY, 0f), OrderFlagCell, root.transform);
            // pole and mound stay put; only the cloth waves (rebuilt pixel by pixel)
            MinefieldClerk.Ambience.FlagCloth.Create(flag);
            flags[cell.Flat()] = root;
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
                sr.color = ArmedFlash;
                yield return new WaitForSeconds(PulseFlashSeconds);
                if (sr == null || !armedMarkers.ContainsKey(key)) yield break;
                sr.color = Color.white;
                yield return new WaitForSeconds(Mathf.Max(PulseMinGap, interval - PulseFlashSeconds));
                t += interval;
            }
        }

        private void OnMineDud(Vector3Int cell)
        {
            Vector3Int key = cell.Flat();
            if (armedMarkers.TryGetValue(key, out GameObject go) && go != null)
            {
                go.GetComponent<SpriteRenderer>().color = DudColor;
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
                yield return new WaitForSeconds(BlastFrameSeconds);
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
                    SpriteRenderer sr = Spawn("Hint " + n, digits[n], Center(cell), OrderHint);
                    Color c = DigitColors[Mathf.Clamp(n, 0, DigitColors.Length - 1)];
                    c.a = HintAlpha;
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
                    Vector3 pos = Center(new Vector3Int(x, y, 0)) + new Vector3(0f, GridLift, 0f);
                    GridPiece(gridUpper, pos);
                    if (x == 0) GridPiece(gridLowerLeft, pos);
                    if (y == 0) GridPiece(gridLowerRight, pos);
                }
            }
        }

        private void GridPiece(Sprite sprite, Vector3 position)
        {
            SpriteRenderer sr = Spawn("g", sprite, position, OrderGrid, gridRoot.transform);
            sr.color = gridSoft;
        }

        private static Sprite MakeEdgeSprite(bool ul, bool ur, bool ll, bool lr)
        {
            const int W = TileW, H = TileH, Mid = TileW / 2;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            void Set(int x, int rowTopDown) { px[(H - 1 - rowTopDown) * W + x] = EdgeColor; }
            for (int k = 0; k < H / 2; k++)
            {
                int x0 = Mid - DiamondSlope * (k + 1);
                int x1 = Mid + DiamondSlope * (k + 1) - 1;
                for (int w = 0; w < OutlineWidth; w++)
                {
                    if (ul) Set(x0 + w, k);
                    if (ur) Set(x1 - w, k);
                    if (ll) Set(x0 + w, H - 1 - k);
                    if (lr) Set(x1 - w, H - 1 - k);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(Num.Half, Num.Half), PixelConstants.PixelsPerUnit);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Stable per-cell pick from a sprite set, so a cell always keeps the same look.</summary>
        private static Sprite PickFor(Sprite[] set, Vector3Int cell)
        {
            uint h = (uint)(cell.x * HashPrimeX ^ cell.y * HashPrimeY);
            h ^= h >> HashShiftA; h *= HashMultiplier; h ^= h >> HashShiftB;
            return set[h % (uint)set.Length];
        }

        /// <summary>Mine art has a bottom-center pivot; lift it so the mine body sits at the cell center.</summary>
        private SpriteRenderer SpawnMine(string objectName, Vector3Int cell)
        {
            Sprite sprite = PickFor(mineSprites, cell);
            // flat marker on the ground: always below standing things (player, props, flags)
            return Spawn(objectName, sprite, Center(cell) + new Vector3(0f, -sprite.bounds.size.y * MineLiftShare, 0f), OrderMine);
        }

        private static Sprite flagCellSprite;

        /// <summary>Cell-sized diamond (outline plus faint fill), bottom-center pivot.</summary>
        private static Sprite FlagCellSprite()
        {
            if (flagCellSprite != null) return flagCellSprite;
            const float CenterX = (TileW - 1) * Num.Half;
            const int HalfH = TileH / 2;
            var tex = new Texture2D(TileW, TileH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < TileH; y++)
            {
                int hw = (int)((y < HalfH ? y + Num.Half : TileH - 1 + Num.Half - y) * DiamondSlope);
                for (int x = 0; x < TileW; x++)
                {
                    float dx = Mathf.Abs(x - CenterX);
                    Color c = Color.clear;
                    if (dx <= hw) c = dx >= hw - OutlineWidth ? FlagCellOutline : FlagCellFill;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            flagCellSprite = Sprite.Create(tex, new Rect(0, 0, TileW, TileH), new Vector2(Num.Half, 0f), PixelConstants.PixelsPerUnit);
            return flagCellSprite;
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
