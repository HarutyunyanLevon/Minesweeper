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
        Armed      // the probe found a mine: its fuse is now burning
    }

    /// <summary>
    /// Hidden mines, probe results and flags for one field. Pure game state: all visuals live in MineFieldView.
    /// Probing a mine arms it: the mine is shown, then after a random fuse (fuseMin..fuseMax seconds) it explodes,
    /// unless it is a dud (dudChance). If the player moves while a fuse burns, the mine always explodes at once.
    /// The first probe of a field is always safe: it and its 8 neighbours are cleared of mines (they are moved elsewhere).
    /// Walking never triggers mines; only probing does. A radar number is the count of mines within `radius`
    /// cells (8 neighbours for radius 1) and is stamped only on tiles the player probed.
    /// </summary>
    public class MineField : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField, Range(0f, 0.5f)] private float density = 0.14f;
        [Tooltip("The first rows next to the entrance never hold mines")]
        [SerializeField] private int safeRows = 2;
        [Tooltip("No mines within this many cells of the start cell: 1 = a 3x3 empty square, 2 = also the ring around it, so those cells are empty or numbers")]
        [SerializeField] private int startSafeRadius = 2;
        [SerializeField] private int radius = 1;
        [Tooltip("-1 = new random layout every time")]
        [SerializeField] private int seed = -1;
        [Header("Fuse")]
        [SerializeField] private float fuseMin = 1.5f;
        [SerializeField] private float fuseMax = 3f;
        [Tooltip("Chance that a probed mine never explodes (only if the player stands still)")]
        [SerializeField, Range(0f, 1f)] private float dudChance = 0.1f;

        private bool[,] mines = new bool[0, 0];
        private readonly Dictionary<Vector3Int, int> probed = new Dictionary<Vector3Int, int>();
        private readonly HashSet<Vector3Int> flags = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> exploded = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> duds = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, Fuse> armed = new Dictionary<Vector3Int, Fuse>();
        private readonly List<Vector3Int> due = new List<Vector3Int>();
        private System.Random rng = new System.Random();
        private bool firstProbeDone;

        private class Fuse
        {
            public float TimeLeft;
            public bool Dud;
        }

        public event Action Regenerated;
        public event Action<Vector3Int, int> Probed;
        public event Action<Vector3Int, bool> FlagChanged;
        public event Action<Vector3Int> MineTriggered;
        /// <summary>A probed mine started burning. Arguments: cell, fuse length in seconds.</summary>
        public event Action<Vector3Int, float> MineArmed;
        /// <summary>A burning mine turned out to be a dud and will never explode.</summary>
        public event Action<Vector3Int> MineDud;
        /// <summary>Mines were moved after generation (the safe first probe).</summary>
        public event Action LayoutChanged;

        public int Radius => radius;
        public int MineCount { get; private set; }
        public int ProbedCount => probed.Count;
        public int FlagCount => flags.Count;
        public int ExplodedCount => exploded.Count;
        public int DudCount => duds.Count;
        public int ArmedCount => armed.Count;
        public bool HasArmed => armed.Count > 0;

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
            duds.Clear();
            armed.Clear();
            firstProbeDone = false;
            MineCount = 0;

            rng = seed >= 0 ? new System.Random(seed) : new System.Random();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = safeRows; y < field.Height; y++)
                {
                    if (InStartZone(x, y)) continue;
                    if (rng.NextDouble() < density)
                    {
                        mines[x, y] = true;
                        MineCount++;
                    }
                }
            }

            Regenerated?.Invoke();
        }

        private bool InStartZone(int x, int y)
        {
            Vector3Int start = field.StartCell;
            return Mathf.Abs(x - start.x) <= startSafeRadius && Mathf.Abs(y - start.y) <= startSafeRadius;
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
        public bool IsArmed(Vector3Int cell) { return armed.ContainsKey(Key(cell)); }
        public bool IsDud(Vector3Int cell) { return duds.Contains(Key(cell)); }

        private void Update()
        {
            if (armed.Count == 0) return;
            due.Clear();
            foreach (KeyValuePair<Vector3Int, Fuse> pair in armed)
            {
                pair.Value.TimeLeft -= Time.deltaTime;
                if (pair.Value.TimeLeft <= 0f) due.Add(pair.Key);
            }
            foreach (Vector3Int cell in due) Resolve(cell, false);
        }

        private void Resolve(Vector3Int cell, bool forceExplode)
        {
            if (!armed.TryGetValue(cell, out Fuse fuse)) return;
            armed.Remove(cell);
            if (fuse.Dud && !forceExplode)
            {
                duds.Add(cell);
                MineDud?.Invoke(cell);
            }
            else
            {
                exploded.Add(cell);
                MineTriggered?.Invoke(cell);
            }
        }

        /// <summary>Makes one burning mine explode now (a dud included).</summary>
        public void Detonate(Vector3Int cell)
        {
            Resolve(Key(cell), true);
        }

        /// <summary>The player moved: every burning mine explodes now, duds included.</summary>
        public void DetonateArmed()
        {
            if (armed.Count == 0) return;
            foreach (Vector3Int cell in new List<Vector3Int>(armed.Keys)) Resolve(cell, true);
        }

        /// <summary>Clears the probed cell and its 8 neighbours of mines, moving them to other free cells.</summary>
        private void MakeStartSafe(Vector3Int cell)
        {
            int removed = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = cell.x + dx;
                    int y = cell.y + dy;
                    if (x < 0 || y < 0 || x >= field.Width || y >= field.Height) continue;
                    if (mines[x, y])
                    {
                        mines[x, y] = false;
                        removed++;
                    }
                }
            }
            if (removed == 0) return;

            var free = new List<Vector2Int>();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = safeRows; y < field.Height; y++)
                {
                    if (mines[x, y]) continue;
                    if (Mathf.Abs(x - cell.x) <= 1 && Mathf.Abs(y - cell.y) <= 1) continue;
                    if (InStartZone(x, y)) continue;
                    free.Add(new Vector2Int(x, y));
                }
            }

            int placed = 0;
            while (placed < removed && free.Count > 0)
            {
                int pick = rng.Next(free.Count);
                mines[free[pick].x, free[pick].y] = true;
                free[pick] = free[free.Count - 1];
                free.RemoveAt(free.Count - 1);
                placed++;
            }
            MineCount -= removed - placed;
            LayoutChanged?.Invoke();
        }

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
            if (flags.Contains(cell) || probed.ContainsKey(cell) || exploded.Contains(cell) || armed.ContainsKey(cell) || duds.Contains(cell)) return ProbeOutcome.Blocked;

            if (!firstProbeDone)
            {
                firstProbeDone = true;
                MakeStartSafe(cell);
            }

            if (mines[cell.x, cell.y]) return Arm(cell);

            int number = CountAround(cell);
            probed[cell] = number;
            Probed?.Invoke(cell, number);
            return ProbeOutcome.Safe;
        }

        private ProbeOutcome Arm(Vector3Int cell)
        {
            float length = Mathf.Lerp(fuseMin, fuseMax, (float)rng.NextDouble());
            armed[cell] = new Fuse { TimeLeft = length, Dud = rng.NextDouble() < dudChance };
            MineArmed?.Invoke(cell, length);
            return ProbeOutcome.Armed;
        }

        /// <summary>
        /// The player walked onto a cell. An unprobed, unflagged mine there fires exactly as if it had been probed
        /// (same fuse, dud chance and move-to-detonate rule). Safe cells are not changed: no number appears.
        /// </summary>
        public ProbeOutcome StepOn(Vector3Int cell)
        {
            cell = Key(cell);
            if (!field.IsWalkable(cell)) return ProbeOutcome.Invalid;
            if (!mines[cell.x, cell.y]) return ProbeOutcome.Safe;
            if (flags.Contains(cell) || probed.ContainsKey(cell) || exploded.Contains(cell) || armed.ContainsKey(cell) || duds.Contains(cell)) return ProbeOutcome.Blocked;
            return Arm(cell);
        }

        /// <summary>Places or removes a flag. Returns false when the tile cannot hold a flag.</summary>
        public bool ToggleFlag(Vector3Int cell)
        {
            cell = Key(cell);
            if (!field.IsWalkable(cell) || probed.ContainsKey(cell) || exploded.Contains(cell) || armed.ContainsKey(cell) || duds.Contains(cell)) return false;

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

        /// <summary>Dev only: every mine that has not been probed yet.</summary>
        public IEnumerable<Vector3Int> LiveMineCells()
        {
            for (int x = 0; x < mines.GetLength(0); x++)
            {
                for (int y = 0; y < mines.GetLength(1); y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (mines[x, y] && !exploded.Contains(cell) && !armed.ContainsKey(cell) && !duds.Contains(cell)) yield return cell;
                }
            }
        }
    }
}
