using System.Collections.Generic;

namespace Match3
{
    public class CheckCellResult
    {
        public CheckerDef Checker;
        public List<Cell> Cells = new List<Cell>();
        public Cell Origin;
        public GridPos SpawnAt;
        public SpecialType SpawnSpecial;
        public int SpawnColor;
    }

    public class CheckGridResult
    {
        public CheckerDef Checker;
        public List<GridPos> Grids = new List<GridPos>();
        public Cell Origin;
        public GridPos SpawnAt;
        public SpecialType SpawnSpecial;
        public int SpawnColor;
    }

    public enum CheckerKind
    {
        FinderGroup,
        FinderOfColor,
        SwapPair,
        SwapOneIsType,
        IsType
    }

    public class CheckerDef
    {
        public int Id;
        public string Name;
        public CheckerKind Kind;
        public FinderId[] Finders;
        public int MinCount;
        public int ParamCount = 1;
        public ObjectType[] TypesA;
        public ObjectType[] TypesB;
        public SpecialType SpawnSpecial;
        public int ColorFilter = -1;
        public bool ColorFromOrigin = true;
    }

    /// <summary>Port of BaseChecker + FinderItemChecker / FinderIsCellItemChecker / ExchangeItemChecker / ExchangeItemAndOtherChecker.</summary>
    public static class Checkers
    {
        public static CheckCellResult Run(CheckerDef def, List<Cell> input, FinderMgr finder, FinderCheckType check)
        {
            if (input == null || input.Count != def.ParamCount) return null;
            switch (def.Kind)
            {
                case CheckerKind.FinderGroup: return FinderGroup(def, input[0], finder, check);
                case CheckerKind.FinderOfColor: return FinderOfColor(def, input[0], finder, check);
                case CheckerKind.SwapPair: return SwapPair(def, input);
                case CheckerKind.SwapOneIsType: return SwapOneIsType(def, input);
                case CheckerKind.IsType: return IsType(def, input[0]);
            }
            return null;
        }

        static CheckCellResult FinderGroup(CheckerDef def, Cell origin, FinderMgr finder, FinderCheckType check)
        {
            if (origin == null || !origin.CanMerge) return null;
            foreach (var fid in def.Finders)
            {
                var found = finder.FindCells(origin, fid, check);
                if (found.Cells.Count >= def.MinCount)
                {
                    return Make(def, origin, found.Cells);
                }
            }
            return null;
        }

        static CheckCellResult FinderOfColor(CheckerDef def, Cell origin, FinderMgr finder, FinderCheckType check)
        {
            if (origin == null || !origin.CanMerge) return null;
            if (def.ColorFilter >= 0 && origin.ColorId != def.ColorFilter) return null;
            return FinderGroup(def, origin, finder, check);
        }

        static CheckCellResult SwapPair(CheckerDef def, List<Cell> input)
        {
            var a = input[0];
            var b = input[1];
            if (a == null || b == null) return null;
            bool match =
                Contains(def.TypesA, a.ObjectType) && Contains(def.TypesB, b.ObjectType)
                || Contains(def.TypesB, a.ObjectType) && Contains(def.TypesA, b.ObjectType);
            if (!match) return null;
            return new CheckCellResult
            {
                Checker = def,
                Origin = a,
                Cells = new List<Cell> { a, b },
                SpawnAt = a.Grid,
                SpawnSpecial = def.SpawnSpecial,
                SpawnColor = a.ColorId
            };
        }

        static CheckCellResult SwapOneIsType(CheckerDef def, List<Cell> input)
        {
            var a = input[0];
            var b = input[1];
            if (a == null || b == null) return null;
            bool aHit = Contains(def.TypesA, a.ObjectType);
            bool bHit = Contains(def.TypesA, b.ObjectType);
            if ((aHit && !bHit && b.IsNormalOrSpecial) || (!aHit && bHit && a.IsNormalOrSpecial))
            {
                return new CheckCellResult
                {
                    Checker = def,
                    Origin = a,
                    Cells = new List<Cell> { a, b },
                    SpawnAt = a.Grid,
                    SpawnSpecial = def.SpawnSpecial,
                    SpawnColor = a.ColorId
                };
            }
            return null;
        }

        static CheckCellResult Make(CheckerDef def, Cell origin, List<Cell> cells)
        {
            var special = def.SpawnSpecial;
            if (special == SpecialType.None && def.MinCount >= 5) special = SpecialType.LightBall;
            return new CheckCellResult
            {
                Checker = def,
                Origin = origin,
                Cells = cells,
                SpawnAt = origin.Grid,
                SpawnSpecial = special,
                SpawnColor = origin.ColorId
            };
        }

        static CheckCellResult IsType(CheckerDef def, Cell origin)
        {
            if (origin == null) return null;
            if (!Contains(def.TypesA, origin.ObjectType)) return null;
            return new CheckCellResult
            {
                Checker = def,
                Origin = origin,
                Cells = new List<Cell> { origin },
                SpawnAt = origin.Grid,
                SpawnSpecial = def.SpawnSpecial,
                SpawnColor = origin.ColorId
            };
        }

        static bool Contains(ObjectType[] arr, ObjectType t)
        {
            if (arr == null) return false;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == t) return true;
            return false;
        }
    }
}
