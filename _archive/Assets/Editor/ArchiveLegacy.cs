using System.IO;
using UnityEditor;
using UnityEngine;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// One-off cleanup: moves prototype leftovers (M0/M1/M2 scene builders, old test scenes, dev scripts and placeholder art)
    /// out of Assets into the project's _archive folder. Nothing is deleted; move a file back to restore it.
    /// Menu: Minefield/Tools/Archive Legacy Files. This script archives itself last.
    /// </summary>
    public static class ArchiveLegacy
    {
        private static readonly string[] Files =
        {
            "Assets/Editor/M0Setup.cs", "Assets/Editor/M1Setup.cs", "Assets/Editor/M2Setup.cs", "Assets/Editor/SceneKit.cs",
            "Assets/Scripts/DevTools/CameraPanTest.cs", "Assets/Scripts/DevTools/CellReadout.cs", "Assets/Scripts/DevTools/SortTestWalker.cs",
            "Assets/Scenes/M0_PixelTest.unity", "Assets/Scenes/M1_Field.unity",
            "Assets/Art/Objects/Test/post.png", "Assets/Art/Objects/Test/walker.png", "Assets/Art/Objects/Test/mine_debug.png",
            "Assets/Art/Tiles/Test/checker.png", "Assets/Art/Tiles/Test/Tile_Checker.asset",
            "Assets/Art/Tiles/Test/probed.png", "Assets/Art/Tiles/Test/probed_b.png",
            "Assets/Editor/ArchiveLegacy.cs",
        };

        [MenuItem("Minefield/Tools/Archive Legacy Files")]
        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            int moved = 0;
            foreach (string rel in Files)
            {
                foreach (string suffix in new[] { "", ".meta" })
                {
                    string from = Path.Combine(root, rel + suffix);
                    if (!File.Exists(from)) continue;
                    string to = Path.Combine(root, "_archive", rel + suffix);
                    Directory.CreateDirectory(Path.GetDirectoryName(to));
                    if (File.Exists(to)) File.Delete(to);
                    File.Move(from, to);
                    if (suffix == "") moved++;
                }
            }
            Debug.Log("[Archive] Moved " + moved + " files to _archive/");
            AssetDatabase.Refresh();
        }
    }
}
