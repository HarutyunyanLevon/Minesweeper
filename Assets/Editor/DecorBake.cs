using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MinefieldClerk.Field;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Turns the generated scenery into ordinary scene objects, sorted into category groups under "Decor",
    /// so props can be moved, deleted or duplicated by hand. Only the fence keeps rebuilding for the field size.
    /// Menu: Minefield/Decor/Regenerate And Bake (discards manual edits)
    /// </summary>
    public static class DecorBake
    {
        private const string OutDir = "Assets/Art/Generated";
        private const string SpritePrefix = "/decor_";
        private const string PngExtension = ".png";

        [MenuItem("Minefield/Decor/Regenerate And Bake (discards manual edits)")]
        public static void Bake()
        {
            var decor = Object.FindAnyObjectByType<FieldDecor>();
            if (decor == null)
            {
                Debug.LogError("[Decor] No FieldDecor in the open scene.");
                return;
            }
            if (decor.Baked && !EditorUtility.DisplayDialog("Regenerate decor",
                    "The decor is already baked. Regenerating replaces every prop and throws away your manual changes. Continue?",
                    "Regenerate", "Cancel"))
            {
                return;
            }

            decor.Baked = false;
            decor.Build();
            PersistSprites(decor);
            decor.Baked = true;

            EditorUtility.SetDirty(decor);
            EditorSceneManager.MarkSceneDirty(decor.gameObject.scene);
            EditorSceneManager.SaveScene(decor.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Decor] Baked " + decor.GetComponentsInChildren<SpriteRenderer>().Length + " props into the scene under Decor.");
        }

        [MenuItem("Minefield/Decor/Rebuild Fence And Tracks")]
        public static void RebuildAuto()
        {
            var decor = Object.FindAnyObjectByType<FieldDecor>();
            if (decor == null) return;
            decor.RebuildAuto();
            PersistSprites(decor);
            EditorUtility.SetDirty(decor);
            EditorSceneManager.MarkSceneDirty(decor.gameObject.scene);
            EditorSceneManager.SaveScene(decor.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Procedural sprites (decals, tracks) only exist in memory; save them as PNG assets so the scene keeps them.</summary>
        private static void PersistSprites(FieldDecor decor)
        {
            Directory.CreateDirectory(OutDir);
            var saved = new Dictionary<Sprite, Sprite>();
            int index = 0;
            foreach (SpriteRenderer sr in decor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Sprite s = sr.sprite;
                if (s == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(s))) continue;

                if (!saved.TryGetValue(s, out Sprite asset))
                {
                    string path = OutDir + SpritePrefix + (index++) + PngExtension;
                    Texture2D tex = s.texture;
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = s.pixelsPerUnit;
                    imp.filterMode = FilterMode.Point;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.mipmapEnabled = false;
                    imp.alphaIsTransparency = true;
                    var settings = new TextureImporterSettings();
                    imp.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
                    imp.SetTextureSettings(settings);
                    imp.SaveAndReimport();
                    asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    saved[s] = asset;
                }
                sr.sprite = asset;
            }
        }
    }
}
