using UnityEngine;

namespace MinefieldClerk
{
    /// <summary>Small isometric and pixel helpers shared by gameplay code.</summary>
    public static class IsoMath
    {
        public static float SnapToPixel(float value)
        {
            return Mathf.Round(value * PixelConstants.PixelsPerUnit) / PixelConstants.PixelsPerUnit;
        }

        public static Vector3 SnapToPixel(Vector3 position)
        {
            return new Vector3(SnapToPixel(position.x), SnapToPixel(position.y), position.z);
        }

        /// <summary>
        /// Splits a screen-space movement into two parts that run along the two isometric cell axes.
        /// Field walls follow the cell axes, so moving along one axis at a time lets the player slide along a wall.
        /// </summary>
        public static void SplitAlongCellAxes(Vector2 delta, out Vector2 alongX, out Vector2 alongY)
        {
            float cw = PixelConstants.CellWidth;
            float ch = PixelConstants.CellHeight;

            // delta = a * (cw/2, ch/2) + b * (-cw/2, ch/2)
            float a = delta.x / cw + delta.y / ch;
            float b = delta.y / ch - delta.x / cw;

            alongX = new Vector2(a * cw * 0.5f, a * ch * 0.5f);
            alongY = new Vector2(-b * cw * 0.5f, b * ch * 0.5f);
        }
    }
}
