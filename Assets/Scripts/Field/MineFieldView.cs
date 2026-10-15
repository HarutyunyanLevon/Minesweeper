using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Draws the mine field state: probed-tile shading, radar numbers (floating, not physically on the field),
    /// flags (physical objects), craters and the blast flash. Placeholder art in M2.
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

        [SerializeField, HideInInspector] private Sprite probedOverlay;
        [SerializeField, HideInInspector] private Sprite probedOverlayB;
        [SerializeField, HideInInspector] private Sprite[] digits;
        [SerializeField, HideInInspector] private Sprite flagSprite;
        [SerializeField, HideInInspector] private Sprite craterSprite;
        [SerializeField, HideInInspector] private Sprite[] blastFrames;
        [SerializeField, HideInInspector] private Sprite mineMarker;

        private readonly Dictionary<Vector3Int, GameObject> flagObjects = new Dictionary<Vector3Int, GameObject>();
        private readonly List<GameObject> markers = new List<GameObject>();
        [Header("Grid lines")]
        [SerializeField] private Color gridTint = new Color(0.10f, 0.05f, 0.0f, 0.7f);
        private bool gridVisible = true;
        private GameObject gridRoot;
        private Sprite gridUpper, gridLowerLeft, gridLowerRight;

        private readonly Dictionary<Vector3Int, GameObject> armedMarkers = new Dictionary<Vector3Int, GameObject>();
        private bool showMines;
        private bool allNumbers;
        private readonly Dictionary<Vector3Int, GameObject> allNumberObjects = new Dictionary<Vector3Int, GameObject>();

        public bool ShowingGrid => gridVisible;

        public void ToggleGrid()
        {
            gridVisible = !gridVisible;
            if (gridRoot != null) gridRoot.SetActive(gridVisible);
        }

        /// <summary>1px diamond outlines drawn on every cell of the plot. Shared edges are drawn once.</summary>
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
                    var cell = new Vector3Int(x, y, 0);
                    Vector3 pos = Center(cell) + new Vector3(0f, 0.001f, 0f);
                    GridPiece(gridUpper, pos);
                    if (x == 0) GridPiece(gridLowerLeft, pos);
                    if (y == 0) GridPiece(gridLowerRight, pos);
                }
            }
        }

        private void GridPiece(Sprite sprite, Vector3 position)
        {
            var go = new GameObject("g");
            go.transform.SetParent(gridRoot.transform, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = gridTint;
            sr.sortingOrder = OrderProbed;
        }

        private static Sprite MakeEdgeSprite(bool ul, bool ur, bool ll, bool lr)
        {
            const int W = 32, H = 16;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            Color32 white = new Color32(255, 255, 255, 255);
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

        public bool ShowingMines => showMines;
        public bool ShowingAllNumbers => allNumbers;

        /// <summary>Dev reveal: shows the radar number of every unprobed, mine-free cell, dimmed. Like F1 does for mines.</summary>
        public void ToggleAllNumbers()
        {
            allNumbers = !allNumbers;
            if (allNumbers)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    for (int y = 0; y < field.Height; y++)
                    {
                        var cell = new Vector3Int(x, y, 0);
                        if (mines.IsProbed(cell) || mines.IsExploded(cell) || mines.IsMine(cell)) continue;
                        int n = mines.CountAround(cell);
                        if (n <= 0 || n >= digits.Length) continue;
                        SpriteRenderer sr = Spawn("Hint " + n, digits[n], Center(cell) + new Vector3(0f, 5f / PixelConstants.PixelsPerUnit, 0f), OrderRadar - 1);
                        Color c = DigitColors[Mathf.Clamp(n, 0, DigitColors.Length - 1)];
                        c.a = 0.65f;
                        sr.color = c;
                        allNumberObjects[cell] = sr.gameObject;
                    }
                }
            }
            else
            {
                ClearAllNumberObjects();
            }
        }

        private void ClearAllNumberObjects()
        {
            foreach (GameObject go in allNumberObjects.Values)
            {
                if (go != null) Destroy(go);
            }
            allNumberObjects.Clear();
        }

        public void ConfigureVariant(Sprite overlayB)
        {
            probedOverlayB = overlayB;
        }

        public void Configure(FieldBuilder fieldBuilder, MineField mineField, Sprite overlay, Sprite[] digitSprites,
            Sprite flag, Sprite crater, Sprite[] blast, Sprite marker)
        {
            field = fieldBuilder;
            mines = mineField;
            probedOverlay = overlay;
            digits = digitSprites;
            flagSprite = flag;
            craterSprite = crater;
            blastFrames = blast;
            mineMarker = marker;
        }

        private void OnEnable()
        {
            Debug.Log("[M2] View sprites: overlay=" + (probedOverlay != null) + " flag=" + (flagSprite != null) + " digits=" + (digits == null ? -1 : digits.Length) + " blast=" + (blastFrames == null ? -1 : blastFrames.Length));
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
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            flagObjects.Clear();
            markers.Clear();
            armedMarkers.Clear();
            allNumberObjects.Clear();
            allNumbers = false;
            showMines = false;
            BuildGrid();
        }

        public void ToggleMines()
        {
            showMines = !showMines;
            if (showMines)
            {
                foreach (Vector3Int cell in mines.LiveMineCells())
                {
                    markers.Add(Spawn("MineMarker", mineMarker, Center(cell), 0).gameObject);
                }
            }
            else
            {
                foreach (GameObject marker in markers)
                {
                    if (marker != null) Destroy(marker);
                }
                markers.Clear();
            }
        }

        private void OnProbed(Vector3Int cell, int number)
        {
            if (allNumberObjects.TryGetValue(cell, out GameObject hint))
            {
                if (hint != null) Destroy(hint);
                allNumberObjects.Remove(cell);
            }

            Vector3 center = Center(cell);
            Sprite dug = (probedOverlayB != null && (((cell.x * 7 + cell.y * 13) & 1) == 1)) ? probedOverlayB : probedOverlay;
            Spawn("Probed", dug, center, OrderProbed);

            if (number > 0 && number < digits.Length)
            {
                // The number floats a little above the tile center, like a radar readout.
                SpriteRenderer digit = Spawn("Radar " + number, digits[number], center + new Vector3(0f, 5f / PixelConstants.PixelsPerUnit, 0f), OrderRadar);
                digit.color = DigitColors[Mathf.Clamp(number, 0, DigitColors.Length - 1)];
            }
        }

        private void OnFlagChanged(Vector3Int cell, bool placed)
        {
            Vector3Int key = new Vector3Int(cell.x, cell.y, 0);
            if (placed)
            {
                SpriteRenderer flag = Spawn("Flag", flagSprite, Center(cell), PixelConstants.SortingOrderDefault);
                flag.spriteSortPoint = SpriteSortPoint.Pivot;
                flagObjects[key] = flag.gameObject;
            }
            else if (flagObjects.TryGetValue(key, out GameObject go))
            {
                if (go != null) Destroy(go);
                flagObjects.Remove(key);
            }
        }

        private void OnLayoutChanged()
        {
            // The safe first probe moved mines: refresh the dev reveals if they are on.
            if (showMines) { ToggleMines(); ToggleMines(); }
            if (allNumbers) { ToggleAllNumbers(); ToggleAllNumbers(); }
        }

        private void OnMineArmed(Vector3Int cell, float fuse)
        {
            Vector3Int key = new Vector3Int(cell.x, cell.y, 0);
            SpriteRenderer sr = Spawn("ArmedMine", mineMarker, Center(cell), 0);
            armedMarkers[key] = sr.gameObject;
            StartCoroutine(Pulse(key, sr, fuse));
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
            Vector3Int key = new Vector3Int(cell.x, cell.y, 0);
            if (armedMarkers.TryGetValue(key, out GameObject go) && go != null)
            {
                go.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.55f, 0.6f);
            }
            armedMarkers.Remove(key);
        }

        private void OnMineTriggered(Vector3Int cell)
        {
            Vector3Int key = new Vector3Int(cell.x, cell.y, 0);
            if (armedMarkers.TryGetValue(key, out GameObject armedGo))
            {
                if (armedGo != null) Destroy(armedGo);
                armedMarkers.Remove(key);
            }
            Spawn("Crater", craterSprite, Center(cell), OrderProbed);
            StartCoroutine(Blast(Center(cell)));
        }

        private IEnumerator Blast(Vector3 center)
        {
            SpriteRenderer sr = Spawn("Blast", blastFrames[0], center, OrderBlast);
            for (int i = 0; i < blastFrames.Length; i++)
            {
                if (sr == null) yield break;
                sr.sprite = blastFrames[i];
                yield return new WaitForSeconds(0.07f);
            }
            if (sr != null) Destroy(sr.gameObject);
        }

        private Vector3 Center(Vector3Int cell)
        {
            return IsoMath.SnapToPixel(field.CellToWorld(cell));
        }

        private SpriteRenderer Spawn(string objectName, Sprite sprite, Vector3 position, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
