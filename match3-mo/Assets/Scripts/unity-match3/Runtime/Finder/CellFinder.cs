using System.Collections.Generic;

namespace Match3
{
    /// <summary>Port of finder/helper/CellFinder.ts — searches cells, not just coordinates.</summary>
    public class CellFinder : BaseFinder
    {
        public CellFinder(Board board) : base(board) { }

        public List<Cell> GetCellsByShape(Cell cell, int[] offsets, NeedSameType needSame, bool hasSelf, FinderCheckType check)
        {
            var result = new List<Cell>();
            if (hasSelf && Passes(cell, check)) result.Add(cell);

            var originType = cell.ObjectType;
            int count = offsets.Length / 2;
            for (int i = 0; i < count; i++)
            {
                int x = cell.Grid.x + offsets[i * 2];
                int y = cell.Grid.y + offsets[i * 2 + 1];
                if (IsOut(x, y))
                {
                    if (needSame != NeedSameType.None) return new List<Cell>();
                    continue;
                }

                var target = Board.GetTopMost(x, y);
                if (target != null && Passes(target, check))
                {
                    if (needSame != NeedSameType.None)
                    {
                        if (!SameType(cell, target, needSame) && target.ObjectType != originType)
                            return new List<Cell>();
                        result.Add(target);
                    }
                    else result.Add(target);
                }
                else if (needSame != NeedSameType.None)
                {
                    return new List<Cell>();
                }
            }
            return result;
        }

        public List<Cell> GetCellsByShapes(Cell cell, int[][] shapes, NeedSameType needSame, bool hasSelf, FinderCheckType check)
        {
            foreach (var shape in shapes)
            {
                var list = GetCellsByShape(cell, shape, needSame, hasSelf, check);
                if (list.Count > 0) return list;
            }
            return new List<Cell>();
        }

        /// <summary>Walk each direction until mismatch / empty. Port of getAllCellsByDir.</summary>
        public List<Cell> GetAllCellsByDir(Cell cell, int[][] directions, NeedSameType needSame, bool hasSelf, FinderCheckType check)
        {
            var result = new List<Cell>();
            if (hasSelf && Passes(cell, check)) result.Add(cell);

            foreach (var dir in directions)
            {
                int step = 1;
                int x = cell.Grid.x + step * dir[0];
                int y = cell.Grid.y + step * dir[1];
                while (!IsOut(x, y))
                {
                    var temp = Board.GetTopMost(x, y);
                    if (temp != null && Passes(temp, check))
                    {
                        if (needSame != NeedSameType.None)
                        {
                            if (!SameType(cell, temp, needSame) && temp.ObjectType != cell.ObjectType)
                                break;
                            result.Add(temp);
                        }
                        else result.Add(temp);
                    }
                    else if (needSame != NeedSameType.None)
                    {
                        break;
                    }

                    step++;
                    x = cell.Grid.x + step * dir[0];
                    y = cell.Grid.y + step * dir[1];
                }
            }
            return result;
        }

        public List<Cell> Merge(params List<Cell>[] lists)
        {
            var map = new Dictionary<int, Cell>();
            foreach (var list in lists)
            {
                if (list == null) continue;
                foreach (var c in list)
                {
                    int id = Board.GetGridId(c.Grid);
                    if (!map.ContainsKey(id)) map[id] = c;
                }
            }
            return new List<Cell>(map.Values);
        }

        public List<Cell> GetLineH(Cell cell, NeedSameType need, bool hasSelf, FinderCheckType check)
        {
            return GetAllCellsByDir(cell, ShapeTable.HDir, need, hasSelf, check);
        }

        public List<Cell> GetLineV(Cell cell, NeedSameType need, bool hasSelf, FinderCheckType check)
        {
            return GetAllCellsByDir(cell, ShapeTable.VDir, need, hasSelf, check);
        }

        /// <summary>Port of getShapeResult — line of 3+ then test L/T/田 offsets.</summary>
        public List<Cell> GetShapeResult(Cell cell, int[][] shapeOffsets, NeedSameType need, bool hasSelf, FinderCheckType check, int limit = 3)
        {
            var h = GetLineH(cell, need, hasSelf, check);
            var v = GetLineV(cell, need, hasSelf, check);
            if (h.Count >= limit && v.Count >= limit)
            {
                if (h.Count >= v.Count)
                {
                    var r = Compare(h, shapeOffsets, need, hasSelf, check);
                    if (r.Count > 0) return r;
                    r = Compare(v, shapeOffsets, need, hasSelf, check);
                    if (r.Count > 0) return r;
                }
                else
                {
                    var r = Compare(v, shapeOffsets, need, hasSelf, check);
                    if (r.Count > 0) return r;
                    r = Compare(h, shapeOffsets, need, hasSelf, check);
                    if (r.Count > 0) return r;
                }
            }
            else
            {
                if (h.Count >= limit)
                {
                    var r = Compare(h, shapeOffsets, need, hasSelf, check);
                    if (r.Count > 0) return r;
                }
                if (v.Count >= limit)
                {
                    var r = Compare(v, shapeOffsets, need, hasSelf, check);
                    if (r.Count > 0) return r;
                }
            }
            return new List<Cell>();
        }

        List<Cell> Compare(List<Cell> line, int[][] shapes, NeedSameType need, bool hasSelf, FinderCheckType check)
        {
            foreach (var c in line)
            {
                var temp = GetCellsByShapes(c, shapes, need, hasSelf, check);
                if (temp.Count > 0) return Merge(temp, line);
            }
            return new List<Cell>();
        }

        public List<Cell> GetAll(FinderCheckType check)
        {
            return Board.CollectMain(c => Passes(c, check));
        }

        public List<Cell> GetAllNormal(FinderCheckType check)
        {
            return Board.CollectMain(c => c.IsNormal && Passes(c, check));
        }

        public List<Cell> GetRoundCells(Cell cell, int radius, FinderCheckType check)
        {
            var list = new List<Cell>();
            foreach (var p in GetRoundPositions(cell.Grid, radius))
            {
                var t = Board.GetTopMost(p.x, p.y);
                if (t != null && Passes(t, check)) list.Add(t);
            }
            return list;
        }
    }
}
