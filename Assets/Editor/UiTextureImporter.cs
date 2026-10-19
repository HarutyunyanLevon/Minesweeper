using System;
using UnityEditor;
using UnityEngine;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Textures under Assets/Resources/UI and Assets/Resources/Rulebook are drawn by IMGUI, not as sprites:
    /// point filter, no mipmaps, uncompressed, clamped.
    /// </summary>
    public class UiTextureImporter : AssetPostprocessor
    {
        private static readonly string[] Folders = { "Assets/Resources/UI/", "Assets/Resources/Rulebook/" };

        private void OnPreprocessTexture()
        {
            bool match = false;
            foreach (string folder in Folders)
            {
                if (assetPath.StartsWith(folder, StringComparison.Ordinal)) match = true;
            }
            if (!match) return;

            var importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
        }
    }
}
