using System;
using System.Collections.Generic;

namespace Match3
{
    /// <summary>Port of checker/CheckHelper.ts — swap preview, hint scan, dead-board test.</summary>
    public class CheckHelper
    {
        readonly Board _board;
        readonly FinderMgr _finder;
        readonly CheckMgr _checks;
        readonly List<CheckGridResult> _hints = new List<CheckGridResult>();

        public CheckHelper(Board board, FinderMgr finder, CheckMgr checks)
        {
            _board = board;
            _finder = finder;
            _checks = checks;
        }

        public IReadOnlyList<CheckGridResult> Hints => _hints;

        public bool CanTouchStart(GridPos grid)
        {
            if (_board.IsOut(grid) || _board.HasHoney(grid.x, grid.y)) return false;
            var cell = _board.GetTopMost(grid.x, grid.y);
            return cell != null && cell.CanFind() && cell.CanMoveDown;
        }

        public bool CanTouchMove(GridPos grid)
        {
            if (_board.IsOut(grid)) return false;
            var cell = _board.GetTopMost(grid.x, grid.y);
            if (cell != null) return cell.CanFind();
            return _board.IsNormalGrid(grid.x, grid.y);
        }

        public void Swap(GridPos a, GridPos b) => _board.Swap(a, b);

        public List<CheckCellResult> CheckSwap(GridPos a, GridPos b, FinderCheckType check, bool markUsed)
        {
            var results = new List<CheckCellResult>();
            if (!CanTouchStart(a)) return results;
            var cell1 = _board.Get(a);
            if (cell1 == null || !cell1.CanFind()) return results;
            if (!CanTouchMove(b)) return results;

            var cell2 = _board.Get(b);
            Swap(a, b);
            cell1 = _board.Get(b);
            cell2 = _board.Get(a);

            if (cell1 != null && cell2 != null)
            {
                var both = _checks.CheckStartForCells(new List<Cell> { cell1, cell2 }, check);
                if (both != null)
                {
                    MaybeMark(both, markUsed);
                    results.Add(both);
                }
                else
                {
                    var r1 = _checks.CheckStartForCells(new List<Cell> { cell1 }, check);
                    var r2 = _checks.CheckStartForCells(new List<Cell> { cell2 }, check);
                    if (r1 != null) { MaybeMark(r1, markUsed); results.Add(r1); }
                    if (r2 != null) { MaybeMark(r2, markUsed); results.Add(r2); }
                }
            }
            else if (cell1 != null)
            {
                var r1 = _checks.CheckStartForCells(new List<Cell> { cell1 }, check);
                if (r1 != null) { MaybeMark(r1, markUsed); results.Add(r1); }
            }

            Swap(a, b);
            return results;
        }

        public CheckCellResult CheckTap(GridPos grid, FinderCheckType check, bool markUsed)
        {
            if (!CanTouchStart(grid)) return null;
            var cell = _board.Get(grid);
            if (cell == null) return null;
            var r = _checks.CheckStartForCells(new List<Cell> { cell }, check);
            if (r != null) MaybeMark(r, markUsed);
            return r;
        }

        public CheckCellResult CheckAfterDrop(Cell cell, FinderCheckType check, bool markUsed)
        {
            var r = _checks.CheckDrop(new List<Cell> { cell }, check);
            if (r != null) MaybeMark(r, markUsed);
            return r;
        }

        /// <summary>Scan the board for any legal swap. Port of checkAllCells / checkStart.</summary>
        public CheckResultState ScanHints()
        {
            foreach (var h in _hints)
                h.Grids.Clear();
            _hints.Clear();

            var seen = new HashSet<int>();
            for (int y = _board.Height - 1; y >= 0; y--)
            for (int x = 0; x < _board.Width; x++)
            {
                if (_board.HasHoney(x, y)) continue;
                var cell = _board.Get(x, y);
                if (cell == null || !cell.CanOperate) continue;
                int id1 = _board.GetGridId(cell.Grid);
                if (!seen.Add(id1)) continue;

                var adj = _finder.FindCells(cell, FinderId.TenWordNotSelf, FinderCheckType.CanFind);
                foreach (var other in adj.Cells)
                {
                    if (other == null || !other.CanOperate || ReferenceEquals(other, cell)) continue;
                    int id2 = _board.GetGridId(other.Grid);
                    if (seen.Contains(id2)) continue;
                    var found = CheckSwap(cell.Grid, other.Grid, FinderCheckType.CanFind, false);
                    if (found.Count > 0)
                    {
                        _hints.Add(new CheckGridResult
                        {
                            Origin = cell,
                            Grids = new List<GridPos> { cell.Grid, other.Grid },
                            SpawnAt = cell.Grid
                        });
                    }
                }
            }
            return _hints.Count > 0 ? CheckResultState.Yes : CheckResultState.No;
        }

        public bool HasImmediateMatch()
        {
            foreach (var cell in _board.AllMain())
            {
                if (!cell.CanMerge || !cell.CanFind()) continue;
                var r = _checks.CheckDrop(new List<Cell> { cell }, FinderCheckType.CanFind);
                if (r != null) return true;
            }
            return false;
        }

        void MaybeMark(CheckCellResult r, bool mark)
        {
            if (!mark) return;
            int batch = _board.NextBatchId();
            r.Origin.BatchId = batch;
            foreach (var c in r.Cells)
            {
                if (c == null) continue;
                c.BatchId = batch;
                c.BeCleanUpBatchId = batch;
            }
        }
    }
}
