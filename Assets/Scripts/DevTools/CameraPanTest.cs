using UnityEngine;

namespace MinefieldClerk.DevTools
{
    /// <summary>
    /// Slowly pans the camera so you can check for tile shimmer, seams and blur while moving.
    /// Dev-only: remove or disable once the player-follow camera exists (M1).
    /// </summary>
    public class CameraPanTest : MonoBehaviour
    {
        [SerializeField] private Vector2 amplitude = new Vector2(4f, 2f);
        [SerializeField] private float speed = 0.35f;

        private Vector3 origin;

        private void Awake()
        {
            origin = transform.position;
        }

        private void LateUpdate()
        {
            float t = Time.time * speed;
            transform.position = origin + new Vector3(Mathf.Sin(t) * amplitude.x, Mathf.Sin(t * 0.7f) * amplitude.y, 0f);
        }
    }
}
