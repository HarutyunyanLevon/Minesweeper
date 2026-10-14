namespace MinefieldClerk
{
    /// <summary>
    /// Single source of truth for the pixel-perfect and isometric setup.
    /// Reference resolution, PPU and tile size are locked once art production starts: do not change them.
    /// </summary>
    public static class PixelConstants
    {
        // Isometric ground tiles are 2:1 diamonds, 32 x 16 px. One world unit = 32 px.
        public const int PixelsPerUnit = 32;
        public const int TileWidthPx = 32;
        public const int TileHeightPx = 16;

        public const int RefResolutionX = 480;
        public const int RefResolutionY = 270;

        // (refResY / 2) / PPU = 135 / 32 = 4.21875
        public const float OrthoSize = (RefResolutionY / 2f) / PixelsPerUnit;

        // Isometric grid cell size in world units: (tile width, tile height, 1).
        public const float CellWidth = TileWidthPx / (float)PixelsPerUnit;    // 1.0
        public const float CellHeight = TileHeightPx / (float)PixelsPerUnit;  // 0.5

        // Every texture under this folder gets the pixel art import settings automatically.
        public const string ArtRoot = "Assets/Art/";

        // Sprites in these folders get a bottom-center pivot, so they sort by their feet.
        // Ground tiles (Assets/Art/Tiles) keep a centered pivot.
        public const string ObjectsFolder = "Assets/Art/Objects/";
        public const string CharactersFolder = "Assets/Art/Characters/";

        // Sprite sheets (Multiple mode, own slicing) go here; all other art is imported as a Single sprite.
        public const string SheetsFolder = "Assets/Art/Sheets/";

        // Isometric sorting: things lower on screen (smaller Y) draw in front.
        // The ground tilemap uses a lower sorting order so it always stays behind everything.
        public const int SortingOrderGround = -10;
        public const int SortingOrderDefault = 0;
    }
}
