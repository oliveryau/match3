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
        const double NewTileMatchChance = 0.1;

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

            Board.Swap(a, b);
            cleared = FindAndClearMatches();
            if (cleared.Count == 0)
            {
                Board.Swap(a, b);
                return false;
            }

            State = PlayState.Playing;
            return true;
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
            return FindAndClearMatches();
        }

        public void FinishTurn()
        {
            IdleAll();
            State = PlayState.WaitOperate;
        }

        List<Cell> FindAndClearMatches()
        {
            var matched = Board.FindColorMatches();
            foreach (var cell in matched)
                Board.Clear(cell.Grid.x, cell.Grid.y);
            IdleAll();
            return matched;
        }

        bool HasPossibleMove()
        {
            for (int x = 0; x < Board.Width; x++)
            for (int y = 0; y < Board.Height; y++)
            {
                var here = Board.Get(x, y);
                if (here == null) continue;
                if (x + 1 < Board.Width && SwapWouldMatch(new GridPos(x, y), new GridPos(x + 1, y)))
                    return true;
                if (y + 1 < Board.Height && SwapWouldMatch(new GridPos(x, y), new GridPos(x, y + 1)))
                    return true;
            }
            return false;
        }

        bool SwapWouldMatch(GridPos a, GridPos b)
        {
            Board.Swap(a, b);
            bool match = Board.FindColorMatches().Count > 0;
            Board.Swap(a, b);
            return match;
        }

        void IdleAll()
        {
            foreach (var cell in Board.AllMain())
            {
                if (cell.State == CellState.Move || cell.State == CellState.Born)
                    cell.State = CellState.Idle;
            }
        }
    }
}
