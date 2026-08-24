using System;

namespace Match3
{
    /// <summary>
    /// Integer board coordinate. Same convention as Cocos Vec2 grid:
    /// x column, y row, (0,0) bottom-left.
    /// </summary>
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.x + b.x, a.y + b.y);
        public static GridPos operator -(GridPos a, GridPos b) => new GridPos(a.x - b.x, a.y - b.y);
        public static bool operator ==(GridPos a, GridPos b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(GridPos a, GridPos b) => !(a == b);

        public bool Equals(GridPos other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (x * 397) ^ y;
        public override string ToString() => $"({x},{y})";

        public int ToId(int width) => y * width + x;

        public bool IsAdjacent(GridPos other)
        {
            int dx = Math.Abs(x - other.x);
            int dy = Math.Abs(y - other.y);
            return dx + dy == 1;
        }

        public bool IsNeighbor(GridPos other) => IsAdjacent(other);
    }
}
