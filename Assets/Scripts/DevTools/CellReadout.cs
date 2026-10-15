using UnityEngine;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

namespace MinefieldClerk.DevTools
{
    /// <summary>Dev-only on-screen readout of the player's cell. Remove before release.</summary>
    public class CellReadout : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField] private PlayerController player;

        private void OnGUI()
        {
            Vector3Int c = player.CurrentCell;
            GUI.Label(new Rect(10, 10, 600, 24),
                "Cell " + c.x + ", " + c.y + (field.IsWalkable(c) ? "" : "  (outside field)") + "   Move: WASD / arrows / stick");
        }
    }
}
