using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Wires the generated art into the M2 scene: ground tile variants, directional deminer, fence, farmhouse and tank.
    /// Menu: Minefield/Art/Apply To M2 Scene
    /// </summary>
    public static class ArtApply
    {
        private const string TileDir = "Assets/Art/Tiles/Test";
        private const string PropDir = "Assets/Art/Objects/Props";
        private const string CharDir = "Assets/Art/Characters";
        private const string ScenePath = "Assets/Scenes/M2_Mines.unity";
        private const string PngExtension = ".png";

        // Numbered sprite series: first index and how many files.
        private const int DigitFirst = 0, DigitCount = 9;     // radar digits 0..8
        private const int BlastFirst = 1, BlastCount = 6;
        private const int DugFirst = 1, DugCount = 3;
        private const int MineFirst = 1, MineCount = 4;

        [MenuItem("Minefield/Art/Apply To M2 Scene")]
        public static void ApplyToM2()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Sprite Load(string path)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null) Debug.LogWarning("[Art] Missing sprite " + path);
                return s;
            }

            // ---- ground
            Tile grassA = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_GrassA.asset");
            Tile grassB = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_GrassB.asset");
            Tile grassC = MakeTile("Tile_GrassC", Load(TileDir + "/grass_c.png"));
            Tile grassD = MakeTile("Tile_GrassD", Load(TileDir + "/grass_d.png"));
            Tile dirt = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_Dirt.asset");
            Tile border = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_Border.asset");

            var field = Object.FindAnyObjectByType<FieldBuilder>();
            var ground = Object.FindAnyObjectByType<Tilemap>();
            var mines = Object.FindAnyObjectByType<MineField>();
            if (field == null || ground == null || mines == null)
            {
                Debug.LogError("[Art] Open scene is missing FieldBuilder / Tilemap / MineField. Build the M2 scene first.");
                return;
            }
            field.Configure(ground, new[] { grassA, grassB, grassC, grassD }, dirt, border);
            EditorUtility.SetDirty(field);

            // ---- deminer
            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<SpriteRenderer>();
            var ds = player.GetComponent<DeminerSprites>();
            if (ds == null) ds = player.gameObject.AddComponent<DeminerSprites>();
            Sprite front = Load(CharDir + "/deminer_front.png");
            ds.Configure(player, body, front, Load(CharDir + "/deminer_back.png"));
            body.sprite = front;
            EditorUtility.SetDirty(ds);
            EditorUtility.SetDirty(body);

            // ---- scenery
            var decor = Object.FindAnyObjectByType<FieldDecor>();
            if (decor == null)
            {
                var go = new GameObject("Decor");
                decor = go.AddComponent<FieldDecor>();
            }
            decor.Configure(field, mines,
                Load(PropDir + "/fence_x.png"), Load(PropDir + "/fence_x_broken.png"),
                Load(PropDir + "/fence_y.png"), Load(PropDir + "/fence_y_broken.png"),
                Load(PropDir + "/farmhouse_big.png"), Load(PropDir + "/tank.png"));
            decor.ConfigureScatter(
                new[] { Load(PropDir + "/tree_dead.png") },
                new[] { Load(PropDir + "/bush.png") },
                new[] { Load(PropDir + "/grass_tuft.png") },
                new[] { Load(PropDir + "/rubble.png") });
            decor.ConfigureRuin(
                new[] { Load(PropDir + "/fence_x_collapse_a.png"), Load(PropDir + "/fence_x_lean.png"), Load(PropDir + "/fence_x_collapse_b.png"), Load(PropDir + "/fence_x_wire.png") },
                new[] { Load(PropDir + "/fence_y_collapse_a.png"), Load(PropDir + "/fence_y_lean.png"), Load(PropDir + "/fence_y_collapse_b.png"), Load(PropDir + "/fence_y_wire.png") },
                new[] { Load(PropDir + "/barrel.png"), Load(PropDir + "/crates.png"), Load(PropDir + "/sandbags.png"),
                        Load(PropDir + "/scrap.png"), Load(PropDir + "/tank_trap.png"), Load(PropDir + "/barbed_wire.png") },
                new[] { Load(PropDir + "/wheat_tall_a.png"), Load(PropDir + "/wheat_tall_b.png"),
                        Load(PropDir + "/wheat_small_a.png"), Load(PropDir + "/wheat_small_b.png") },
                new[] { Load(PropDir + "/haystack.png"), Load(PropDir + "/cart.png") },
                Load(PropDir + "/barn.png"), Load(PropDir + "/well.png"), Load(PropDir + "/kennel.png"),
                Load(PropDir + "/dog_sleep.png"), Load(PropDir + "/sign_mine.png"), Load(PropDir + "/telegraph_pole.png"));
            EditorUtility.SetDirty(decor);

            // ---- field view art: radar digits, flag, scorch mark, blast, dug craters, mines
            var view = Object.FindAnyObjectByType<MineFieldView>();
            if (view != null)
            {
                view.Configure(
                    LoadSeries("Assets/Art/Radar/digit_", DigitFirst, DigitCount),
                    Load("Assets/Art/Objects/Test/flag_small.png"),
                    Load(TileDir + "/crater.png"),
                    LoadSeries(TileDir + "/blast_", BlastFirst, BlastCount),
                    LoadSeries(TileDir + "/dug_", DugFirst, DugCount),
                    LoadSeries("Assets/Art/Objects/Mines/mine_", MineFirst, MineCount));
                EditorUtility.SetDirty(view);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Art] Applied art to " + ScenePath);
        }

        /// <summary>Loads prefix + first .. prefix + (first + count - 1) + ".png".</summary>
        private static Sprite[] LoadSeries(string prefix, int first, int count)
        {
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + (first + i) + PngExtension);
                if (sprites[i] == null) Debug.LogWarning("[Art] Missing sprite " + prefix + (first + i) + PngExtension);
            }
            return sprites;
        }

        private static Tile MakeTile(string name, Sprite sprite)
        {
            string path = TileDir + "/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
            return tile;
        }
    }
}
