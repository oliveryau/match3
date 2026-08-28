using System.Collections.Generic;

namespace Match3
{
    public class ResolveResult
    {
        public readonly HashSet<GridPos> Cleared = new HashSet<GridPos>();
        public readonly List<Cell> ClearedCells = new List<Cell>();
        public readonly List<Cell> SpawnedSpecials = new List<Cell>();
        public readonly List<Cell> Activated = new List<Cell>();
    }

    /// <summary>
    /// Port of actions/game CleanUpMissile / CleanUpPowderKeg / CleanUpPropeller / CleanUpLightBall
    /// and the two-special combo actions.
    /// </summary>
    public class SpecialResolver
    {
        readonly Board _board;
        readonly FinderMgr _finder;
        readonly System.Random _rng = new System.Random();

        public SpecialResolver(Board board, FinderMgr finder)
        {
            _board = board;
            _finder = finder;
        }

        public void ApplyMatch(CheckCellResult match, ResolveResult into)
        {
            if (match == null) return;
            if (match.Checker != null && match.Checker.ParamCount == 2)
            {
                ApplyCombo(match, into);
                return;
            }

            var origin = match.Origin;
            if (origin != null && origin.IsSpecial)
            {
                Activate(origin, into, origin.ColorId);
                return;
            }

            foreach (var c in match.Cells)
                ClearCell(c, into, true);

            if (match.SpawnSpecial != SpecialType.None && origin != null)
            {
                var special = Cell.CreateSpecial(match.SpawnSpecial, match.SpawnColor, match.SpawnAt);
                _board.Set(match.SpawnAt.x, match.SpawnAt.y, special);
                into.SpawnedSpecials.Add(special);
                into.Cleared.Remove(match.SpawnAt);
            }
        }

        public void Activate(Cell special, ResolveResult into, int colorHint)
        {
            if (special == null) return;
            var grid = special.Grid;
            if (!into.Cleared.Add(grid)) return;

            into.Activated.Add(special);
            var type = special.Special;
            special.State = CellState.Destroy;
            into.ClearedCells.Add(special);
            _board.Set(grid.x, grid.y, null);

            switch (type)
            {
                case SpecialType.HMissile:
                case SpecialType.VMissile:
                case SpecialType.PowderKeg:
                    // Gold peach / keg: clear a local area around the special.
                    ClearRadius(grid, 2, into);
                    break;
                case SpecialType.Propeller:
                    ClearCell(PickPropellerTarget(), into, true);
                    break;
                case SpecialType.LightBall:
                    ClearColor(colorHint > 0 ? colorHint : PickRandomColor(), into);
                    break;
            }
        }

        void ApplyCombo(CheckCellResult match, ResolveResult into)
        {
            var a = match.Cells[0];
            var b = match.Cells.Count > 1 ? match.Cells[1] : null;
            var name = match.Checker != null ? match.Checker.Name : "";
            switch (name)
            {
                case "TwoLightBall":
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    ClearAll(into);
                    break;
                case "LightBallAndOther":
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    ApplyLightBallWith(a, b, into);
                    break;
                case "TwoMissile":
                    ClearSquare(Midpoint(a.Grid, b != null ? b.Grid : a.Grid), 4, into);
                    break;
                case "TwoPowderKeg":
                    ClearSquare(a.Grid, 4, into);
                    break;
                case "TwoPropeller":
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    ClearCell(PickPropellerTarget(), into, true);
                    ClearCell(PickPropellerTarget(), into, true);
                    ClearCell(PickPropellerTarget(), into, true);
                    break;
                case "MissileAndKeg":
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    ClearLine(a.Grid.y - 1, true, into);
                    ClearLine(a.Grid.y, true, into);
                    ClearLine(a.Grid.y + 1, true, into);
                    ClearLine(a.Grid.x, false, into);
                    break;
                case "PropellerAndMissile":
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    var t = PickPropellerTarget();
                    if (t != null)
                    {
                        ClearCell(t, into, true);
                        ClearLine(t.Grid.y, true, into);
                    }
                    break;
                case "PropellerAndKeg":
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    var t2 = PickPropellerTarget();
                    if (t2 != null) ClearRadius(t2.Grid, 2, into);
                    break;
                default:
                    ClearCell(a, into, false);
                    ClearCell(b, into, false);
                    Activate(a, into, a.ColorId);
                    if (b != null) Activate(b, into, b.ColorId);
                    break;
            }
        }

        void ApplyLightBallWith(Cell a, Cell b, ResolveResult into)
        {
            var ball = a.Special == SpecialType.LightBall ? a : b;
            var other = ball == a ? b : a;
            if (other == null)
            {
                ClearColor(PickRandomColor(), into);
                return;
            }
            if (other.IsSpecial)
            {
                int color = PickRandomColor();
                var clones = new List<Cell>();
                foreach (var c in _board.CollectMain(x => x.IsNormal && x.ColorId == color))
                    clones.Add(c);
                foreach (var c in clones)
                {
                    var spawned = Cell.CreateSpecial(other.Special, color, c.Grid);
                    _board.Set(c.Grid.x, c.Grid.y, spawned);
                    Activate(spawned, into, color);
                }
            }
            else
            {
                ClearColor(other.ColorId, into);
            }
        }

        void ClearLine(int index, bool horizontal, ResolveResult into)
        {
            if (horizontal)
            {
                for (int x = 0; x < _board.Width; x++)
                    ClearCell(_board.Get(x, index), into, true);
            }
            else
            {
                for (int y = 0; y < _board.Height; y++)
                    ClearCell(_board.Get(index, y), into, true);
            }
        }

        void ClearRadius(GridPos center, int radius, ResolveResult into)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                var c = _board.Get(center.x + dx, center.y + dy);
                ClearCell(c, into, true);
            }
        }

        /// <summary>Axis-aligned square clear (e.g. size 4 → 4x4 cells), centered on <paramref name="center"/>.</summary>
        public void ClearSquare(GridPos center, int size, ResolveResult into)
        {
            if (size <= 0) return;
            int startX = center.x - (size - 1) / 2;
            int startY = center.y - (size - 1) / 2;
            for (int x = startX; x < startX + size; x++)
            for (int y = startY; y < startY + size; y++)
            {
                var c = _board.Get(x, y);
                if (c == null) continue;
                // Destroy specials in the blast without chaining their own smaller clears.
                ForceDestroy(c, into);
            }
        }

        public static GridPos Midpoint(GridPos a, GridPos b)
        {
            return new GridPos((a.x + b.x) / 2, (a.y + b.y) / 2);
        }

        void ForceDestroy(Cell cell, ResolveResult into)
        {
            if (cell == null) return;
            var grid = cell.Grid;
            if (!into.Cleared.Add(grid)) return;
            cell.State = CellState.Destroy;
            into.ClearedCells.Add(cell);
            _board.Set(grid.x, grid.y, null);
        }

        void ClearColor(int colorId, ResolveResult into)
        {
            var list = _board.CollectMain(c => c.IsNormal && c.ColorId == colorId);
            foreach (var c in list) ClearCell(c, into, true);
        }

        void ClearAll(ResolveResult into)
        {
            foreach (var c in _board.CollectMain(_ => true))
                ClearCell(c, into, true);
        }

        void ClearCell(Cell cell, ResolveResult into, bool hitNeighbors)
        {
            if (cell == null) return;
            var grid = cell.Grid;

            // Specials detonate (and may chain) instead of a plain clear.
            if (cell.IsSpecial && cell.Special != SpecialType.None)
            {
                Activate(cell, into, cell.ColorId);
                return;
            }

            if (!into.Cleared.Add(grid)) return;

            if (cell.IsObstacle)
            {
                cell.Hp--;
                if (cell.Hp > 0)
                {
                    into.Cleared.Remove(grid);
                    return;
                }
            }

            var grass = _board.Get(grid.x, grid.y, GridLayer.Grass);
            if (grass != null)
            {
                grass.Hp--;
                if (grass.Hp <= 0) _board.Set(grid.x, grid.y, null, GridLayer.Grass);
            }

            cell.State = CellState.Destroy;
            into.ClearedCells.Add(cell);
            _board.Set(grid.x, grid.y, null);

            if (hitNeighbors)
                HitNeighbors(grid, into);
        }

        void HitNeighbors(GridPos grid, ResolveResult into)
        {
            foreach (var n in ShapeTable.Neighbors4(grid))
            {
                var c = _board.Get(n);
                if (c != null && c.CanCleanUpByRoundItem)
                    ClearCell(c, into, false);
            }
        }

        Cell PickPropellerTarget()
        {
            var obstacles = _board.CollectMain(c => c.IsObstacle);
            if (obstacles.Count > 0) return obstacles[_rng.Next(obstacles.Count)];
            var normals = _board.CollectMain(c => c.IsNormal && c.CanFind());
            if (normals.Count > 0) return normals[_rng.Next(normals.Count)];
            return null;
        }

        int PickRandomColor()
        {
            var normals = _board.CollectMain(c => c.IsNormal);
            if (normals.Count == 0) return 1;
            return normals[_rng.Next(normals.Count)].ColorId;
        }
    }
}
