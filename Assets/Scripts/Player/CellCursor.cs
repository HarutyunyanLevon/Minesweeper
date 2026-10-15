using UnityEngine;
using MinefieldClerk.Field;

namespace MinefieldClerk.Player
{
    /// <summary>Highlights the target cell: the hovered neighbour while the mouse is in use, else the cell the player faces.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellCursor : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField] private PlayerController player;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            Vector3Int cell = player.TargetCell;
            bool inside = field.IsWalkable(cell);
            sr.enabled = inside;
            if (inside)
            {
                transform.position = IsoMath.SnapToPixel(field.CellToWorld(cell));
            }
        }
    }
}
