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
        [SerializeField] private int scenery = 8;
        [Tooltip("-1 = new random field on every play")]
        [SerializeField] private int seed = -1;

        public int Width => fieldWidth;
        public int Height => fieldHeight;
        public Vector3Int StartCell => new Vector3Int(fieldWidth / 2, 0, 0);

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

            ground.ClearAllTiles();
            var rng = seed >= 0 ? new System.Random(seed) : new System.Random();

            int min = -1 - scenery;
            for (int x = min; x <= fieldWidth + scenery; x++)
            {
                for (int y = min; y <= fieldHeight + scenery; y++)
                {
                    bool inPlot = x >= 0 && x < fieldWidth && y >= 0 && y < fieldHeight;
                    bool inPlotOrRing = x >= -1 && x <= fieldWidth && y >= -1 && y <= fieldHeight;

                    Tile tile;
                    if (inPlot)
                    {
                        tile = (dirtTile != null && rng.Next(7) == 0) ? dirtTile : grassTiles[rng.Next(grassTiles.Length)];
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
