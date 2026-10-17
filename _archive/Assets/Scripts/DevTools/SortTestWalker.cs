using UnityEngine;

namespace MinefieldClerk.DevTools
{
    /// <summary>
    /// Moves back and forth along a direction past a post, so you can check isometric sorting:
    /// when the walker is higher on screen than the post's base it must be hidden behind it, lower it must be in front.
    /// Positions are rounded to whole art pixels. Dev-only.
    /// </summary>
    public class SortTestWalker : MonoBehaviour
    {
        [SerializeField] private Vector2 direction = Vector2.up;
        [SerializeField] private float amplitude = 2.5f;
        [SerializeField] private float speed = 0.8f;

        private Vector3 origin;

        public void Configure(Vector2 newDirection, float newAmplitude, float newSpeed)
        {
            direction = newDirection;
            amplitude = newAmplitude;
            speed = newSpeed;
        }

        private void Awake()
        {
            origin = transform.position;
        }

        private void Update()
        {
            Vector2 offset = direction * (Mathf.Sin(Time.time * speed) * amplitude);
            float ppu = PixelConstants.PixelsPerUnit;
            transform.position = new Vector3(
                Mathf.Round((origin.x + offset.x) * ppu) / ppu,
                Mathf.Round((origin.y + offset.y) * ppu) / ppu,
                origin.z);
        }
    }
}
