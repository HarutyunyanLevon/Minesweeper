using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MinefieldClerk.EditorTools
{
    /// <summary>Editor helpers that create the standard pixel-perfect camera, light and isometric grid.</summary>
    public static class SceneKit
    {
        public static Camera CreateCamera(Vector3 position)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = position;

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

            return cam;
        }

        public static void CreateGlobalLight()
        {
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
        }

        public static Tilemap CreateIsoGround(out Grid grid)
        {
            var gridGo = new GameObject("Grid");
            grid = gridGo.AddComponent<Grid>();
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
            return tilemap;
        }
    }
}
