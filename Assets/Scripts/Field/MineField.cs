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

    /// <summary>What a hold-to-act on a cell would do. The answer never reveals whether an unflagged cell holds a mine.</summary>
    public enum CellWork
    {
        None,        // nothing to do here (outside the field, already probed, exploded or collected)
        NeedsFlag,   // an untouched cell: flag it first
        Armed,       // a burning mine: too late
        Defuse,      // flagged and not yet defused
        Collect      // a defused mine or an exposed dud
    }

    public enum DefuseOutcome
    {
        Invalid,
        Defused,     // the flagged mine is safe now
        NothingThere // the flag was on safe ground; the flag is removed
    }

    /// <summary>
    /// Hidden mines, probe results and flags for one field. Pure game state: all visuals live in MineFieldView.
    /// Probing a mine arms it: the mine is shown, then after a random fuse (fuseMin..fuseMax seconds) it explodes,
    /// unless it is a dud (dudChance). If the player moves while a fuse burns, the mine always explodes at once.
    /// The spawn cell and its 8 neighbours are mine-free from the start. Probing elsewhere is not guaranteed safe and never reshuffles mines.
    /// Walking never triggers mines; only probing does. A radar number is the count of mines within `radius`
    /// cells (8 neighbours for radius 1) and is stamped only on tiles the player probed.
    /// Collect chain (days with the collect-mines rule): a flagged mine can be defused, a defused mine or an exposed
    /// dud can be collected. A probed mine is armed and can never be defused. A collected mine is out of the ground: the radar numbers of the probed cells around it go down by one.
    /// </summary>
    public class MineField : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField, Range(0f, MaxDensity)] private float density = 0.14f;
        // At the start only the player's cell and its 8 neighbours are mine-free; everything else may hold mines.
        // Constants (not inspector fields) so old scene values cannot override this.
        private const float MaxDensity = 0.5f;
        private const int GoodRepairDivisor = 6;     // a layout that needed at most MineCount / this fixes is good enough to stop searching
        private const int MinGoodRepairs = 2;
        private const int LayoutTries = 12;          // random layouts tried; the one needing the fewest fixes wins
        private const int TopUpAttempts = 60;        // tries to put removed mines back without creating a guess
        private const int startSafeRadius = 1;
        [SerializeField] private int radius = 1;
        [Tooltip("-1 = new random layout every time")]
        [SerializeField] private int seed = -1;
        [Header("Fairness")]
        [Tooltip("After the first probe, remove just enough mines that the whole field can be cleared by deduction alone (no guessing)")]
        [SerializeField] private bool noGuessing = true;
        [Tooltip("How many fix-up attempts the fairness pass may make on one layout")]
        [SerializeField] private int maxRepairs = 1000;
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
        private readonly HashSet<Vector3Int> defused = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> collected = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, Fuse> armed = new Dictionary<Vector3Int, Fuse>();
        private readonly List<Vector3Int> due = new List<Vector3Int>();
        private System.Random rng = new System.Random();

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
        /// <summary>A flagged mine was defused.</summary>
        public event Action<Vector3Int> MineDefused;
        /// <summary>A defused mine or a dud was collected. Arguments: cell, true when it was a dud.</summary>
        public event Action<Vector3Int, bool> MineCollected;
        /// <summary>The radar number of a probed cell changed because a mine near it was collected. Arguments: cell, new number.</summary>
        public event Action<Vector3Int, int> NumberChanged;

        public int MineCount { get; private set; }
        public int ProbedCount => probed.Count;
        public int FlagCount => flags.Count;
        public int ExplodedCount => exploded.Count;
        public int DudCount => duds.Count;
        public int CollectedCount => collected.Count;
        public int ArmedCount => armed.Count;
        public bool HasArmed => armed.Count > 0;

        /// <summary>Fuse length range in seconds and the dud chance. The cheat window changes them live; they apply to mines armed afterwards.</summary>
        public float FuseMin { get => fuseMin; set => fuseMin = Mathf.Max(0f, value); }
        public float FuseMax { get => fuseMax; set => fuseMax = Mathf.Max(0f, value); }
        public float DudChance { get => dudChance; set => dudChance = Mathf.Clamp01(value); }

        /// <summary>Generates with the inspector density (about density x cells mines).</summary>
        public void Generate()
        {
            Generate(Mathf.RoundToInt(density * field.Width * field.Height));
        }

        /// <summary>Generates a field aiming for this many mines. The fairness pass can leave a few fewer when the layout could not be solved without them.</summary>
        public void Generate(int mineCount)
        {
            probed.Clear();
            flags.Clear();
            exploded.Clear();
            duds.Clear();
            defused.Clear();
            collected.Clear();
            armed.Clear();

            rng = seed >= 0 ? new System.Random(seed) : new System.Random();

            // The spawn zone is mine-free. With noGuessing the layout is also made solvable from the spawn cell
            // right now, so probing anywhere later never reshuffles the field.
            mines = noGuessing ? FairLayout(mineCount) : RandomLayout(mineCount);
            MineCount = CountMines(mines);

            Regenerated?.Invoke();
        }

        private bool[,] RandomLayout(int mineCount)
        {
            var layout = new bool[field.Width, field.Height];
            var candidates = new List<Vector2Int>();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    if (!InStartZone(x, y) && field.IsWalkable(new Vector3Int(x, y, 0))) candidates.Add(new Vector2Int(x, y));
                }
            }
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            int count = Mathf.Min(mineCount, candidates.Count);
            for (int i = 0; i < count; i++) layout[candidates[i].x, candidates[i].y] = true;
            return layout;
        }

        private static int CountMines(bool[,] layout)
        {
            int count = 0;
            foreach (bool mine in layout)
            {
                if (mine) count++;
            }
            return count;
        }

        private bool InStartZone(int x, int y)
        {
            Vector3Int start = field.StartCell;
            return Mathf.Abs(x - start.x) <= startSafeRadius && Mathf.Abs(y - start.y) <= startSafeRadius;
        }

        public bool IsMine(Vector3Int cell)
        {
            return field.IsWalkable(cell) && mines[cell.x, cell.y];
        }

        /// <summary>
        /// The cell already has an outcome (probed, burning, exploded, a dud, defused or collected):
        /// it cannot be probed, flagged or unflagged any more, and walking onto it is safe.
        /// </summary>
        private bool IsResolved(Vector3Int cell)
        {
            return probed.ContainsKey(cell) || exploded.Contains(cell) || armed.ContainsKey(cell) || duds.Contains(cell)
                || defused.Contains(cell) || collected.Contains(cell);
        }

        public bool IsProbed(Vector3Int cell) { return probed.ContainsKey(cell.Flat()); }
        public bool IsExploded(Vector3Int cell) { return exploded.Contains(cell.Flat()); }

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
                mines[cell.x, cell.y] = false;   // the blast destroys the mine: it no longer counts for the numbers around it
                MineCount--;
                MineTriggered?.Invoke(cell);
                RefreshNumbersAround(cell);
            }
        }

        /// <summary>Makes one burning mine explode now (a dud included).</summary>
        public void Detonate(Vector3Int cell)
        {
            Resolve(cell.Flat(), true);
        }

        /// <summary>The player moved: every burning mine explodes now, duds included.</summary>
        public void DetonateArmed()
        {
            if (armed.Count == 0) return;
            foreach (Vector3Int cell in new List<Vector3Int>(armed.Keys)) Resolve(cell, true);
        }

        /// <summary>
        /// Builds a layout the deduction solver can clear from the spawn cell without a single guess, with no safe cell walled off by mines.
        /// Tries several random layouts and repairs each one by removing mines next to cells that cannot be proven safe;
        /// the layout that needed the fewest fixes wins. The removed mines are then put back wherever that keeps the field solvable.
        /// </summary>
        private bool[,] FairLayout(int mineCount)
        {
            var walkable = new bool[field.Width, field.Height];
            for (int x = 0; x < field.Width; x++)
                for (int y = 0; y < field.Height; y++)
                    walkable[x, y] = field.IsWalkable(new Vector3Int(x, y, 0));

            Vector3Int start = field.StartCell;
            var origin = new Vector2Int(start.x, start.y);
            bool[,] best = null;
            int bestLeft = int.MaxValue, bestRemoved = int.MaxValue;

            for (int attempt = 0; attempt < LayoutTries; attempt++)
            {
                bool[,] layout = RandomLayout(mineCount);
                int removed = Repair(layout, walkable, origin, out int left);   // left = flaws still in the layout
                if (left < bestLeft || (left == bestLeft && removed < bestRemoved))
                {
                    best = layout;
                    bestLeft = left;
                    bestRemoved = removed;
                }
                if (left == 0 && removed <= Mathf.Max(MinGoodRepairs, mineCount / GoodRepairDivisor)) break;
            }

            int missing = Mathf.Min(mineCount, CountMines(best) + bestRemoved) - CountMines(best);
            int added = bestLeft == 0 ? TopUp(best, walkable, origin, missing) : 0;
            Debug.Log("[MineField] fairness pass: removed " + bestRemoved + " mine(s), put back " + added + ", " + bestLeft + " flaw(s) left (cells needing a guess or cut off), " + CountMines(best) + " mines left");
            return best;
        }

        /// <summary>
        /// Removes mines until the layout is fair: the solver clears it without a guess and no safe cell is cut off from the
        /// spawn by a wall of mines. Mines next to an unprovable cell go first, then mines walling off a pocket.
        /// Returns how many were removed; <paramref name="flaws"/> is what is still wrong (0 = fair).
        /// </summary>
        private int Repair(bool[,] layout, bool[,] walkable, Vector2Int origin, out int flaws)
        {
            var unresolved = new List<Vector2Int>();
            var cut = new List<Vector2Int>();
            var candidates = new List<Vector2Int>();
            int removed = 0;
            flaws = 0;

            for (int attempt = 0; attempt <= maxRepairs; attempt++)
            {
                int unproven = MineSolver.Solve(layout, walkable, origin, radius, unresolved);
                FindCutOff(layout, walkable, origin, cut);
                flaws = unproven + cut.Count;
                if (flaws == 0 || attempt == maxRepairs) break;

                candidates.Clear();
                if (unproven > 0)
                {
                    Vector2Int stuck = unresolved[rng.Next(unresolved.Count)];
                    for (int reach = radius; reach <= radius + 1 && candidates.Count == 0; reach++)
                    {
                        for (int dx = -reach; dx <= reach; dx++)
                        {
                            for (int dy = -reach; dy <= reach; dy++)
                            {
                                AddIfMine(layout, stuck.x + dx, stuck.y + dy, candidates);
                            }
                        }
                    }
                }
                else
                {
                    Vector2Int pocket = cut[rng.Next(cut.Count)];
                    foreach (Vector2Int step in Orthogonal) AddIfMine(layout, pocket.x + step.x, pocket.y + step.y, candidates);
                }
                if (candidates.Count == 0) continue;

                Vector2Int pick = candidates[rng.Next(candidates.Count)];
                layout[pick.x, pick.y] = false;
                removed++;
            }
            return removed;
        }

        private static readonly Vector2Int[] Orthogonal = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private void AddIfMine(bool[,] layout, int x, int y, List<Vector2Int> into)
        {
            if (x < 0 || y < 0 || x >= field.Width || y >= field.Height) return;
            if (layout[x, y]) into.Add(new Vector2Int(x, y));
        }

        /// <summary>
        /// Safe cells the deminer could not walk to from the spawn without crossing a mine (steps are orthogonal, so a diagonal
        /// line of mines is a wall too). Fills <paramref name="cut"/> with them.
        /// </summary>
        private void FindCutOff(bool[,] layout, bool[,] walkable, Vector2Int origin, List<Vector2Int> cut)
        {
            int w = layout.GetLength(0), h = layout.GetLength(1);
            var seen = new bool[w, h];
            var queue = new Queue<Vector2Int>();
            seen[origin.x, origin.y] = true;
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int step in Orthogonal)
                {
                    int x = c.x + step.x, y = c.y + step.y;
                    if (x < 0 || y < 0 || x >= w || y >= h || seen[x, y] || layout[x, y] || !walkable[x, y]) continue;
                    seen[x, y] = true;
                    queue.Enqueue(new Vector2Int(x, y));
                }
            }
            cut.Clear();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (walkable[x, y] && !layout[x, y] && !seen[x, y]) cut.Add(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>Adds up to <paramref name="missing"/> mines, each only where the layout stays fair. Returns how many were added.</summary>
        private int TopUp(bool[,] layout, bool[,] walkable, Vector2Int origin, int missing)
        {
            var cut = new List<Vector2Int>();
            int added = 0;
            for (int attempt = 0; attempt < TopUpAttempts && added < missing; attempt++)
            {
                int x = rng.Next(field.Width), y = rng.Next(field.Height);
                if (layout[x, y] || !walkable[x, y] || InStartZone(x, y)) continue;
                layout[x, y] = true;
                FindCutOff(layout, walkable, origin, cut);
                if (cut.Count == 0 && MineSolver.Solve(layout, walkable, origin, radius, null) == 0) added++;
                else layout[x, y] = false;
            }
            return added;
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
            cell = cell.Flat();
            if (!field.IsWalkable(cell)) return ProbeOutcome.Invalid;
            if (flags.Contains(cell) || IsResolved(cell)) return ProbeOutcome.Blocked;

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
            cell = cell.Flat();
            if (!field.IsWalkable(cell)) return ProbeOutcome.Invalid;
            if (!mines[cell.x, cell.y]) return ProbeOutcome.Safe;
            if (flags.Contains(cell) || IsResolved(cell)) return ProbeOutcome.Blocked;
            return Arm(cell);
        }

        /// <summary>Places or removes a flag. Returns false when the tile cannot hold a flag.</summary>
        public bool ToggleFlag(Vector3Int cell)
        {
            cell = cell.Flat();
            if (!field.IsWalkable(cell) || IsResolved(cell)) return false;

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

        /// <summary>What holding the action on this cell would do (see <see cref="CellWork"/>).</summary>
        public CellWork WorkAt(Vector3Int cell)
        {
            cell = cell.Flat();
            if (!field.IsWalkable(cell) || collected.Contains(cell)) return CellWork.None;
            if (armed.ContainsKey(cell)) return CellWork.Armed;
            if (duds.Contains(cell) || defused.Contains(cell)) return CellWork.Collect;
            if (flags.Contains(cell)) return CellWork.Defuse;
            if (probed.ContainsKey(cell) || exploded.Contains(cell)) return CellWork.None;
            return CellWork.NeedsFlag;
        }

        /// <summary>Defuses a flagged mine. On safe ground the flag is removed instead (the time was wasted).</summary>
        public DefuseOutcome Defuse(Vector3Int cell)
        {
            cell = cell.Flat();
            if (WorkAt(cell) != CellWork.Defuse) return DefuseOutcome.Invalid;

            if (!mines[cell.x, cell.y])
            {
                flags.Remove(cell);
                FlagChanged?.Invoke(cell, false);
                return DefuseOutcome.NothingThere;
            }
            defused.Add(cell);
            MineDefused?.Invoke(cell);
            return DefuseOutcome.Defused;
        }

        /// <summary>Takes a defused mine or an exposed dud out of the ground. Returns false when there is nothing to collect.</summary>
        public bool Collect(Vector3Int cell)
        {
            cell = cell.Flat();
            if (WorkAt(cell) != CellWork.Collect) return false;

            bool wasDud = duds.Contains(cell);
            collected.Add(cell);
            mines[cell.x, cell.y] = false;   // the mine is out of the ground
            MineCount--;
            if (flags.Remove(cell)) FlagChanged?.Invoke(cell, false);
            MineCollected?.Invoke(cell, wasDud);
            RefreshNumbersAround(cell);
            return true;
        }

        /// <summary>Recounts every probed cell within the radar radius of a cell where a mine was taken out.</summary>
        private void RefreshNumbersAround(Vector3Int cell)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var near = new Vector3Int(cell.x + dx, cell.y + dy, 0);
                    if (!probed.TryGetValue(near, out int before)) continue;
                    int now = CountAround(near);
                    if (now == before) continue;
                    probed[near] = now;
                    NumberChanged?.Invoke(near, now);
                }
            }
        }

        /// <summary>Exposed duds that were not collected (they count as located but still lie in the ground).</summary>
        public int UncollectedDudCount
        {
            get
            {
                int count = 0;
                foreach (Vector3Int cell in duds)
                {
                    if (!collected.Contains(cell)) count++;
                }
                return count;
            }
        }

        /// <summary>Counts flags on real mines and flags on safe ground (for the debrief).</summary>
        public void Tally(out int correct, out int wrong)
        {
            correct = 0;
            wrong = 0;
            foreach (Vector3Int f in flags)
            {
                if (mines[f.x, f.y]) correct++;
                else wrong++;
            }
        }

        /// <summary>Dev only: every mine that has not been probed yet.</summary>
        public IEnumerable<Vector3Int> LiveMineCells()
        {
            for (int x = 0; x < mines.GetLength(0); x++)
            {
                for (int y = 0; y < mines.GetLength(1); y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (mines[x, y] && !exploded.Contains(cell) && !armed.ContainsKey(cell) && !duds.Contains(cell) && !collected.Contains(cell)) yield return cell;
                }
            }
        }
    }
}
