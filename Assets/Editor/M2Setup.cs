using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using MinefieldClerk.CameraRig;
using MinefieldClerk.DevTools;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// M2: builds Assets/Scenes/M2_Mines.unity, the core mine loop with placeholder art:
    /// mines, probing the cell in front of you, radar numbers, flags, mine hits.
    /// Needs the M0 test tiles and sprites (run Minefield/M0/Run Setup first).
    /// Menu: Minefield/M2/Build Mines Scene
    /// </summary>
    public static class M2Setup
    {
        private const string TileDir = "Assets/Art/Tiles/Test";
        private const string ObjectsDir = "Assets/Art/Objects/Test";
        private const string RadarDir = "Assets/Art/Radar";
        private const string ScenePath = "Assets/Scenes/M2_Mines.unity";

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        // 5x7 digit font, top row first.
        private static readonly string[][] Glyphs =
        {
            new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
            new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
            new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
            new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
            new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
            new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" },
            new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
            new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" },
            new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" },
        };

        [MenuItem("Minefield/M2/Build Mines Scene")]
        public static void BuildMinesScene()
        {
            var grassA = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_GrassA.asset");
            var grassB = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_GrassB.asset");
            var dirt = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_Dirt.asset");
            var border = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_Border.asset");
            var walker = AssetDatabase.LoadAssetAtPath<Sprite>(ObjectsDir + "/walker.png");
            if (grassA == null || grassB == null || dirt == null || border == null || walker == null)
            {
                Debug.LogError("[M2] Missing M0 test assets. Run Minefield/M0/Run Setup first.");
                return;
            }

            EnsureFolder("Assets/Scenes");
            EnsureFolder(RadarDir);

            // ---- placeholder art
            Sprite cursor = MakeSprite(TileDir, "cursor", 32, 16, CursorPixel);
            Sprite overlay = MakeSprite(TileDir, "probed", 32, 16, (x, y) => InDiamond(x, y) ? new Color32(0, 0, 0, 96) : Clear);
            Sprite crater = MakeSprite(TileDir, "crater", 32, 16, CraterPixel);
            Sprite[] blast =
            {
                MakeSprite(TileDir, "blast_1", 16, 16, (x, y) => BlastPixel(x, y, 16)),
                MakeSprite(TileDir, "blast_2", 24, 24, (x, y) => BlastPixel(x, y, 24)),
                MakeSprite(TileDir, "blast_3", 32, 32, (x, y) => BlastPixel(x, y, 32)),
            };
            Sprite flag = MakeSprite(ObjectsDir, "flag", 8, 16, FlagPixel);
            Sprite marker = MakeSprite(ObjectsDir, "mine_debug", 10, 8, MarkerPixel);
            var digits = new Sprite[Glyphs.Length];
            for (int d = 0; d < digits.Length; d++)
            {
                int digit = d;
                digits[d] = MakeSprite(RadarDir, "digit_" + d, 8, 10, (x, y) => DigitPixel(digit, x, y));
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera cam = SceneKit.CreateCamera(new Vector3(0f, 0f, -10f));
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            SceneKit.CreateGlobalLight();
            Tilemap ground = SceneKit.CreateIsoGround(out Grid grid);

            // ---- field and mines
            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(grid.transform, false);
            var field = fieldGo.AddComponent<FieldBuilder>();
            field.Configure(ground, new[] { grassA, grassB }, dirt, border);
            field.SetBuildOnAwake(false);
            EditorUtility.SetDirty(field);

            var minesGo = new GameObject("Mines");
            var mines = minesGo.AddComponent<MineField>();
            mines.Configure(field);
            EditorUtility.SetDirty(mines);

            var viewGo = new GameObject("MineFieldView");
            var view = viewGo.AddComponent<MineFieldView>();
            view.Configure(field, mines, overlay, digits, flag, crater, blast, marker);
            EditorUtility.SetDirty(view);

            // ---- player
            var playerGo = new GameObject("Player");
            var body = playerGo.AddComponent<SpriteRenderer>();
            body.sprite = walker;
            body.sortingOrder = PixelConstants.SortingOrderDefault;
            body.spriteSortPoint = SpriteSortPoint.Pivot;
            var player = playerGo.AddComponent<PlayerController>();
            SetObject(player, "field", field);
            SetObject(player, "body", body);
            playerGo.transform.position = field.CellToWorld(field.StartCell);

            var health = playerGo.AddComponent<PlayerHealth>();
            SetObject(health, "player", player);
            SetObject(health, "body", body);

            var probe = playerGo.AddComponent<PlayerProbe>();
            SetObject(probe, "player", player);
            SetObject(probe, "mines", mines);
            SetObject(probe, "health", health);

            // ---- cursor: the cell the player faces, above the probed shading
            var cursorGo = new GameObject("CellCursor");
            var cursorSr = cursorGo.AddComponent<SpriteRenderer>();
            cursorSr.sprite = cursor;
            cursorSr.sortingOrder = PixelConstants.SortingOrderGround + 2;
            var cellCursor = cursorGo.AddComponent<CellCursor>();
            SetObject(cellCursor, "field", field);
            SetObject(cellCursor, "player", player);

            // ---- session, camera, hud
            var sessionGo = new GameObject("Session");
            var session = sessionGo.AddComponent<FieldSession>();
            SetObject(session, "field", field);
            SetObject(session, "mines", mines);
            SetObject(session, "view", view);
            SetObject(session, "player", player);
            SetObject(session, "health", health);

            SetObject(follow, "target", playerGo.transform);
            cam.transform.position = new Vector3(playerGo.transform.position.x, playerGo.transform.position.y + 0.35f, -10f);

            var hudGo = new GameObject("DevHud");
            var hud = hudGo.AddComponent<DevHud>();
            SetObject(hud, "player", player);
            SetObject(hud, "mines", mines);
            SetObject(hud, "health", health);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[M2] Built " + ScenePath + ". Play: WASD walk, Space probe, F flag, R new field, F1 show mines.");
        }

        // ------------------------------------------------------------------ sprite makers

        private static Sprite MakeSprite(string dir, string name, int width, int height, Func<int, int, Color32> pixel)
        {
            string assetPath = dir + "/" + name + ".png";
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, pixel(x, y));
                }
            }
            tex.Apply();
            File.WriteAllBytes(Path.Combine(ProjectRoot(), assetPath), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144665;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) & 0x7fffffff;
            }
        }

        // Same 2:1 diamond as the ground tiles.
        private static bool InDiamond(int x, int y)
        {
            int height = PixelConstants.TileHeightPx;
            int width = PixelConstants.TileWidthPx;
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            int half = height / 2;
            int k = y < half ? y : height - 1 - y;
            int rowWidth = 4 * (k + 1);
            int center = width / 2;
            return x >= center - rowWidth / 2 && x < center + rowWidth / 2;
        }

        private static Color32 CursorPixel(int x, int y)
        {
            if (!InDiamond(x, y)) return Clear;
            bool edge = !InDiamond(x - 1, y) || !InDiamond(x + 1, y) || !InDiamond(x, y - 1) || !InDiamond(x, y + 1);
            return edge ? new Color32(255, 240, 200, 230) : new Color32(255, 240, 200, 28);
        }

        private static Color32 CraterPixel(int x, int y)
        {
            if (!InDiamond(x, y)) return Clear;
            float dx = Mathf.Abs(x + 0.5f - 16f) / 16f;
            float dy = Mathf.Abs(y + 0.5f - 8f) / 8f;
            float d = dx + dy;
            int noise = (Hash(x, y, 41) & 15) - 8;
            if (d <= 0.7f) return new Color32((byte)(34 + noise), (byte)(24 + noise), (byte)(18 + noise), 255);
            if (d <= 0.9f) return new Color32((byte)(58 + noise), (byte)(40 + noise), (byte)(28 + noise), 230);
            return new Color32(40, 30, 22, 90);
        }

        private static Color32 BlastPixel(int x, int y, int size)
        {
            float half = size / 2f;
            float dx = x + 0.5f - half;
            float dy = y + 0.5f - half;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / half;
            float edge = 0.7f + 0.3f * ((Hash(x / 2, y / 2, 43) & 255) / 255f);
            if (r > edge) return Clear;
            if (r < 0.35f) return new Color32(255, 246, 210, 255);
            if (r < 0.7f) return new Color32(255, 190, 70, 255);
            return new Color32(220, 90, 40, 255);
        }

        private static Color32 FlagPixel(int x, int y)
        {
            // 8x16, y = 0 is the bottom (pivot). Pole at x = 3, cloth to the right near the top.
            if (x == 3) return new Color32(70, 52, 36, 255);
            if (y >= 9 && y <= 14 && x >= 4)
            {
                int[] widths = { 2, 2, 3, 3, 4, 4 }; // y = 9..14
                if (x - 4 < widths[y - 9])
                {
                    return y == 14 ? new Color32(214, 84, 70, 255) : new Color32(186, 52, 44, 255);
                }
            }
            return Clear;
        }

        private static Color32 MarkerPixel(int x, int y)
        {
            // 10x8 dark mine disc with a red core, dev marker.
            float dx = x + 0.5f - 5f;
            float dy = y + 0.5f - 4f;
            if (dx * dx / 25f + dy * dy / 16f > 1f) return Clear;
            if (Mathf.Abs(dx) < 1.6f && Mathf.Abs(dy) < 1.6f) return new Color32(210, 44, 40, 255);
            return new Color32(36, 34, 38, 255);
        }

        private static bool GlyphAt(int digit, int gx, int gyFromBottom)
        {
            if (gx < 0 || gx > 4 || gyFromBottom < 0 || gyFromBottom > 6) return false;
            return Glyphs[digit][6 - gyFromBottom][gx] == '1';
        }

        private static Color32 DigitPixel(int digit, int x, int y)
        {
            // 8x10 canvas: white glyph (tinted at runtime) with a 1px dark outline.
            if (GlyphAt(digit, x - 1, y - 1)) return new Color32(255, 255, 255, 255);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dx != 0 || dy != 0) && GlyphAt(digit, x - 1 + dx, y - 1 + dy)) return new Color32(22, 16, 12, 255);
                }
            }
            return Clear;
        }

        // ------------------------------------------------------------------ helpers

        private static void SetObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(fieldName);
            if (p == null)
            {
                Debug.LogError("[M2] Missing serialized field '" + fieldName + "' on " + target.GetType().Name);
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static void EnsureFolder(string assetFolder)
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot(), assetFolder));
            AssetDatabase.Refresh();
        }
    }
}
