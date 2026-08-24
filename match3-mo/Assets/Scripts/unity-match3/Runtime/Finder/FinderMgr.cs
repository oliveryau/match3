using System.Collections.Generic;

namespace Match3
{
    public class FinderResult
    {
        public Cell Origin;
        public List<Cell> Cells = new List<Cell>();
        public List<GridPos> Grids = new List<GridPos>();
    }

    /// <summary>Port of FinderMgr + the finder implement classes.</summary>
    public class FinderMgr
    {
        public readonly Board Board;
        public readonly CellFinder Cells;
        public readonly GridFinder Grids;

        public FinderMgr(Board board)
        {
            Board = board;
            Cells = new CellFinder(board);
            Grids = new GridFinder(board);
        }

        public FinderResult FindCells(Cell cell, FinderId id, FinderCheckType check)
        {
            var r = new FinderResult { Origin = cell };
            if (cell == null) return r;
            const NeedSameType same = NeedSameType.SameType;
            switch (id)
            {
                case FinderId.SameTypeH:
                    r.Cells = Cells.GetAllCellsByDir(cell, ShapeTable.HDir, same, true, check);
                    break;
                case FinderId.SameTypeV:
                    r.Cells = Cells.GetAllCellsByDir(cell, ShapeTable.VDir, same, true, check);
                    break;
                case FinderId.AllH:
                    r.Cells = Cells.GetAllCellsByDir(cell, ShapeTable.HDir, NeedSameType.None, true, check);
                    break;
                case FinderId.AllV:
                    r.Cells = Cells.GetAllCellsByDir(cell, ShapeTable.VDir, NeedSameType.None, true, check);
                    break;
                case FinderId.TenWord:
                    r.Cells = Cells.GetCellsByShapes(cell, ShapeTable.TenDir, same, true, check);
                    if (r.Cells.Count == 0)
                        r.Cells = Cells.Merge(Cells.GetLineH(cell, same, true, check), Cells.GetLineV(cell, same, true, check));
                    break;
                case FinderId.TenWordNotSelf:
                    r.Cells = Cells.GetCellsByShapes(cell, ShapeTable.TenDir, same, false, check);
                    break;
                case FinderId.LShape:
                    r.Cells = Cells.GetShapeResult(cell, ShapeTable.LDir, same, true, check);
                    break;
                case FinderId.TWord:
                    r.Cells = Cells.GetShapeResult(cell, ShapeTable.TDir, same, true, check);
                    break;
                case FinderId.TianWord:
                    r.Cells = Cells.GetShapeResult(cell, ShapeTable.TianDir, same, true, check, 2);
                    break;
                case FinderId.OneH:
                    r.Cells = Cells.GetCellsByShapes(cell, ShapeTable.HDir, same, true, check);
                    break;
                case FinderId.OneV:
                    r.Cells = Cells.GetCellsByShapes(cell, ShapeTable.VDir, same, true, check);
                    break;
                case FinderId.All:
                    r.Cells = Cells.GetAll(check);
                    break;
                case FinderId.RoundTwo:
                    r.Cells = Cells.GetRoundCells(cell, 2, check);
                    break;
                case FinderId.RoundFour:
                    r.Cells = Cells.GetRoundCells(cell, 4, check);
                    break;
            }
            foreach (var c in r.Cells) r.Grids.Add(c.Grid);
            return r;
        }

        public FinderResult FindGrids(Cell cell, FinderId id, FinderCheckType check)
        {
            var r = new FinderResult { Origin = cell };
            if (cell == null) return r;
            const NeedSameType same = NeedSameType.SameType;
            switch (id)
            {
                case FinderId.SameTypeH:
                    r.Grids = Grids.GetAllCellsByDir(cell, ShapeTable.HDir, same, true, check);
                    break;
                case FinderId.SameTypeV:
                    r.Grids = Grids.GetAllCellsByDir(cell, ShapeTable.VDir, same, true, check);
                    break;
                case FinderId.AllH:
                    r.Grids = Grids.GetAllCellsByDir(cell, ShapeTable.HDir, NeedSameType.None, true, check);
                    break;
                case FinderId.AllV:
                    r.Grids = Grids.GetAllCellsByDir(cell, ShapeTable.VDir, NeedSameType.None, true, check);
                    break;
                case FinderId.TenWord:
                    r.Grids = Grids.GetCellsByShapes(cell, ShapeTable.TenDir, same, true, check);
                    if (r.Grids.Count == 0)
                        r.Grids = Grids.Merge(Grids.GetLineH(cell, same, true, check), Grids.GetLineV(cell, same, true, check));
                    break;
                case FinderId.TenWordNotSelf:
                    r.Grids = Grids.GetCellsByShapes(cell, ShapeTable.TenDir, same, false, check);
                    break;
                case FinderId.LShape:
                    r.Grids = Grids.GetShapeResult(cell, ShapeTable.LDir, same, true, check);
                    break;
                case FinderId.TWord:
                    r.Grids = Grids.GetShapeResult(cell, ShapeTable.TDir, same, true, check);
                    break;
                case FinderId.TianWord:
                    r.Grids = Grids.GetShapeResult(cell, ShapeTable.TianDir, same, true, check, 2);
                    break;
                case FinderId.RoundTwo:
                    r.Grids = Grids.GetRoundGrids(cell, 2, check);
                    break;
                case FinderId.All:
                    r.Grids = Grids.GetAll(check);
                    break;
                default:
                    var cells = FindCells(cell, id, check);
                    r.Grids = cells.Grids;
                    r.Cells = cells.Cells;
                    return r;
            }
            foreach (var g in r.Grids)
            {
                var c = Board.Get(g);
                if (c != null) r.Cells.Add(c);
            }
            return r;
        }
    }
}
