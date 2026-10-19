using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// The screen-space canvas that holds the UI windows (briefing book, debrief). Created on first use together with an
    /// EventSystem. The windows themselves are prefabs in Resources/UI/Windows that you edit in Unity.
    /// </summary>
    public static class UiCanvas
    {
        private const string CanvasName = "UiCanvas", EventSystemName = "EventSystem";
        private const int SortingOrder = 100;
        private const float ReferenceWidth = 1024f, ReferenceHeight = 660f;   // every window fits inside this; the canvas scales it to the screen

        private static Canvas canvas;

        public static Transform Root
        {
            get
            {
                if (canvas == null) Build();
                return canvas.transform;
            }
        }

        private static void Build()
        {
            var go = new GameObject(CanvasName);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject(EventSystemName);
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        /// <summary>Instantiates a window prefab from Resources under the canvas, or returns null (and logs) when it is missing.</summary>
        public static T Spawn<T>(string resourcePath) where T : Component
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError("[UI] Missing window prefab Resources/" + resourcePath + ". Run the menu Minefield > UI > Build Windows.");
                return null;
            }
            GameObject instance = Object.Instantiate(prefab, Root, false);
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
