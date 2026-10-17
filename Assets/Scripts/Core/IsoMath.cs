using UnityEngine;

namespace MinefieldClerk
{
    /// <summary>Small isometric and pixel helpers shared by gameplay code.</summary>
    public static class IsoMath
    {
        /// <summary>Cell-edge units (u, v) per world unit of height: the 2:1 projection makes this width / height.</summary>
        public const float Slope = PixelConstants.CellWidth / PixelConstants.CellHeight;

        /// <summary>World position of a point in cell-edge coordinates (u along cell x, v along cell y).</summary>
        public static Vector3 EdgeToWorld(float u, float v)
        {
            return new Vector3((u - v) * Num.Half, (u + v) * Num.Quarter, 0f);
        }

        /// <summary>Inverse of <see cref="EdgeToWorld"/>.</summary>
        public static Vector2 WorldToEdge(Vector3 world)
        {
            return new Vector2(world.x + Slope * world.y, Slope * world.y - world.x);
        }

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

            alongX = new Vector2(a * cw * Num.Half, a * ch * Num.Half);
            alongY = new Vector2(-b * cw * Num.Half, b * ch * Num.Half);
        }
    }
}
