using System.Collections.Generic;

namespace Match3
{
    /// <summary>Port of finder/helper/BaseFinder.ts</summary>
    public class BaseFinder
    {
        protected readonly Board Board;

        public BaseFinder(Board board)
        {
            Board = board;
        }

        public bool Passes(Cell cell, FinderCheckType check)
        {
            if (cell == null) return false;
            switch (check)
            {
                case FinderCheckType.CanFind: return cell.CanFind();
                case FinderCheckType.CanUse: return cell.CanUse();
                case FinderCheckType.CanFindAndNormal: return cell.CanFind() && cell.IsNormal;
                case FinderCheckType.CanFindAndSpecial: return cell.CanFind() && cell.IsSpecial;
                case FinderCheckType.CanUseAndNormal: return cell.CanUse() && cell.IsNormal;
                default: return false;
            }
        }

        public bool IsOut(int x, int y) => Board.IsOut(x, y);

        public bool SameType(Cell a, Cell b, NeedSameType need)
        {
            if (need == NeedSameType.None) return true;
            if (a == null || b == null) return false;
            if (a.ObjectType == b.ObjectType) return true;
            if (need == NeedSameType.SimilarType)
            {
                return a.SimilarTypes.Contains((int)b.ObjectType)
                       || b.SimilarTypes.Contains((int)a.ObjectType);
            }
            return false;
        }

        /// <summary>Axis-aligned diamond used by powder keg / propeller splash.</summary>
        public List<GridPos> GetRoundPositions(GridPos grid, int radius)
        {
            var h = GetRoundH(grid, radius);
            var result = new List<GridPos>(h);
            foreach (var p in h)
                result.AddRange(GetRoundV(p, radius));
            result.AddRange(GetRoundV(grid, radius));
            return result;
        }

        public List<GridPos> GetRoundH(GridPos grid, int radius)
        {
            var list = new List<GridPos>();
            for (int i = 1; i <= radius; i++)
            {
                int x = grid.x - i;
                if (IsOut(x, grid.y)) break;
                list.Add(new GridPos(x, grid.y));
            }
            for (int i = 1; i <= radius; i++)
            {
                int x = grid.x + i;
                if (IsOut(x, grid.y)) break;
                list.Add(new GridPos(x, grid.y));
            }
            return list;
        }

        public List<GridPos> GetRoundV(GridPos grid, int radius)
        {
            var list = new List<GridPos>();
            for (int i = 1; i <= radius; i++)
            {
                int y = grid.y - i;
                if (IsOut(grid.x, y)) break;
                list.Add(new GridPos(grid.x, y));
            }
            for (int i = 1; i <= radius; i++)
            {
                int y = grid.y + i;
                if (IsOut(grid.x, y)) break;
                list.Add(new GridPos(grid.x, y));
            }
            return list;
        }
    }
}
