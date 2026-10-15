using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Field
{
    public enum ProbeOutcome
    {
        Invalid,   // outside the field
        Blocked,   // flagged, already probed or already exploded
        Safe,      // a number was stamped on the tile
        Mine       // the probe set off a mine
    }

    /// <summary>
    /// Hidden mines, probe results and flags for one field. Pure game state: all visuals live in MineFieldView.
    /// Walking never triggers mines; only probing does. A radar number is the count of mines within `radius`
    /// cells (8 neighbours for radius 1) and is stamped only on tiles the player probed.
    /// </summary>
    public class MineField : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField, Range(0f, 0.5f)] private float density = 0.14f;
        [Tooltip("The first rows next to the entrance never hold mines")]
        [SerializeField] private int safeRows = 2;
        [SerializeField] private int radius = 1;
        [Tooltip("-1 = new random layout every time")]
        [SerializeField] private int seed = -1;

        private bool[,] mines = new bool[0, 0];
        private readonly Dictionary<Vector3Int, int> probed = new Dictionary<Vector3Int, int>();
        private readonly HashSet<Vector3Int> flags = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> exploded = new HashSet<Vector3Int>();

        public event Action Regenerated;
        public event Action<Vector3Int, int> Probed;
        public event Action<Vector3Int, bool> FlagChanged;
        public event Action<Vector3Int> MineTriggered;

        public int Radius => radius;
        public int MineCount { get; private set; }
        public int ProbedCount => probed.Count;
        public int FlagCount => flags.Count;
        public int ExplodedCount => exploded.Count;

        public void Configure(FieldBuilder fieldBuilder)
        {
            field = fieldBuilder;
        }

        public void Generate()
        {
            mines = new bool[field.Width, field.Height];
            probed.Clear();
            flags.Clear();
            exploded.Clear();
            MineCount = 0;

            var rng = seed >= 0 ? new System.Random(seed) : new System.Random();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = safeRows; y < field.Height; y++)
                {
                    if (rng.NextDouble() < density)
                    {
                        mines[x, y] = true;
                        MineCount++;
                    }
                }
            }

            Regenerated?.Invoke();
        }

        private static Vector3Int Key(Vector3Int cell)
        {
            return new Vector3Int(cell.x, cell.y, 0);
        }

        public bool IsMine(Vector3Int cell)
        {
            return field.IsWalkable(cell) && mines[cell.x, cell.y];
        }

        public bool IsFlagged(Vector3Int cell) { return flags.Contains(Key(cell)); }
        public bool IsProbed(Vector3Int cell) { return probed.ContainsKey(Key(cell)); }
        public bool IsExploded(Vector3Int cell) { return exploded.Contains(Key(cell)); }

        public int CountAround(Vector3Int cell)
        {
            int count = 0;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (IsMine(new Vector3Int(cell.x + dx, cell.y + dy, 0))) count++;
                }
            }
            return count;
        }

        public ProbeOutcome Probe(Vector3Int cell)
        {
            cell = Key(cell);
            if (!field.IsWalkable(cell)) return ProbeOutcome.Invalid;
            if (flags.Contains(cell) || probed.ContainsKey(cell) || exploded.Contains(cell)) return ProbeOutcome.Blocked;

            if (mines[cell.x, cell.y])
            {
                exploded.Add(cell);
                MineTriggered?.Invoke(cell);
                return ProbeOutcome.Mine;
            }

            int number = CountAround(cell);
            probed[cell] = number;
            Probed?.Invoke(cell, number);
            return ProbeOutcome.Safe;
        }

        /// <summary>Places or removes a flag. Returns false when the tile cannot hold a flag.</summary>
        public bool ToggleFlag(Vector3Int cell)
        {
            cell = Key(cell);
            if (!field.IsWalkable(cell) || probed.ContainsKey(cell) || exploded.Contains(cell)) return false;

            if (flags.Remove(cell))
            {
                FlagChanged?.Invoke(cell, false);
            }
            else
            {
                flags.Add(cell);
                FlagChanged?.Invoke(cell, true);
            }
            return true;
        }

        /// <summary>Dev only: every mine that has not exploded yet.</summary>
        public IEnumerable<Vector3Int> LiveMineCells()
        {
            for (int x = 0; x < mines.GetLength(0); x++)
            {
                for (int y = 0; y < mines.GetLength(1); y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (mines[x, y] && !exploded.Contains(cell)) yield return cell;
                }
            }
        }
    }
}
