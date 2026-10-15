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
    /// M1: builds Assets/Scenes/M1_Field.unity, a walkable isometric field with placeholder art.
    /// Needs the M0 test tiles and sprites (run Minefield/M0/Run Setup first).
    /// Menu: Minefield/M1/Build Field Scene
    /// </summary>
    public static class M1Setup
    {
        private const string TileDir = "Assets/Art/Tiles/Test";
        private const string ObjectsDir = "Assets/Art/Objects/Test";
        private const string ScenePath = "Assets/Scenes/M1_Field.unity";
        private const string CursorPath = TileDir + "/cursor.png";

        [MenuItem("Minefield/M1/Build Field Scene")]
        public static void BuildFieldScene()
        {
            var grassA = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_GrassA.asset");
            var grassB = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_GrassB.asset");
            var dirt = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_Dirt.asset");
            var border = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/Tile_Border.asset");
            var walker = AssetDatabase.LoadAssetAtPath<Sprite>(ObjectsDir + "/walker.png");
            if (grassA == null || grassB == null || dirt == null || border == null || walker == null)
            {
                Debug.LogError("[M1] Missing M0 test assets. Run Minefield/M0/Run Setup first.");
                return;
            }

            Directory.CreateDirectory(Path.Combine(ProjectRoot(), "Assets/Scenes"));
            Sprite cursor = CreateCursorSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera cam = SceneKit.CreateCamera(new Vector3(0f, 0f, -10f));
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            SceneKit.CreateGlobalLight();

            Tilemap ground = SceneKit.CreateIsoGround(out Grid grid);

            // Field.
            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(grid.transform, false);
            var field = fieldGo.AddComponent<FieldBuilder>();
            field.Configure(ground, new[] { grassA, grassB }, dirt, border);
            EditorUtility.SetDirty(field);
            field.Build();
            Debug.Log("[M1] Field built: " + ground.GetUsedTilesCount() + " tiles. grassA sprite=" + (grassA.sprite != null ? grassA.sprite.name + " " + grassA.sprite.rect : "NULL")
                + ", grassB sprite=" + (grassB.sprite != null ? grassB.sprite.name + " " + grassB.sprite.rect : "NULL")
                + ", dirt sprite=" + (dirt.sprite != null ? dirt.sprite.name + " " + dirt.sprite.rect : "NULL")
                + ", border sprite=" + (border.sprite != null ? border.sprite.name + " " + border.sprite.rect : "NULL")
                + ", tile at (3,3)=" + (ground.GetTile(new Vector3Int(3, 3, 0)) != null ? ground.GetTile(new Vector3Int(3, 3, 0)).name : "NULL")
                + ", tile at (-1,-1)=" + (ground.GetTile(new Vector3Int(-1, -1, 0)) != null ? ground.GetTile(new Vector3Int(-1, -1, 0)).name : "NULL"));

            // Player.
            var playerGo = new GameObject("Player");
            var body = playerGo.AddComponent<SpriteRenderer>();
            body.sprite = walker;
            body.sortingOrder = PixelConstants.SortingOrderDefault;
            body.spriteSortPoint = SpriteSortPoint.Pivot;
            var player = playerGo.AddComponent<PlayerController>();
            SetObject(player, "field", field);
            SetObject(player, "body", body);
            playerGo.transform.position = field.CellToWorld(field.StartCell);

            // Cell cursor, drawn just above the ground and below everything standing on it.
            var cursorGo = new GameObject("CellCursor");
            var cursorSr = cursorGo.AddComponent<SpriteRenderer>();
            cursorSr.sprite = cursor;
            cursorSr.sortingOrder = PixelConstants.SortingOrderGround + 1;
            var cellCursor = cursorGo.AddComponent<CellCursor>();
            SetObject(cellCursor, "field", field);
            SetObject(cellCursor, "player", player);

            SetObject(follow, "target", playerGo.transform);
            cam.transform.position = new Vector3(playerGo.transform.position.x, playerGo.transform.position.y + 0.35f, -10f);

            // Dev readout.
            var readoutGo = new GameObject("DevReadout");
            var readout = readoutGo.AddComponent<CellReadout>();
            SetObject(readout, "field", field);
            SetObject(readout, "player", player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[M1] Built " + ScenePath + ". Press Play and walk with WASD or the arrow keys.");
        }

        // ------------------------------------------------------------------ cursor sprite

        private static Sprite CreateCursorSprite()
        {
            const int w = PixelConstants.TileWidthPx;
            const int h = PixelConstants.TileHeightPx;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (InDiamond(x, y))
                    {
                        bool edge = !InDiamond(x - 1, y) || !InDiamond(x + 1, y) || !InDiamond(x, y - 1) || !InDiamond(x, y + 1);
                        c = edge ? new Color32(255, 240, 200, 230) : new Color32(255, 240, 200, 28);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(Path.Combine(ProjectRoot(), CursorPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(CursorPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(CursorPath) as TextureImporter;
            if (importer != null && importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(CursorPath);
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

        // ------------------------------------------------------------------ serialized field helpers

        private static void SetObject(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError("[M1] Missing serialized field '" + field + "' on " + target.GetType().Name);
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError("[M1] Missing serialized field '" + field + "' on " + target.GetType().Name);
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }
    }
}
