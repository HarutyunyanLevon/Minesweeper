using System;
using UnityEditor;
using UnityEngine;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Applies the pixel art import settings to every texture under Assets/Art/.
    /// Point filter, no mipmaps, uncompressed, PPU 32, full-rect sprite mesh.
    /// Sprite sheets live under Assets/Art/Sheets/ and keep their slicing; everything else becomes a single sprite.
    /// Single sprites under Assets/Art/Objects/ and Assets/Art/Characters/ get a bottom-center pivot
    /// (isometric: they sort by their feet). Keep their pixel width and height even.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        // Safety net: the project's default import preset may leave new textures in Multiple mode, and setting the
        // mode inside OnPreprocessTexture is not always honored. Fix it after the import and reimport once.
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (!path.StartsWith(PixelConstants.ArtRoot, StringComparison.Ordinal)) continue;
                if (path.StartsWith(PixelConstants.SheetsFolder, StringComparison.Ordinal)) continue;
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PixelConstants.ArtRoot, StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;

            // Everything is a Single sprite (full texture rect), except textures under Assets/Art/Sheets/,
            // which are sprite sheets and keep their own slicing. A stale Multiple mode with an old slice rect
            // crops the sprite (this is what turned 32x16 tiles into half-diamond triangles).
            bool isSheet = assetPath.StartsWith(PixelConstants.SheetsFolder, StringComparison.Ordinal);
            if (!isSheet)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }
            else if (importer.spriteImportMode == SpriteImportMode.None)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
            }

            // Texture-level settings first, so the property assignments below are not overwritten.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            // SetTextureSettings below writes the whole settings block back, so the sprite mode must be set here too,
            // otherwise a stale Multiple mode is restored.
            settings.spriteMode = isSheet ? (int)SpriteImportMode.Multiple : (int)SpriteImportMode.Single;

            bool bottomPivot =
                assetPath.StartsWith(PixelConstants.ObjectsFolder, StringComparison.Ordinal) ||
                assetPath.StartsWith(PixelConstants.CharactersFolder, StringComparison.Ordinal);
            if (bottomPivot && !isSheet)
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }
            importer.SetTextureSettings(settings);

            importer.spritePixelsPerUnit = PixelConstants.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;
        }
    }
}
