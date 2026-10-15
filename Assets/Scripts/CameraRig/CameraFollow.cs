using UnityEngine;

namespace MinefieldClerk.CameraRig
{
    /// <summary>
    /// Smoothly follows a target. The smoothed position is kept as a float and the camera is placed on whole
    /// art pixels, so the ground does not shimmer while scrolling.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.12f;
        [Tooltip("Look slightly above the feet, at the middle of the body")]
        [SerializeField] private float heightOffset = 0.35f;

        private Vector2 smoothed;
        private Vector2 velocity;

        private void Start()
        {
            smoothed = Goal();
            Place();
        }

        private void LateUpdate()
        {
            smoothed = Vector2.SmoothDamp(smoothed, Goal(), ref velocity, smoothTime);
            Place();
        }

        private Vector2 Goal()
        {
            return new Vector2(target.position.x, target.position.y + heightOffset);
        }

        private void Place()
        {
            transform.position = IsoMath.SnapToPixel(new Vector3(smoothed.x, smoothed.y, transform.position.z));
        }
    }
}
