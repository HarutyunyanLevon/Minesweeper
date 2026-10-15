using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Probing and flagging the cell in front of the player.
    /// Space / gamepad A / right click probes, F / gamepad X / middle click flags. A probed mine tile can set the mine off.
    /// </summary>
    public class PlayerProbe : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private MineField mines;
        [SerializeField] private PlayerHealth health;

        private void Update()
        {
            if (health.Dead) return;

            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            bool probe = (kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            bool flag = (kb != null && kb.fKey.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame);

            Vector3Int cell = player.TargetCell;

            // Mouse: right click probes, middle click flags, the hovered neighbour cell.
            Mouse mouse = Mouse.current;
            if (mouse != null && player.HoverValid && !MinefieldClerk.DevTools.DevHud.IsOverUi(mouse.position.ReadValue()))
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    probe = true;
                    cell = player.HoverCell;
                }
                else if (mouse.middleButton.wasPressedThisFrame)
                {
                    flag = true;
                    cell = player.HoverCell;
                }
            }

            if (probe)
            {
                player.FaceCell(cell);
                if (mines.Probe(cell) == ProbeOutcome.Mine)
                {
                    health.Hit();
                }
            }
            else if (flag)
            {
                player.FaceCell(cell);
                mines.ToggleFlag(cell);
            }
        }
    }
}
