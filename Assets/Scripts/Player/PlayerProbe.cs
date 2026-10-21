using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.DevTools;
using MinefieldClerk.Day;
using MinefieldClerk.Field;
using MinefieldClerk.Sound;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Probing and flagging the cell in front of the player.
    /// Space / gamepad A / right click probes, F / gamepad X / middle click flags.
    /// Walking onto an unprobed, unflagged mine arms it exactly like a probe does, once enough of the body is on the mine's cell
    /// (StepShare of the footprint; brushing a corner does nothing).
    /// A burning mine explodes after 1.5-3 s unless it is a dud (10%).
    /// A mine the player stepped on explodes at once when the body leaves its tile completely (shuffling on its edge is fine).
    /// A mine armed by probing a different cell explodes at once when the player moves at all.
    /// Also adds the MineWork component (hold E: defuse and collect on days with the collect-mines rule).
    /// </summary>
    public class PlayerProbe : MonoBehaviour
    {
        private const float FootprintCells = 0.5f;   // side of the body's footprint, in cells
        private const float StepShare = 0.15f;        // share of the footprint that must lie on a mine's cell to set it off
        private const float FlagWaveDelaySeconds = 0.12f;   // the flag cloth sounds this long after the stake

        [SerializeField] private PlayerController player;
        [SerializeField] private MineField mines;
        [SerializeField] private PlayerHealth health;

        private DeminerSprites sprites;
        private MineWork work;

        private readonly HashSet<Vector3Int> underFoot = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, Vector3> remote = new Dictionary<Vector3Int, Vector3>();
        private readonly List<Vector3Int> scratch = new List<Vector3Int>();
        private bool stepping;   // a step is arming a mine right now

        private void OnEnable()
        {
            if (sprites == null) sprites = player.GetComponent<DeminerSprites>();
            if (work == null)
            {
                work = GetComponent<MineWork>();
                if (work == null) work = gameObject.AddComponent<MineWork>();
                work.Setup(player, mines, health);
            }
            mines.MineArmed += OnArmed;
            mines.MineTriggered += OnTriggered;
            mines.MineDud += OnResolved;
            mines.Regenerated += OnRegenerated;
        }

        private void OnDisable()
        {
            mines.MineArmed -= OnArmed;
            mines.MineTriggered -= OnTriggered;
            mines.MineDud -= OnResolved;
            mines.Regenerated -= OnRegenerated;
        }

        /// <summary>The metal stake rings first, then the cloth catches the wind (the second layer is optional).</summary>
        private static void PlayFlagSound()
        {
            Sfx.Play(SfxEvent.FlagPlant);
            if (Sfx.Has(SfxEvent.FlagWave)) Sfx.PlayDelayed(SfxEvent.FlagWave, FlagWaveDelaySeconds);
        }

        private void OnArmed(Vector3Int cell, float fuse)
        {
            if (stepping || cell.Flat() == player.CurrentCell.Flat()) underFoot.Add(cell.Flat());
            else remote[cell.Flat()] = player.transform.position;
        }

        private void OnResolved(Vector3Int cell)
        {
            underFoot.Remove(cell.Flat());
            remote.Remove(cell.Flat());
        }

        private void OnRegenerated()
        {
            underFoot.Clear();
            remote.Clear();
        }

        private void OnTriggered(Vector3Int cell)
        {
            OnResolved(cell);
            health.Hit();
            Sfx.Play(health.Dead ? SfxEvent.PlayerDeath : SfxEvent.PlayerHurt);
        }

        /// <summary>Share (0..1) of the body footprint, centred at <paramref name="body"/>, that lies on a cell.</summary>
        private static float Overlap(Vector2 body, Vector3Int cell)
        {
            float half = FootprintCells * Num.Half;
            float x = Mathf.Min(body.x + half, cell.x + 1f) - Mathf.Max(body.x - half, cell.x);
            float y = Mathf.Min(body.y + half, cell.y + 1f) - Mathf.Max(body.y - half, cell.y);
            if (x <= 0f || y <= 0f) return 0f;
            return x * y / (FootprintCells * FootprintCells);
        }

        private void ArmSteppedMines(Vector2 body)
        {
            float half = FootprintCells * Num.Half;
            for (int x = Mathf.FloorToInt(body.x - half); x <= Mathf.FloorToInt(body.x + half); x++)
            {
                for (int y = Mathf.FloorToInt(body.y - half); y <= Mathf.FloorToInt(body.y + half); y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (Overlap(body, cell) < StepShare) continue;
                    stepping = true;
                    mines.StepOn(cell);
                    stepping = false;
                }
            }
        }

        private void Update()
        {
            // Walking onto a mine arms it like a probe would (no number appears on safe cells), once 15% of the body is on it.
            Vector2 body = player.CellPosition;
            ArmSteppedMines(body);

            // Leaving the tile of a burning mine detonates it immediately.
            if (underFoot.Count > 0)
            {
                scratch.Clear();
                foreach (Vector3Int c in underFoot)
                {
                    if (Overlap(body, c) <= 0f) scratch.Add(c);
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
                    if ((pos - pair.Value).sqrMagnitude > Num.Epsilon) scratch.Add(pair.Key);
                }
                foreach (Vector3Int c in scratch) mines.Detonate(c);
            }

            DayManager day = DayManager.Instance;
            if (health.Dead || (day != null && !day.CanAct) || work.Busy) return;

            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            bool probe = (kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            bool flag = (kb != null && kb.fKey.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame);

            Vector3Int cell = player.TargetCell;

            // Mouse: right click probes, middle click flags, the hovered neighbour cell.
            Mouse mouse = Mouse.current;
            if (mouse != null && player.HoverValid && !DevHud.IsOverUi(mouse.position.ReadValue()))
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
                if (day != null && !day.CanProbe)
                {
                    day.Notify("Detector battery is dead. Flag from memory.");
                }
                else
                {
                    if (sprites != null) sprites.PlayAction("probe");
                    ProbeOutcome result = mines.Probe(cell);
                    if (result == ProbeOutcome.Safe || result == ProbeOutcome.Armed)
                    {
                        Sfx.Play(SfxEvent.DigHole);
                        if (day != null) day.RegisterProbe();
                    }
                }
            }
            else if (flag)
            {
                player.FaceCell(cell);
                if (sprites != null) sprites.PlayAction("flag");
                if (mines.ToggleFlag(cell)) PlayFlagSound();
            }
        }
    }
}
