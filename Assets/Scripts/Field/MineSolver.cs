using System.Collections.Generic;
using UnityEngine;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Deduction-only solver used to make fields fair. It plays the field the way a careful player would:
    /// probe the first cell, probe every cell proven safe, and use the radar numbers (single-cell rules plus the
    /// subset rule between two numbers) to prove more cells. It never guesses.
    /// </summary>
    public static class MineSolver
    {
        private const byte Unknown = 0, Safe = 1, Mine = 2, Probed = 3;

        private struct Constraint
        {
            public int[] Cells;   // sorted indices x * height + y of the unknown cells around a probed cell
            public int Mines;     // how many of those cells are mines
        }

        /// <summary>
        /// Returns how many safe cells could NOT be proven. `unresolved` receives those cells.
        /// 0 means the whole field can be cleared without a single guess.
        /// </summary>
        public static int Solve(bool[,] mines, bool[,] walkable, Vector2Int start, int radius, List<Vector2Int> unresolved)
        {
            int w = mines.GetLength(0);
            int h = mines.GetLength(1);
            var state = new byte[w, h];
            var number = new int[w, h];
            var probedCells = new List<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            var constraints = new List<Constraint>();
            var unknownBuffer = new List<int>();

            state[start.x, start.y] = Safe;
            stack.Push(start);

            while (true)
            {
                // 1) probe everything that is proven safe
                while (stack.Count > 0)
                {
                    Vector2Int c = stack.Pop();
                    if (state[c.x, c.y] == Probed) continue;
                    state[c.x, c.y] = Probed;
                    int n = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int a = c.x + dx, b = c.y + dy;
                            if (a < 0 || b < 0 || a >= w || b >= h || !walkable[a, b]) continue;
                            if (mines[a, b]) n++;
                        }
                    }
                    number[c.x, c.y] = n;
                    probedCells.Add(c);
                    if (n != 0) continue;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            int a = c.x + dx, b = c.y + dy;
                            if (a < 0 || b < 0 || a >= w || b >= h || !walkable[a, b]) continue;
                            if (state[a, b] != Unknown) continue;
                            state[a, b] = Safe;
                            stack.Push(new Vector2Int(a, b));
                        }
                    }
                }

                // 2) single-number rules
                bool progress = false;
                constraints.Clear();
                foreach (Vector2Int c in probedCells)
                {
                    unknownBuffer.Clear();
                    int knownMines = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int a = c.x + dx, b = c.y + dy;
                            if (a < 0 || b < 0 || a >= w || b >= h || !walkable[a, b]) continue;
                            if (state[a, b] == Unknown) unknownBuffer.Add(a * h + b);
                            else if (state[a, b] == Mine) knownMines++;
                        }
                    }
                    if (unknownBuffer.Count == 0) continue;
                    int remaining = number[c.x, c.y] - knownMines;
                    if (remaining == 0)
                    {
                        foreach (int i in unknownBuffer) MarkSafe(i, h, state, stack);
                        progress = true;
                    }
                    else if (remaining == unknownBuffer.Count)
                    {
                        foreach (int i in unknownBuffer) state[i / h, i % h] = Mine;
                        progress = true;
                    }
                    else
                    {
                        unknownBuffer.Sort();
                        constraints.Add(new Constraint { Cells = unknownBuffer.ToArray(), Mines = remaining });
                    }
                }
                if (progress) continue;

                // 3) subset rule: if A is inside B, the cells of B outside A hold (B - A) mines
                for (int i = 0; i < constraints.Count; i++)
                {
                    for (int j = 0; j < constraints.Count; j++)
                    {
                        if (i == j) continue;
                        Constraint a = constraints[i];
                        Constraint b = constraints[j];
                        if (a.Cells.Length >= b.Cells.Length) continue;
                        if (!IsSubset(a.Cells, b.Cells)) continue;
                        int diffMines = b.Mines - a.Mines;
                        int diffCount = b.Cells.Length - a.Cells.Length;
                        if (diffMines != 0 && diffMines != diffCount) continue;
                        int ai = 0;
                        for (int k = 0; k < b.Cells.Length; k++)
                        {
                            int idx = b.Cells[k];
                            if (ai < a.Cells.Length && a.Cells[ai] == idx) { ai++; continue; }
                            if (state[idx / h, idx % h] != Unknown) continue;
                            if (diffMines == 0) MarkSafe(idx, h, state, stack);
                            else state[idx / h, idx % h] = Mine;
                            progress = true;
                        }
                    }
                }
                if (!progress) break;
            }

            unresolved?.Clear();
            int left = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (!walkable[x, y] || mines[x, y] || state[x, y] != Unknown) continue;
                    left++;
                    unresolved?.Add(new Vector2Int(x, y));
                }
            }
            return left;
        }

        private static void MarkSafe(int index, int h, byte[,] state, Stack<Vector2Int> stack)
        {
            int x = index / h, y = index % h;
            if (state[x, y] != Unknown) return;
            state[x, y] = Safe;
            stack.Push(new Vector2Int(x, y));
        }

        private static bool IsSubset(int[] small, int[] big)
        {
            int j = 0;
            for (int i = 0; i < small.Length; i++)
            {
                while (j < big.Length && big[j] < small[i]) j++;
                if (j >= big.Length || big[j] != small[i]) return false;
                j++;
            }
            return true;
        }
    }
}
