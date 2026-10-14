using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using MinefieldClerk.DevTools;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// M0 foundation tooling.
    ///   Minefield/M0/Run Setup         : project settings + test tiles + test scene
    ///   Minefield/M0/Check Pixel Setup : read-only diagnostic of the pixel-perfect setup
    /// Safe to run repeatedly: assets are reused, the test scene is rebuilt.
    /// </summary>
    public static class M0Setup
    {
        private const string TileDir = "Assets/Art/Tiles/Test";
        private const string ObjectsDir = "Assets/Art/Objects/Test";
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = "Assets/Scenes/M0_PixelTest.unity";

        // Test field layout, in isometric cells. Cell (x, y) steps along the two diagonal axes of the screen,
        // so this rectangle shows up on screen as a rotated rectangle (a long diamond).
        private const int MapWidth = 30;
        private const int MapHeight = 26;
        private const int PlotMinX = 5;
        private const int PlotMaxX = 24;   // 20 cells along the x axis
        private const int PlotMinY = 6;
        private const int PlotMaxY = 17;   // 12 cells along the y axis

        // ------------------------------------------------------------------ run setup

        [MenuItem("Minefield/M0/Run Setup")]
        public static void RunSetup()
        {
            EnsureFolder(TileDir);
            EnsureFolder(ObjectsDir);
            EnsureFolder(SceneDir);
            EnsureFolder("Assets/Scripts");

            ApplyProjectSettings();

            Sprite grassA = CreateTileSprite("grass_a", (x, y) => GrassPixel(x, y, 1));
            Sprite grassB = CreateTileSprite("grass_b", (x, y) => GrassPixel(x, y, 2));
            Sprite dirt = CreateTileSprite("dirt", DirtPixel);
            Sprite border = CreateTileSprite("border", BorderPixel);
            Sprite checker = CreateTileSprite("checker", CheckerPixel);

            // Isometric test props: bottom-center pivot comes from the importer (Assets/Art/Objects).
            Sprite post = CreateSprite(ObjectsDir, "post", 16, 32, PostPixel);
            Sprite walker = CreateSprite(ObjectsDir, "walker", 16, 24, WalkerPixel);

            Tile grassATile = CreateTileAsset("Tile_GrassA", grassA);
            Tile grassBTile = CreateTileAsset("Tile_GrassB", grassB);
            Tile dirtTile = CreateTileAsset("Tile_Dirt", dirt);
            Tile borderTile = CreateTileAsset("Tile_Border", border);
            Tile checkerTile = CreateTileAsset("Tile_Checker", checker);

            BuildTestScene(grassATile, grassBTile, dirtTile, borderTile, checkerTile, post, walker);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[M0] Setup done. Open " + ScenePath + ", press Play, and run Minefield/M0/Check Pixel Setup.");
        }

        // ------------------------------------------------------------------ project settings

        private static void ApplyProjectSettings()
        {
            // Anti-aliasing and anisotropic filtering off on every quality level.
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
            QualitySettings.SetQualityLevel(current, false);

            // URP asset: no HDR, no MSAA.
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.supportsHDR = false;
                urp.msaaSampleCount = 1;
                EditorUtility.SetDirty(urp);
            }
            else
            {
                Debug.LogWarning("[M0] No Universal Render Pipeline asset is active. Check Project Settings > Graphics.");
            }

            // Isometric: sort sprites by Y (lower on screen draws in front).
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);

            // Editor grid snapping = 1 / PPU, so objects move in whole art pixels.
            float snap = 1f / PixelConstants.PixelsPerUnit;
            EditorSnapSettings.move = new Vector3(snap, snap, snap);
        }

        // ------------------------------------------------------------------ test tiles

        private static Sprite CreateTileSprite(string name, System.Func<int, int, Color32> pixel)
        {
            // Ground tiles are 32x16 diamonds: everything outside the diamond is transparent.
            return CreateSprite(TileDir, name, PixelConstants.TileWidthPx, PixelConstants.TileHeightPx,
                (x, y) => InDiamond(x, y) ? pixel(x, y) : Clear);
        }

        // 2:1 pixel diamond for a 32x16 tile: row widths 4, 8, ... 32, 32, ... 8, 4 (y = 0 is the bottom row).
        private static bool InDiamond(int x, int y)
        {
            int half = PixelConstants.TileHeightPx / 2;
            int k = y < half ? y : PixelConstants.TileHeightPx - 1 - y;
            int width = 4 * (k + 1);
            int center = PixelConstants.TileWidthPx / 2;
            return x >= center - width / 2 && x < center + width / 2;
        }

        private static Sprite CreateSprite(string dir, string name, int width, int height, System.Func<int, int, Color32> pixel)
        {
            string assetPath = dir + "/" + name + ".png";
            string fullPath = Path.Combine(ProjectRoot(), assetPath);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, pixel(x, y));
                }
            }
            tex.Apply();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // A texture left in Multiple mode keeps an old slice rect and crops the new sprite. Force Single.
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Tile CreateTileAsset(string name, Sprite sprite)
        {
            string assetPath = TileDir + "/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
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

        private static Color32 Shade(int r, int g, int b, int delta)
        {
            return new Color32(
                (byte)Mathf.Clamp(r + delta, 0, 255),
                (byte)Mathf.Clamp(g + delta, 0, 255),
                (byte)Mathf.Clamp(b + delta, 0, 255),
                255);
        }

        private static Color32 GrassPixel(int x, int y, int seed)
        {
            int h = Hash(x, y, seed);
            int delta = (h & 15) - 8;
            if (h % 23 == 0)
            {
                delta += 24; // light blade
            }
            return Shade(104, 120, 56, delta);
        }

        private static Color32 DirtPixel(int x, int y)
        {
            int h = Hash(x, y, 7);
            int delta = (h & 15) - 8;
            if (h % 29 == 0)
            {
                delta -= 20; // pebble
            }
            return Shade(139, 106, 64, delta);
        }

        private static Color32 BorderPixel(int x, int y)
        {
            int h = Hash(x, y, 11);
            int delta = (h & 7) - 4;
            // Lighter upper half, darker lower half, so the diamond edges are easy to see.
            delta += y >= PixelConstants.TileHeightPx / 2 ? 12 : -14;
            return Shade(86, 66, 44, delta);
        }

        private static Color32 CheckerPixel(int x, int y)
        {
            // One-pixel checkerboard: any blur, scaling error or moire shows up immediately.
            bool on = ((x + y) & 1) == 0;
            return on ? new Color32(240, 232, 208, 255) : new Color32(24, 20, 16, 255);
        }

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private static Color32 PostPixel(int x, int y)
        {
            // 16x32 wooden post, y = 0 is the bottom (the sprite's pivot / feet).
            if (y >= 28 && y <= 29 && x >= 5 && x <= 10) return Shade(150, 112, 70, (Hash(x, y, 13) & 7) - 4);
            if (y <= 27 && x >= 6 && x <= 9)
            {
                int delta = (Hash(x, y, 17) & 7) - 4;
                if (x == 6) delta += 14;   // lit left edge
                if (x == 9) delta -= 16;   // shaded right edge
                return Shade(122, 88, 52, delta);
            }
            return Clear;
        }

        private static Color32 WalkerPixel(int x, int y)
        {
            // 16x24 placeholder figure in an olive coat and hat, y = 0 is the feet.
            if (y >= 20 && y <= 22 && x >= 5 && x <= 10) return Shade(60, 66, 40, (Hash(x, y, 19) & 7) - 4);   // hat
            if (y == 19 && x >= 4 && x <= 11) return Shade(52, 58, 34, 0);                                       // brim
            if (y >= 15 && y <= 18 && x >= 6 && x <= 9) return Shade(214, 170, 130, (Hash(x, y, 23) & 7) - 4);  // face
            if (y >= 6 && y <= 14 && x >= 4 && x <= 11) return Shade(98, 106, 62, (Hash(x, y, 29) & 11) - 6);   // coat
            if (y >= 8 && y <= 13 && (x == 3 || x == 12)) return Shade(78, 86, 48, 0);                           // arms
            if (y <= 5 && ((x >= 5 && x <= 7) || (x >= 8 && x <= 10))) return Shade(50, 44, 36, (Hash(x, y, 31) & 5) - 2); // legs
            return Clear;
        }

        // ------------------------------------------------------------------ test scene

        private static void BuildTestScene(Tile grassA, Tile grassB, Tile dirt, Tile border, Tile checker, Sprite post, Sprite walker)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera, centered on the tilemap.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Vector2 plotCenter = CellCenterWorld((PlotMinX + PlotMaxX) / 2, (PlotMinY + PlotMaxY) / 2);
            camGo.transform.position = new Vector3(plotCenter.x, plotCenter.y, -10f);

            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.orthographicSize = PixelConstants.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(18, 14, 10, 255);
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.allowDynamicResolution = false;
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);

            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;
            camData.antialiasing = AntialiasingMode.None;

            var pp = camGo.AddComponent<PixelPerfectCamera>();
            pp.assetsPPU = PixelConstants.PixelsPerUnit;
            pp.refResolutionX = PixelConstants.RefResolutionX;
            pp.refResolutionY = PixelConstants.RefResolutionY;
            pp.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            pp.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

            camGo.AddComponent<CameraPanTest>();

            // Global 2D light.
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            // Grid + tilemap.
            var gridGo = new GameObject("Grid");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(PixelConstants.CellWidth, PixelConstants.CellHeight, 1f);

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            var tilemap = groundGo.AddComponent<Tilemap>();
            var tilemapRenderer = groundGo.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = PixelConstants.SortingOrderGround;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                Material mat = GraphicsSettings.currentRenderPipeline.default2DMaterial;
                if (mat != null)
                {
                    tilemapRenderer.sharedMaterial = mat;
                }
            }

            FillTestField(tilemap, grassA, grassB, dirt, border, checker);

            // Isometric sorting test: three posts in a row along the cell x axis, plus two walkers passing the middle one.
            // One walker moves straight up/down the screen, the other along the diagonal cell axis.
            var propsGo = new GameObject("Props");
            int rowY = (PlotMinY + PlotMaxY) / 2;
            int midX = (PlotMinX + PlotMaxX) / 2;
            CreateSpriteObject("Post_Left", propsGo.transform, post, CellCenterWorld(midX - 3, rowY));
            CreateSpriteObject("Post_Middle", propsGo.transform, post, CellCenterWorld(midX, rowY));
            CreateSpriteObject("Post_Right", propsGo.transform, post, CellCenterWorld(midX + 3, rowY));

            GameObject walkerV = CreateSpriteObject("SortTestWalker_Vertical", propsGo.transform, walker, CellCenterWorld(midX, rowY));
            walkerV.AddComponent<SortTestWalker>().Configure(new Vector2(0f, 1f), 2.5f, 0.8f);

            GameObject walkerD = CreateSpriteObject("SortTestWalker_Diagonal", propsGo.transform, walker, CellCenterWorld(midX, rowY + 2));
            walkerD.AddComponent<SortTestWalker>().Configure(new Vector2(0.5f, -0.25f), 6f, 0.6f);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // World position of the center of isometric cell (x, y), for the grid set up in BuildTestScene.
        private static Vector2 CellCenterWorld(int x, int y)
        {
            float cw = PixelConstants.CellWidth;
            float ch = PixelConstants.CellHeight;
            return new Vector2((x - y) * 0.5f * cw, (x + y) * 0.5f * ch + ch * 0.5f);
        }

        private static GameObject CreateSpriteObject(string name, Transform parent, Sprite sprite, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = PixelConstants.SortingOrderDefault;
            // Sort by the sprite's pivot (its feet), not by its center, or tall sprites sort wrongly.
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            return go;
        }

        private static void FillTestField(Tilemap tilemap, Tile grassA, Tile grassB, Tile dirt, Tile border, Tile checker)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    TileBase tile;

                    bool inPlot = x >= PlotMinX && x <= PlotMaxX && y >= PlotMinY && y <= PlotMaxY;
                    bool onRing = x >= PlotMinX - 1 && x <= PlotMaxX + 1 && y >= PlotMinY - 1 && y <= PlotMaxY + 1 && !inPlot;
                    bool inChecker = x >= 26 && x <= 27 && y >= 22 && y <= 23;

                    if (inChecker)
                    {
                        tile = checker;
                    }
                    else if (onRing)
                    {
                        tile = border;
                    }
                    else if (inPlot)
                    {
                        // Rectangular playable plot: grass with dirt patches.
                        int h = Hash(x, y, 3);
                        tile = (h % 7 == 0) ? dirt : ((h & 1) == 0 ? grassA : grassB);
                    }
                    else
                    {
                        tile = (Hash(x, y, 5) & 1) == 0 ? grassA : grassB;
                    }

                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        // ------------------------------------------------------------------ diagnostic

        [MenuItem("Minefield/M0/Check Pixel Setup")]
        public static void CheckPixelSetup()
        {
            int problems = 0;

            // URP asset
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
            {
                problems += Report(false, "No URP asset is active.");
            }
            else
            {
                problems += Report(!urp.supportsHDR, "URP asset: HDR is off.");
                problems += Report(urp.msaaSampleCount == 1, "URP asset: MSAA is off.");
            }

            problems += Report(GraphicsSettings.transparencySortMode == TransparencySortMode.CustomAxis && GraphicsSettings.transparencySortAxis == new Vector3(0f, 1f, 0f),
                "Project Graphics: transparency sort is custom axis (0, 1, 0).");

            // Quality
            problems += Report(QualitySettings.antiAliasing == 0, "Quality (current level): anti-aliasing is 0.");
            problems += Report(QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable, "Quality (current level): anisotropic filtering is disabled.");

            // Scene camera
            var pp = Object.FindAnyObjectByType<PixelPerfectCamera>();
            if (pp == null)
            {
                problems += Report(false, "Open scene: no URP PixelPerfectCamera found (open " + ScenePath + ").");
            }
            else
            {
                var cam = pp.GetComponent<Camera>();
                problems += Report(cam.orthographic, "Camera: orthographic.");
                problems += Report(!cam.allowHDR && !cam.allowMSAA && !cam.allowDynamicResolution, "Camera: HDR, MSAA and dynamic resolution are off.");
                problems += Report(pp.assetsPPU == PixelConstants.PixelsPerUnit, "PixelPerfectCamera: assets PPU is " + PixelConstants.PixelsPerUnit + ".");
                problems += Report(pp.refResolutionX == PixelConstants.RefResolutionX && pp.refResolutionY == PixelConstants.RefResolutionY,
                    "PixelPerfectCamera: reference resolution is " + PixelConstants.RefResolutionX + "x" + PixelConstants.RefResolutionY + ".");
                problems += Report(pp.gridSnapping == PixelPerfectCamera.GridSnapping.PixelSnapping, "PixelPerfectCamera: pixel snapping.");
                problems += Report(pp.cropFrame == PixelPerfectCamera.CropFrame.Windowbox, "PixelPerfectCamera: windowbox crop.");
                problems += Report(cam.transparencySortMode == TransparencySortMode.CustomAxis && cam.transparencySortAxis == new Vector3(0f, 1f, 0f),
                    "Camera: transparency sort is custom axis (0, 1, 0), for isometric Y-sorting.");
                var gridInScene = Object.FindAnyObjectByType<Grid>();
                problems += Report(gridInScene != null && gridInScene.cellLayout == GridLayout.CellLayout.Isometric
                    && Mathf.Approximately(gridInScene.cellSize.x, PixelConstants.CellWidth)
                    && Mathf.Approximately(gridInScene.cellSize.y, PixelConstants.CellHeight),
                    "Grid: isometric layout, cell size " + PixelConstants.CellWidth + " x " + PixelConstants.CellHeight + ".");
                if (Application.isPlaying)
                {
                    Debug.Log("[M0 check] Current pixel ratio (screen scale): " + pp.pixelRatio + "x");
                }
            }

            // Textures under Assets/Art
            if (AssetDatabase.IsValidFolder("Assets/Art"))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool ok = importer.filterMode == FilterMode.Point
                        && !importer.mipmapEnabled
                        && importer.textureCompression == TextureImporterCompression.Uncompressed
                        && Mathf.Approximately(importer.spritePixelsPerUnit, PixelConstants.PixelsPerUnit);
                    problems += Report(ok, "Texture import: " + path);
                }
            }

            Debug.Log(problems == 0
                ? "[M0 check] All checks passed."
                : "[M0 check] " + problems + " problem(s). See the warnings above.");
        }

        private static int Report(bool ok, string message)
        {
            if (ok)
            {
                Debug.Log("[M0 check] OK: " + message);
                return 0;
            }
            Debug.LogWarning("[M0 check] PROBLEM: " + message);
            return 1;
        }

        // ------------------------------------------------------------------ helpers

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
