using System;
using System.Collections.Generic;

namespace Match3
{
    /// <summary>
    /// Port of drop/CellDownMgr.ts + CellDropNotifyMgrV1.
    /// Gravity is -Y. Each column queues holes and, after DelayNotifyTime,
    /// pulls the cell above, or slides from the diagonal if the column is blocked.
    /// </summary>
    public class DropSystem
    {
        public const float DelayNotifyTime = 0.08f;

        readonly Board _board;
        readonly List<float> _timer;
        readonly List<List<int>> _holes;

        public DropSystem(Board board)
        {
            _board = board;
            _timer = new List<float>(board.Width);
            _holes = new List<List<int>>(board.Width);
            for (int x = 0; x < board.Width; x++)
            {
                _timer.Add(0f);
                _holes.Add(new List<int>());
            }
        }

        public bool IsBusy
        {
            get
            {
                for (int x = 0; x < _holes.Count; x++)
                    if (_holes[x].Count > 0) return true;
                return false;
            }
        }

        public void Clear()
        {
            for (int x = 0; x < _holes.Count; x++)
            {
                _holes[x].Clear();
                _timer[x] = 0f;
            }
        }

        public void Notify(GridPos grid)
        {
            int y = grid.y + 1;
            if (_board.IsOut(grid.x, y)) return;
            Push(grid.x, y);
        }

        public void NotifyMany(IEnumerable<GridPos> grids)
        {
            foreach (var g in grids) Notify(g);
        }

        public void Push(int x, int y)
        {
            if (x < 0 || x >= _holes.Count) return;
            if (_holes[x].Contains(y)) return;
            _holes[x].Add(y);
            _holes[x].Sort();
            if (_timer[x] <= 0f) _timer[x] = DelayNotifyTime;
        }

        /// <summary>Apply as many gravity steps as possible. Returns cells that moved this call.</summary>
        public List<Cell> Tick(float dt, Func<int, Cell> spawnAtTop)
        {
            var moved = new List<Cell>();
            for (int x = 0; x < _holes.Count; x++)
            {
                if (_timer[x] > 0f)
                {
                    _timer[x] -= dt;
                    if (_timer[x] > 0f) continue;
                    _timer[x] = 0f;
                }
                if (_holes[x].Count == 0) continue;
                int y = _holes[x][0];
                _holes[x].RemoveAt(0);
                var cell = StepColumn(x, y, spawnAtTop);
                if (cell != null) moved.Add(cell);
                if (_holes[x].Count > 0) _timer[x] = DelayNotifyTime;
            }
            return moved;
        }

        /// <summary>Run gravity to rest (no animation delay). Used by the logic engine.</summary>
        public List<Cell> CollapseAll(Func<int, Cell> spawnAtTop)
        {
            var moved = new List<Cell>();
            bool any = true;
            int guard = _board.Width * _board.Height * 4;
            while (any && guard-- > 0)
            {
                any = false;
                for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    if (!_board.IsNormalGrid(x, y)) continue;
                    if (_board.HasHoney(x, y)) continue;
                    if (_board.Get(x, y) != null) continue;
                    var c = StepColumn(x, y + 1, spawnAtTop);
                    if (c != null)
                    {
                        moved.Add(c);
                        any = true;
                    }
                }
            }
            Clear();
            return moved;
        }

        Cell StepColumn(int x, int y, Func<int, Cell> spawnAtTop)
        {
            if (_board.IsOut(x, y))
            {
                return TrySpawn(x, spawnAtTop);
            }

            var cell = _board.Get(x, y);
            if (cell != null && cell.CanMoveDown)
            {
                return TryMoveDown(cell);
            }

            if (IsBlockedAbove(x, y))
            {
                var left = _board.Get(x - 1, y);
                if (left != null && left.CanMoveLeftRight && TryMoveDown(left) != null)
                    return left;
                var right = _board.Get(x + 1, y);
                if (right != null && right.CanMoveLeftRight && TryMoveDown(right) != null)
                    return right;
            }

            if (cell == null) return TrySpawn(x, spawnAtTop);
            return null;
        }

        Cell TryMoveDown(Cell cell)
        {
            int x = cell.Grid.x;
            int y = cell.Grid.y;
            int destY = y - 1;
            if (_board.IsOut(x, destY) || !_board.IsNormalGrid(x, destY)) return null;
            if (_board.HasHoney(x, destY)) return null;
            if (_board.Get(x, destY) != null) return null;
            _board.Set(x, y, null);
            _board.Set(x, destY, cell);
            cell.State = CellState.Move;
            Push(x, y);
            return cell;
        }

        Cell TrySpawn(int x, Func<int, Cell> spawnAtTop)
        {
            if (spawnAtTop == null) return null;
            int y = _board.Height - 1;
            while (y >= 0 && !_board.IsNormalGrid(x, y)) y--;
            if (y < 0) return null;
            if (_board.Get(x, y) != null) return null;
            var created = spawnAtTop(x);
            if (created == null) return null;
            created.State = CellState.Born;
            _board.Set(x, y, created);
            return created;
        }

        bool IsBlockedAbove(int x, int y)
        {
            for (int yy = y; yy < _board.Height; yy++)
            {
                var c = _board.Get(x, yy);
                if (c != null && !c.CanMoveDown) return true;
                if (c != null && c.CanMoveDown) return false;
            }
            return false;
        }
    }
}
