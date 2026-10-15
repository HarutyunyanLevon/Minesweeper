using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Probing and flagging the cell in front of the player.
    /// Space / gamepad A / right click probes, F / gamepad X / middle click flags.
    /// Walking onto an unprobed, unflagged mine arms it exactly like a probe does.
    /// A burning mine explodes after 1.5-3 s unless it is a dud (10%).
    /// A mine under the player's feet explodes at once when the player leaves its tile (moving inside the tile is fine).
    /// A mine armed by probing a different cell explodes at once when the player moves at all.
    /// </summary>
    public class PlayerProbe : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private MineField mines;
        [SerializeField] private PlayerHealth health;

        private readonly HashSet<Vector3Int> underFoot = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, Vector3> remote = new Dictionary<Vector3Int, Vector3>();
        private readonly List<Vector3Int> scratch = new List<Vector3Int>();
        private Vector3Int lastCell;
        private bool haveLastCell;

        private void OnEnable()
        {
            mines.MineArmed += OnArmed;
            mines.MineTriggered += OnResolved;
            mines.MineDud += OnResolved;
            mines.MineTriggered += OnTriggered;
            mines.Regenerated += OnRegenerated;
        }

        private void OnDisable()
        {
            mines.MineArmed -= OnArmed;
            mines.MineTriggered -= OnResolved;
            mines.MineDud -= OnResolved;
            mines.MineTriggered -= OnTriggered;
            mines.Regenerated -= OnRegenerated;
        }

        private void OnArmed(Vector3Int cell, float fuse)
        {
            if (cell.x == player.CurrentCell.x && cell.y == player.CurrentCell.y)
            {
                underFoot.Add(new Vector3Int(cell.x, cell.y, 0));
            }
            else
            {
                remote[new Vector3Int(cell.x, cell.y, 0)] = player.transform.position;
            }
        }

        private void OnResolved(Vector3Int cell)
        {
            Vector3Int key = new Vector3Int(cell.x, cell.y, 0);
            underFoot.Remove(key);
            remote.Remove(key);
        }

        private void OnRegenerated()
        {
            underFoot.Clear();
            remote.Clear();
            haveLastCell = false;
        }

        private void OnTriggered(Vector3Int cell)
        {
            health.Hit();
        }

        private void Update()
        {
            // Walking onto a mine arms it like a probe would (no number appears on safe cells).
            Vector3Int now = player.CurrentCell;
            if (!haveLastCell)
            {
                lastCell = now;
                haveLastCell = true;
            }
            else if (now != lastCell)
            {
                lastCell = now;
                mines.StepOn(now);
            }

            // Leaving the tile of a burning mine detonates it immediately.
            if (underFoot.Count > 0)
            {
                scratch.Clear();
                foreach (Vector3Int c in underFoot)
                {
                    if (c.x != now.x || c.y != now.y) scratch.Add(c);
                }
                foreach (Vector3Int c in scratch) mines.Detonate(c);
            }

            // A mine armed at a distance explodes as soon as the player moves.
            if (remote.Count > 0)
            {
                scratch.Clear();
                Vector3 pos = player.transform.position;
                foreach (KeyValuePair<Vector3Int, Vector3> pair in remote)
                {
                    if ((pos - pair.Value).sqrMagnitude > 0.0001f) scratch.Add(pair.Key);
                }
                foreach (Vector3Int c in scratch) mines.Detonate(c);
            }

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
                mines.Probe(cell);
            }
            else if (flag)
            {
                player.FaceCell(cell);
                mines.ToggleFlag(cell);
            }
        }
    }
}
