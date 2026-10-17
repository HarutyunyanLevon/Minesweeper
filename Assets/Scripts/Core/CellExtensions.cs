using UnityEngine;

namespace MinefieldClerk
{
    public static class CellExtensions
    {
        /// <summary>The cell with z forced to 0, so it can be used as a dictionary / set key.</summary>
        public static Vector3Int Flat(this Vector3Int cell)
        {
            return new Vector3Int(cell.x, cell.y, 0);
        }
    }
}
