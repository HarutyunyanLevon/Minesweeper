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
        [SerializeField, HideInInspector] private Sprite[] digits;
        [SerializeField, HideInInspector] private Sprite flagSprite;
        [SerializeField, HideInInspector] private Sprite craterSprite;
        [SerializeField, HideInInspector] private Sprite[] blastFrames;
        [SerializeField, HideInInspector] private Sprite mineMarker;

        private readonly Dictionary<Vector3Int, GameObject> flagObjects = new Dictionary<Vector3Int, GameObject>();
        private readonly List<GameObject> markers = new List<GameObject>();
        private bool showMines;
        private bool allNumbers;
        private readonly Dictionary<Vector3Int, GameObject> allNumberObjects = new Dictionary<Vector3Int, GameObject>();

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
        }

        private void OnDisable()
        {
            mines.Regenerated -= ClearAll;
            mines.Probed -= OnProbed;
            mines.FlagChanged -= OnFlagChanged;
            mines.MineTriggered -= OnMineTriggered;
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
            allNumberObjects.Clear();
            allNumbers = false;
            showMines = false;
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
            Spawn("Probed", probedOverlay, center, OrderProbed);

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

        private void OnMineTriggered(Vector3Int cell)
        {
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
