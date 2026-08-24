using System.Collections.Generic;

namespace Match3
{
    /// <summary>Port of finder/helper/GridFinder.ts — same searches, returning coordinates.</summary>
    public class GridFinder : BaseFinder
    {
        public GridFinder(Board board) : base(board) { }

        public List<GridPos> GetCellsByShape(Cell cell, int[] offsets, NeedSameType needSame, bool hasSelf, FinderCheckType check)
        {
            var result = new List<GridPos>();
            if (hasSelf && Passes(cell, check)) result.Add(cell.Grid);

            var originType = cell.ObjectType;
            int count = offsets.Length / 2;
            for (int i = 0; i < count; i++)
            {
                int x = cell.Grid.x + offsets[i * 2];
                int y = cell.Grid.y + offsets[i * 2 + 1];
                if (IsOut(x, y))
                {
                    if (needSame != NeedSameType.None) return new List<GridPos>();
                    continue;
                }

                var target = Board.GetTopMost(x, y);
                if (target != null && Passes(target, check))
                {
                    if (needSame != NeedSameType.None)
                    {
                        if (!SameType(cell, target, needSame) && target.ObjectType != originType)
                            return new List<GridPos>();
                        result.Add(target.Grid);
                    }
                    else result.Add(target.Grid);
                }
                else if (needSame != NeedSameType.None)
                {
                    return new List<GridPos>();
                }
            }
            return result;
        }

        public List<GridPos> GetCellsByShapes(Cell cell, int[][] shapes, NeedSameType needSame, bool hasSelf, FinderCheckType check)
        {
            foreach (var shape in shapes)
            {
                var list = GetCellsByShape(cell, shape, needSame, hasSelf, check);
                if (list.Count > 0) return list;
            }
            return new List<GridPos>();
        }

        public List<GridPos> GetAllCellsByDir(Cell cell, int[][] directions, NeedSameType needSame, bool hasSelf, FinderCheckType check)
        {
            var result = new List<GridPos>();
            if (hasSelf && Passes(cell, check)) result.Add(cell.Grid);

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
                            result.Add(temp.Grid);
                        }
                        else result.Add(temp.Grid);
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

        public List<GridPos> Merge(params List<GridPos>[] lists)
        {
            var map = new Dictionary<int, GridPos>();
            foreach (var list in lists)
            {
                if (list == null) continue;
                foreach (var g in list)
                {
                    int id = Board.GetGridId(g);
                    if (!map.ContainsKey(id)) map[id] = g;
                }
            }
            return new List<GridPos>(map.Values);
        }

        public List<GridPos> GetLineH(Cell cell, NeedSameType need, bool hasSelf, FinderCheckType check)
        {
            return GetAllCellsByDir(cell, ShapeTable.HDir, need, hasSelf, check);
        }

        public List<GridPos> GetLineV(Cell cell, NeedSameType need, bool hasSelf, FinderCheckType check)
        {
            return GetAllCellsByDir(cell, ShapeTable.VDir, need, hasSelf, check);
        }

        public List<GridPos> GetShapeResult(Cell cell, int[][] shapeOffsets, NeedSameType need, bool hasSelf, FinderCheckType check, int limit = 3)
        {
            var h = GetLineH(cell, need, hasSelf, check);
            var v = GetLineV(cell, need, hasSelf, check);
            if (h.Count >= limit && v.Count >= limit)
            {
                var first = h.Count >= v.Count ? h : v;
                var second = h.Count >= v.Count ? v : h;
                var r = Compare(first, shapeOffsets, need, hasSelf, check);
                if (r != null && r.Count > 0) return r;
                r = Compare(second, shapeOffsets, need, hasSelf, check);
                if (r != null && r.Count > 0) return r;
            }
            else
            {
                if (h.Count >= limit)
                {
                    var r = Compare(h, shapeOffsets, need, hasSelf, check);
                    if (r != null && r.Count > 0) return r;
                }
                if (v.Count >= limit)
                {
                    var r = Compare(v, shapeOffsets, need, hasSelf, check);
                    if (r != null && r.Count > 0) return r;
                }
            }
            return new List<GridPos>();
        }

        List<GridPos> Compare(List<GridPos> line, int[][] shapes, NeedSameType need, bool hasSelf, FinderCheckType check)
        {
            foreach (var g in line)
            {
                var c = Board.Get(g);
                if (c == null) continue;
                var temp = GetCellsByShapes(c, shapes, need, hasSelf, check);
                if (temp.Count > 0) return Merge(temp, line);
            }
            return null;
        }

        public List<GridPos> GetRoundGrids(Cell cell, int radius, FinderCheckType check)
        {
            var list = new List<GridPos>();
            foreach (var p in GetRoundPositions(cell.Grid, radius))
            {
                var t = Board.GetTopMost(p.x, p.y);
                if (t != null && Passes(t, check)) list.Add(t.Grid);
            }
            return list;
        }

        public List<GridPos> GetAll(FinderCheckType check)
        {
            var list = new List<GridPos>();
            for (int x = 0; x < Board.Width; x++)
            for (int y = 0; y < Board.Height; y++)
            {
                var c = Board.GetTopMost(x, y);
                if (c != null && Passes(c, check)) list.Add(c.Grid);
            }
            return list;
        }
    }
}
