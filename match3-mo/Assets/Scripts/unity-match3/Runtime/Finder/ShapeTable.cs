using System.Collections.Generic;

namespace Match3
{
    /// <summary>
    /// Hardcoded shape offsets. Original ShapeMgr loaded these from ShapeItemModel Excel.
    /// Offsets are [dx, dy, dx, dy, ...] per orientation, (0,0) bottom-left, +y up.
    /// </summary>
    public static class ShapeTable
    {
        public static readonly int[][] HDir =
        {
            new[] { -1, 0 },
            new[] { 1, 0 }
        };

        public static readonly int[][] VDir =
        {
            new[] { 0, -1 },
            new[] { 0, 1 }
        };

        // Cross: left, right, up, down
        public static readonly int[][] TenDir =
        {
            new[] { -1, 0, 1, 0, 0, 1, 0, -1 }
        };

        // 2x2 square (田) — four orientations around the origin
        public static readonly int[][] TianDir =
        {
            new[] { -1, 0, -1, 1, 0, 1 },
            new[] { 1, 0, 1, 1, 0, 1 },
            new[] { -1, 0, -1, -1, 0, -1 },
            new[] { 1, 0, 1, -1, 0, -1 }
        };

        // L of 5 tiles (3+3 sharing corner)
        public static readonly int[][] LDir =
        {
            new[] { 0, 1, 0, 2, -1, 0, -2, 0 },
            new[] { 0, 1, 0, 2, 1, 0, 2, 0 },
            new[] { 0, -1, 0, -2, -1, 0, -2, 0 },
            new[] { 0, -1, 0, -2, 1, 0, 2, 0 }
        };

        // T of 5 tiles — stem + bar, several sample points from GameDefine Direction[5]
        public static readonly int[][] TDir =
        {
            new[] { 0, -1, 0, 1, -1, 0, -2, 0 },
            new[] { 0, -1, 0, 1, 1, 0, 2, 0 },
            new[] { -1, 0, 1, 0, 0, 1, 0, 2 },
            new[] { -1, 0, 1, 0, 0, -1, 0, -2 }
        };

        public static int[][] Get(FinderId id)
        {
            switch (id)
            {
                case FinderId.SameTypeH:
                case FinderId.AllH:
                case FinderId.OneH: return HDir;
                case FinderId.SameTypeV:
                case FinderId.AllV:
                case FinderId.OneV: return VDir;
                case FinderId.TenWord:
                case FinderId.TenWordNotSelf: return TenDir;
                case FinderId.LShape: return LDir;
                case FinderId.TWord: return TDir;
                case FinderId.TianWord: return TianDir;
                default: return TenDir;
            }
        }

        public static IEnumerable<GridPos> Neighbors4(GridPos p)
        {
            yield return new GridPos(p.x - 1, p.y);
            yield return new GridPos(p.x + 1, p.y);
            yield return new GridPos(p.x, p.y - 1);
            yield return new GridPos(p.x, p.y + 1);
        }

        public static IEnumerable<GridPos> SquareAround(GridPos center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                yield return new GridPos(center.x + dx, center.y + dy);
            }
        }
    }
}
