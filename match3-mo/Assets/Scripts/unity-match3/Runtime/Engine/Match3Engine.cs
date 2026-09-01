using System;
using System.Collections.Generic;

namespace Match3
{
    public class BoardCommand
    {
        public enum Kind
        {
            Swap,
            SwapFail,
            Clear,
            SpawnSpecial,
            Drop,
            Spawn,
            Shuffle,
            Hint
        }

        public Kind Type;
        public GridPos A;
        public GridPos B;
        public List<GridPos> Grids = new List<GridPos>();
        public SpecialType Special;
        public int ColorId;
        public Cell Cell;
    }

    /// <summary>
    /// Swap / clear / drop are stepped so the view can animate each wave.
    /// Tiles only fall straight down; the board is never shuffled during play.
    /// </summary>
    public class Match3Engine
    {
        public readonly Board Board;
        public readonly FinderMgr Finder;
        public readonly CheckMgr Checks;
        public readonly CheckHelper Helper;
        public readonly DropSystem Drop;
        public readonly SpecialResolver Specials;

        public int ColorCount = 5;
        public PlayState State = PlayState.WaitOperate;
        /// <summary>0 = never spawn a color that matches immediately when avoidable.</summary>
        public double NewTileMatchChance = 0.02;

        readonly Random _rng = new Random();

        public Match3Engine(int width, int height)
        {
            Board = new Board();
            Board.Init(width, height);
            Finder = new FinderMgr(Board);
            Checks = new CheckMgr(Finder);
            Helper = new CheckHelper(Board, Finder, Checks);
            Drop = new DropSystem(Board);
            Specials = new SpecialResolver(Board, Finder);
        }

        public void NewBoard(int colorCount = 5)
        {
            ColorCount = colorCount;
            for (int x = 0; x < Board.Width; x++)
            for (int y = 0; y < Board.Height; y++)
            {
                Board.SetGridType(x, y, GridType.NormalGrid);
                Board.Set(x, y, null);
            }

            int guard = 40;
            do
            {
                Board.FillNoMatches(ColorCount, _rng);
            } while ((Board.FindColorMatches().Count > 0 || !HasPossibleMove()) && --guard > 0);

            IdleAll();
            State = PlayState.WaitOperate;
        }

        /// <summary>Replaces the leftmost and rightmost columns with fixed obstacles.</summary>
        public void PlaceEdgeColumnObstacles(ObjectType type = ObjectType.Box)
        {
            if (Board.Width < 3)
                return;

            int right = Board.Width - 1;
            for (int y = 0; y < Board.Height; y++)
            {
                PlaceObstacle(0, y, type);
                PlaceObstacle(right, y, type);
            }
        }

        void PlaceObstacle(int x, int y, ObjectType type)
        {
            Board.Set(x, y, Cell.CreateObstacle(type, new GridPos(x, y)));
        }

        public List<Cell> LastSpawnedSpecials { get; private set; } = new List<Cell>();
        public GridPos? PreferredSpecialSpawn { get; set; }
        /// <summary>Longest same-color run in the last clear (0 if peach-only / none).</summary>
        public int LastMaxMatchRunLength { get; private set; }
        /// <summary>True if the last clear was a player-triggered gold peach burst.</summary>
        public bool LastWasGoldPeachBurst { get; private set; }

        public Cell SpawnColor(int x, int y)
        {
            int color = Board.PickColorAvoidingMatch(x, y, ColorCount, _rng, NewTileMatchChance);
            return Cell.CreateNormal(color, new GridPos(x, y));
        }

        public bool TrySwap(GridPos a, GridPos b, out List<Cell> cleared)
        {
            cleared = new List<Cell>();
            if (State != PlayState.WaitOperate) return false;
            if (!a.IsNeighbor(b)) return false;

            var ca = Board.Get(a);
            var cb = Board.Get(b);
            if (ca == null || cb == null || !ca.CanOperate || !cb.CanOperate)
                return false;

            Board.Swap(a, b);
            var atA = Board.Get(a);
            var atB = Board.Get(b);

            // Gold peach swapped with anything → burst (chains into other gold peaches).
            if (IsGoldPeach(atA) || IsGoldPeach(atB))
            {
                var peach = IsGoldPeach(atA) ? atA : atB;
                cleared = DetonateGoldPeach(peach);
                State = PlayState.Playing;
                return true;
            }

            // Prefer spawning a new gold peach where the swapped-in piece landed.
            PreferredSpecialSpawn = b;
            cleared = FindAndClearMatches();
            PreferredSpecialSpawn = null;
            if (cleared.Count == 0)
            {
                Board.Swap(a, b);
                return false;
            }

            State = PlayState.Playing;
            return true;
        }

        static bool IsGoldPeach(Cell cell)
        {
            return cell != null
                   && cell.IsSpecial
                   && (cell.Special == SpecialType.HMissile || cell.Special == SpecialType.VMissile);
        }

        List<Cell> DetonateGoldPeach(Cell peach)
        {
            LastSpawnedSpecials = new List<Cell>();
            LastMaxMatchRunLength = 0;
            LastWasGoldPeachBurst = true;
            if (peach == null)
                return new List<Cell>();

            var resolve = new ResolveResult();
            Specials.Activate(peach, resolve, peach.ColorId);
            IdleAll();
            return new List<Cell>(resolve.ClearedCells);
        }

        public List<Cell> DropFill(out List<Cell> spawned)
        {
            var moved = new List<Cell>();
            spawned = new List<Cell>();
            for (int x = 0; x < Board.Width; x++)
                Board.CompactColumnDown(x, moved);

            for (int x = 0; x < Board.Width; x++)
            {
                for (int y = 0; y < Board.Height; y++)
                {
                    if (Board.Get(x, y) != null) continue;
                    var created = SpawnColor(x, y);
                    Board.Set(x, y, created);
                    spawned.Add(created);
                }
            }

            IdleAll();
            return moved;
        }

        public List<Cell> ClearMatches()
        {
            PreferredSpecialSpawn = null;
            return FindAndClearMatches();
        }

        public void FinishTurn()
        {
            IdleAll();
            State = PlayState.WaitOperate;
        }

        List<Cell> FindAndClearMatches()
        {
            LastSpawnedSpecials = new List<Cell>();
            LastWasGoldPeachBurst = false;
            LastMaxMatchRunLength = 0;

            var matched = new HashSet<Cell>();
            var peachSpawns = new List<(GridPos grid, int colorId, SpecialType special)>();

            CollectLineMatches(horizontal: true, matched, peachSpawns);
            CollectLineMatches(horizontal: false, matched, peachSpawns);

            if (matched.Count == 0)
                return new List<Cell>();

            // One peach per spawn cell (e.g. L-shapes that form both a row and column).
            var spawnByGrid = new Dictionary<int, (GridPos grid, int colorId, SpecialType special)>();
            for (int i = 0; i < peachSpawns.Count; i++)
            {
                var spawn = peachSpawns[i];
                int id = Board.GetGridId(spawn.grid);
                if (!spawnByGrid.ContainsKey(id))
                    spawnByGrid[id] = spawn;
            }

            var cleared = new List<Cell>(matched);
            foreach (var cell in cleared)
                Board.Clear(cell.Grid.x, cell.Grid.y);

            cleared.AddRange(DamageAdjacentObstacles(cleared));

            foreach (var kv in spawnByGrid)
            {
                var spawn = kv.Value;
                if (Board.Get(spawn.grid.x, spawn.grid.y) != null)
                    continue;
                var special = Cell.CreateSpecial(spawn.special, spawn.colorId, spawn.grid);
                Board.Set(spawn.grid.x, spawn.grid.y, special);
                LastSpawnedSpecials.Add(special);
            }

            IdleAll();
            return cleared;
        }

        /// <summary>Obstacles adjacent to cleared tiles lose HP and are removed at 0.</summary>
        List<Cell> DamageAdjacentObstacles(IReadOnlyList<Cell> clearedTiles)
        {
            var destroyed = new List<Cell>();
            var damaged = new HashSet<Cell>();

            for (int i = 0; i < clearedTiles.Count; i++)
            {
                var cleared = clearedTiles[i];
                if (cleared == null)
                    continue;

                foreach (var neighbor in ShapeTable.Neighbors4(cleared.Grid))
                {
                    var obstacle = Board.Get(neighbor);
                    if (obstacle == null || !obstacle.IsObstacle || !obstacle.CanCleanUpByRoundItem)
                        continue;
                    if (!damaged.Add(obstacle))
                        continue;

                    obstacle.Hp--;
                    if (obstacle.Hp > 0)
                        continue;

                    obstacle.State = CellState.Destroy;
                    Board.Clear(neighbor.x, neighbor.y);
                    destroyed.Add(obstacle);
                }
            }

            return destroyed;
        }

        void CollectLineMatches(
            bool horizontal,
            HashSet<Cell> matched,
            List<(GridPos grid, int colorId, SpecialType special)> peachSpawns)
        {
            int major = horizontal ? Board.Height : Board.Width;
            int minor = horizontal ? Board.Width : Board.Height;

            for (int a = 0; a < major; a++)
            {
                int b = 0;
                while (b < minor)
                {
                    var start = horizontal ? Board.Get(b, a) : Board.Get(a, b);
                    if (start == null || !start.IsNormal)
                    {
                        b++;
                        continue;
                    }

                    int end = b + 1;
                    while (end < minor)
                    {
                        var next = horizontal ? Board.Get(end, a) : Board.Get(a, end);
                        if (next == null || !next.IsNormal || next.ColorId != start.ColorId)
                            break;
                        end++;
                    }

                    int length = end - b;
                    if (length >= 3)
                    {
                        if (length > LastMaxMatchRunLength)
                            LastMaxMatchRunLength = length;

                        var run = new List<Cell>(length);
                        for (int i = b; i < end; i++)
                        {
                            var cell = horizontal ? Board.Get(i, a) : Board.Get(a, i);
                            if (cell != null)
                            {
                                matched.Add(cell);
                                run.Add(cell);
                            }
                        }

                        if (length >= 4 && run.Count > 0)
                        {
                            var spawnCell = PickPeachSpawnCell(run);
                            if (spawnCell != null)
                            {
                                peachSpawns.Add((
                                    spawnCell.Grid,
                                    spawnCell.ColorId,
                                    horizontal ? SpecialType.HMissile : SpecialType.VMissile));
                            }
                        }
                    }

                    b = end;
                }
            }
        }

        Cell PickPeachSpawnCell(List<Cell> run)
        {
            if (PreferredSpecialSpawn.HasValue)
            {
                var preferred = PreferredSpecialSpawn.Value;
                for (int i = 0; i < run.Count; i++)
                {
                    if (run[i].Grid.Equals(preferred))
                        return run[i];
                }
            }

            return run[run.Count / 2];
        }

        bool HasPossibleMove()
        {
            return TryGetHintSwap(out _, out _, out _);
        }

        /// <summary>First legal swap that creates a match (or involves a gold peach).</summary>
        public bool TryGetHintSwap(out GridPos a, out GridPos b)
        {
            return TryGetHintSwap(out a, out b, out _);
        }

        /// <summary>
        /// First legal swap, plus the pre-swap grid to highlight: the tile that
        /// ends up inside the resulting match (not the tile swapped out of it).
        /// </summary>
        public bool TryGetHintSwap(out GridPos a, out GridPos b, out GridPos highlight)
        {
            a = default;
            b = default;
            highlight = default;
            for (int x = 0; x < Board.Width; x++)
            for (int y = 0; y < Board.Height; y++)
            {
                var here = Board.Get(x, y);
                if (here == null || !here.CanOperate) continue;

                if (x + 1 < Board.Width)
                {
                    var right = new GridPos(x + 1, y);
                    if (TryDescribeHintSwap(new GridPos(x, y), right, out highlight))
                    {
                        a = new GridPos(x, y);
                        b = right;
                        return true;
                    }
                }

                if (y + 1 < Board.Height)
                {
                    var up = new GridPos(x, y + 1);
                    if (TryDescribeHintSwap(new GridPos(x, y), up, out highlight))
                    {
                        a = new GridPos(x, y);
                        b = up;
                        return true;
                    }
                }
            }
            return false;
        }

        bool SwapWouldMatch(GridPos a, GridPos b)
        {
            return TryDescribeHintSwap(a, b, out _);
        }

        /// <summary>
        /// Returns true if swapping a↔b matches. <paramref name="highlight"/> is the
        /// pre-swap position of a tile that belongs to the match after the swap.
        /// </summary>
        bool TryDescribeHintSwap(GridPos a, GridPos b, out GridPos highlight)
        {
            highlight = a;
            var ca = Board.Get(a);
            var cb = Board.Get(b);
            if (ca == null || cb == null)
                return false;

            if (IsGoldPeach(ca) || IsGoldPeach(cb))
            {
                // Prefer highlighting the gold peach itself.
                highlight = IsGoldPeach(ca) ? a : b;
                return true;
            }

            Board.Swap(a, b);
            var matched = Board.FindColorMatches();
            bool ok = matched.Count > 0;
            if (ok)
            {
                // After swap, the piece that started at `a` sits at `b`, and vice versa.
                // Highlight the pre-swap seat of whichever piece is in the match.
                var movedFromA = Board.Get(b);
                var movedFromB = Board.Get(a);
                if (matched.Contains(movedFromA) && !matched.Contains(movedFromB))
                    highlight = a;
                else if (matched.Contains(movedFromB) && !matched.Contains(movedFromA))
                    highlight = b;
                else if (matched.Contains(movedFromA))
                    highlight = a;
                else if (matched.Contains(movedFromB))
                    highlight = b;
                else
                {
                    // Match elsewhere (rare); fall back to a matched cell's grid.
                    highlight = matched[0].Grid.Equals(a) || matched[0].Grid.Equals(b)
                        ? matched[0].Grid
                        : a;
                }
            }
            Board.Swap(a, b);
            return ok;
        }

        void IdleAll()
        {
            foreach (var cell in Board.AllMain())
            {
                if (cell.State == CellState.Move || cell.State == CellState.Born || cell.State == CellState.Destroy)
                    cell.State = CellState.Idle;
            }
        }
    }
}
