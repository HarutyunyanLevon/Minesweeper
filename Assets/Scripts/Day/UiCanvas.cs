using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// The screen-space canvases that hold the UI prefabs, created on first use together with an EventSystem.
    /// Hud: the in-shift HUD, toast and work bar (designed for 1920 x 1080). Windows: briefing book, debrief and expenses (1024 x 660).
    /// Every prefab lives in Resources/UI and is edited in Unity; see Assets/Docs/ui-windows.md.
    /// </summary>
    public static class UiCanvas
    {
        public enum Layer { Hud, Windows }

        private const string EventSystemName = "EventSystem";
        private const int HudOrder = 50, WindowsOrder = 100;   // windows draw over the HUD
        private static readonly Vector2 HudReference = new Vector2(1920f, 1080f), WindowsReference = new Vector2(1024f, 660f);

        private static Canvas hud, windows;

        public static Transform Root(Layer layer)
        {
            if (layer == Layer.Hud)
            {
                if (hud == null) hud = Build("HudCanvas", HudOrder, HudReference);
                return hud.transform;
            }
            if (windows == null) windows = Build("WindowsCanvas", WindowsOrder, WindowsReference);
            return windows.transform;
        }

        private static Canvas Build(string name, int order, Vector2 reference)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject(EventSystemName);
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            return canvas;
        }

        /// <summary>Instantiates a prefab from Resources under a canvas, or returns null (and logs) when it is missing.</summary>
        public static T Spawn<T>(string resourcePath, Layer layer = Layer.Windows) where T : Component
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError("[UI] Missing prefab Resources/" + resourcePath + ". Run the menu Minefield > UI > Build Prefabs.");
                return null;
            }
            GameObject instance = Object.Instantiate(prefab, Root(layer), false);
            instance.name = prefab.name;
            return instance.GetComponent<T>();
        }

        /// <summary>Drops the UI selection so Enter does not also press the button that was just clicked.</summary>
        public static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
