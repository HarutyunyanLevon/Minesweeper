using UnityEngine;
using UnityEngine.UI;

namespace MinefieldClerk.Day
{
    /// <summary>
    /// The progress bar above the deminer's head while defusing or collecting, a prefab you edit in Unity
    /// (Resources/UI/Hud/WorkBar.prefab). Its pivot is the point that follows the deminer.
    /// </summary>
    public class WorkBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;   // stretches inside the bar; the script sets its right edge
        [SerializeField] private Text label;

        private RectTransform canvasRect;
        private RectTransform rect;

        private void Awake()
        {
            rect = (RectTransform)transform;
            canvasRect = (RectTransform)transform.parent;
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>Puts the bar at a world position and shows the progress (0..1) with a caption.</summary>
        public void Show(Camera cam, Vector3 worldPosition, float progress01, string text)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            rect.anchoredPosition = local;
            fill.anchorMax = new Vector2(Mathf.Clamp01(progress01), 1f);
            label.text = text;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
