using UnityEngine;
using UnityEngine.Tilemaps;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Builds the ground of a rectangular minefield (in isometric cells) and answers walkability questions.
    /// The playable plot is cells (0,0) to (width-1, height-1), surrounded by a one-cell border ring and some scenery.
    /// </summary>
    public class FieldBuilder : MonoBehaviour
    {
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tile[] grassTiles;
        [SerializeField] private Tile dirtTile;
        [SerializeField] private Tile borderTile;
        [SerializeField] private int fieldWidth = 24;
        [SerializeField] private int fieldHeight = 16;
        // Ground tiles around the plot. The camera sees about 16 cells past the player, so 22 keeps the edge of the ground
        // off screen everywhere on the plot. A const, so an old scene value cannot shrink it.
        private const int scenery = 22;
        private const int BorderRing = 1;   // cells of border tile around the plot
        [Tooltip("One in this many plot cells is dirt instead of grass")]
        [SerializeField] private int dirtOneIn = 7;
        [Tooltip("-1 = new random field on every play")]
        [SerializeField] private int seed = -1;

        /// <summary>Size of the field (Medium) the scenery was laid out around. Other sizes stretch the layout from it.</summary>
        private const int StartColumnDivisor = 2;   // the player starts in the middle column
        public const int AnchorWidth = 24;
        public const int AnchorHeight = 16;

        /// <summary>World offset of the field grid (none: cell (0,0) is the world origin, the props move instead).</summary>
        public Vector3 Origin => Vector3.zero;

        public int Width => fieldWidth;
        public int Height => fieldHeight;
        public Vector3Int StartCell => new Vector3Int(fieldWidth / StartColumnDivisor, 0, 0);

        /// <summary>Sets the plot size for the next Build (difficulty presets).</summary>
        public void Resize(int width, int height)
        {
            fieldWidth = Mathf.Max(DifficultySettings.MinFieldSide, width);
            fieldHeight = Mathf.Max(DifficultySettings.MinFieldSide, height);
        }

        /// <summary>Used by the editor art menu to wire up the tiles.</summary>
        public void Configure(Tilemap groundMap, Tile[] grass, Tile dirt, Tile border)
        {
            ground = groundMap;
            grassTiles = grass;
            dirtTile = dirt;
            borderTile = border;
        }

        public bool IsWalkable(Vector3Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < fieldWidth && cell.y < fieldHeight;
        }

        public Vector3Int WorldToCell(Vector3 world)
        {
            return ground.WorldToCell(world);
        }

        /// <summary>
        /// Position in cell units without rounding: cell (x, y) spans x..x+1 and y..y+1, its centre is (x+0.5, y+0.5).
        /// Lets the player's body be measured against cells (area overlap) instead of only the cell under the feet.
        /// </summary>
        public Vector2 WorldToCellInterpolated(Vector3 world)
        {
            Vector3 cell = ground.layoutGrid.LocalToCellInterpolated(ground.layoutGrid.WorldToLocal(world));
            return new Vector2(cell.x, cell.y);
        }

        public Vector3 CellToWorld(Vector3Int cell)
        {
            return ground.GetCellCenterWorld(cell);
        }

        [ContextMenu("Build")]
        public void Build()
        {
            if (ground == null || grassTiles == null || grassTiles.Length == 0)
            {
                Debug.LogError("[FieldBuilder] Ground tilemap or grass tiles are not assigned.");
                return;
            }

            ground.layoutGrid.transform.position = Origin;
            ground.ClearAllTiles();
            var rng = seed >= 0 ? new System.Random(seed) : new System.Random();

            int min = -BorderRing - scenery;
            for (int x = min; x <= fieldWidth - 1 + BorderRing + scenery; x++)
            {
                for (int y = min; y <= fieldHeight - 1 + BorderRing + scenery; y++)
                {
                    bool inPlot = x >= 0 && x < fieldWidth && y >= 0 && y < fieldHeight;
                    bool inPlotOrRing = x >= -BorderRing && x <= fieldWidth - 1 + BorderRing && y >= -BorderRing && y <= fieldHeight - 1 + BorderRing;

                    Tile tile;
                    if (inPlot)
                    {
                        tile = (dirtTile != null && rng.Next(dirtOneIn) == 0) ? dirtTile : grassTiles[rng.Next(grassTiles.Length)];
                    }
                    else if (inPlotOrRing && borderTile != null)
                    {
                        tile = borderTile;
                    }
                    else
                    {
                        tile = grassTiles[rng.Next(grassTiles.Length)];
                    }

                    ground.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }
}
