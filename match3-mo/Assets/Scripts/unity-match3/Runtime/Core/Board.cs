using System;
using System.Collections.Generic;

namespace Match3
{
    /// <summary>
    /// Board storage for the main layer plus optional grass/honey overlays.
    /// Port of GameHelper + IGridBgLayer.
    /// </summary>
    public class Board
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        Cell[][] _main;
        Cell[][] _grass;
        Cell[][] _honey;
        GridType[][] _gridType;
        int _batch;

        public void Init(int width, int height)
        {
            Width = width;
            Height = height;
            _main = MakeLayer();
            _grass = MakeLayer();
            _honey = MakeLayer();
            _gridType = new GridType[width][];
            for (int x = 0; x < width; x++)
            {
                _gridType[x] = new GridType[height];
                for (int y = 0; y < height; y++)
                    _gridType[x][y] = GridType.NormalGrid;
            }
        }

        Cell[][] MakeLayer()
        {
            var layer = new Cell[Width][];
            for (int x = 0; x < Width; x++)
                layer[x] = new Cell[Height];
            return layer;
        }

        public bool IsOut(int x, int y)
        {
            return x < 0 || y < 0 || x >= Width || y >= Height;
        }

        public bool IsOut(GridPos p) => IsOut(p.x, p.y);

        public int GetGridId(GridPos p) => p.y * Width + p.x;

        public int GetGridId(int x, int y) => y * Width + x;

        public GridType GetGridType(int x, int y)
        {
            if (IsOut(x, y)) return GridType.NullGrid;
            return _gridType[x][y];
        }

        public void SetGridType(int x, int y, GridType type)
        {
            if (!IsOut(x, y)) _gridType[x][y] = type;
        }

        public bool IsNormalGrid(int x, int y)
        {
            return !IsOut(x, y) && _gridType[x][y] == GridType.NormalGrid;
        }

        public Cell Get(int x, int y, GridLayer layer = GridLayer.Main)
        {
            if (IsOut(x, y)) return null;
            switch (layer)
            {
                case GridLayer.Grass: return _grass[x][y];
                case GridLayer.Honey: return _honey[x][y];
                default: return _main[x][y];
            }
        }

        public Cell Get(GridPos p, GridLayer layer = GridLayer.Main) => Get(p.x, p.y, layer);

        /// <summary>Search from honey down to grass, matching getCellByXYForStepNoCheck.</summary>
        public Cell GetTopMost(int x, int y)
        {
            return Get(x, y, GridLayer.Honey)
                   ?? Get(x, y, GridLayer.Main)
                   ?? Get(x, y, GridLayer.Grass);
        }

        public bool HasHoney(int x, int y) => Get(x, y, GridLayer.Honey) != null;

        public void Set(Cell cell, GridLayer layer = GridLayer.Main)
        {
            if (cell == null) return;
            Set(cell.Grid.x, cell.Grid.y, cell, layer);
        }

        public void Set(int x, int y, Cell cell, GridLayer layer = GridLayer.Main)
        {
            if (IsOut(x, y)) return;
            if (cell != null) cell.Grid = new GridPos(x, y);
            switch (layer)
            {
                case GridLayer.Grass: _grass[x][y] = cell; break;
                case GridLayer.Honey: _honey[x][y] = cell; break;
                default: _main[x][y] = cell; break;
            }
        }

        public void Clear(int x, int y, GridLayer layer = GridLayer.Main)
        {
            Set(x, y, null, layer);
        }

        public void Swap(GridPos a, GridPos b)
        {
            var ca = Get(a);
            var cb = Get(b);
            Set(a.x, a.y, cb);
            Set(b.x, b.y, ca);
        }

        public IEnumerable<Cell> AllMain()
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var c = _main[x][y];
                if (c != null) yield return c;
            }
        }

        public int NextBatchId() => ++_batch;

        public List<Cell> CollectMain(Func<Cell, bool> pred)
        {
            var list = new List<Cell>();
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var c = _main[x][y];
                if (c != null && pred(c)) list.Add(c);
            }
            return list;
        }

        public void FillRandom(int colorCount, Random rng)
        {
            FillNoMatches(colorCount, rng);
        }

        public void FillNoMatches(int colorCount, Random rng)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (!IsNormalGrid(x, y)) continue;
                Set(x, y, null);
                int color = PickSafeColor(x, y, colorCount, rng);
                Set(x, y, Cell.CreateNormal(color, new GridPos(x, y)));
            }
        }

        int PickSafeColor(int x, int y, int colorCount, Random rng)
        {
            return PickColorAvoidingMatch(x, y, colorCount, rng, 0);
        }

        public int PickColorAvoidingMatch(int x, int y, int colorCount, Random rng, double matchChance)
        {
            if (matchChance > 0 && rng.NextDouble() < matchChance)
                return rng.Next(1, colorCount + 1);

            var colors = ShuffledColors(colorCount, rng);
            for (int i = 0; i < colors.Length; i++)
            {
                if (!WouldCreateMatch(x, y, colors[i]))
                    return colors[i];
            }

            return colors[0];
        }

        static int[] ShuffledColors(int colorCount, Random rng)
        {
            var colors = new int[colorCount];
            for (int i = 0; i < colorCount; i++)
                colors[i] = i + 1;
            for (int i = colorCount - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = colors[i];
                colors[i] = colors[j];
                colors[j] = tmp;
            }
            return colors;
        }

        public bool WouldCreateMatch(int x, int y, int color, int minLength = 3)
        {
            int horizontal = 1 + CountSame(x, y, -1, 0, color) + CountSame(x, y, 1, 0, color);
            if (horizontal >= minLength) return true;
            int vertical = 1 + CountSame(x, y, 0, -1, color) + CountSame(x, y, 0, 1, color);
            return vertical >= minLength;
        }

        int CountSame(int x, int y, int dx, int dy, int color)
        {
            int n = 0;
            int cx = x + dx;
            int cy = y + dy;
            while (!IsOut(cx, cy) && SameColor(Get(cx, cy), color))
            {
                n++;
                cx += dx;
                cy += dy;
            }
            return n;
        }

        static bool SameColor(Cell cell, int color)
        {
            return cell != null && cell.IsNormal && cell.ColorId == color;
        }

        public List<Cell> FindColorMatches(int minLength = 3)
        {
            var matched = new HashSet<Cell>();
            for (int y = 0; y < Height; y++)
            {
                int x = 0;
                while (x < Width)
                {
                    var start = Get(x, y);
                    if (!IsMatchable(start))
                    {
                        x++;
                        continue;
                    }
                    int end = x + 1;
                    while (end < Width && SameColor(Get(end, y), start.ColorId)) end++;
                    if (end - x >= minLength)
                    {
                        for (int i = x; i < end; i++)
                            matched.Add(Get(i, y));
                    }
                    x = end;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                int y = 0;
                while (y < Height)
                {
                    var start = Get(x, y);
                    if (!IsMatchable(start))
                    {
                        y++;
                        continue;
                    }
                    int end = y + 1;
                    while (end < Height && SameColor(Get(x, end), start.ColorId)) end++;
                    if (end - y >= minLength)
                    {
                        for (int i = y; i < end; i++)
                            matched.Add(Get(x, i));
                    }
                    y = end;
                }
            }

            return new List<Cell>(matched);
        }

        static bool IsMatchable(Cell cell)
        {
            return cell != null && cell.IsNormal;
        }

        public void CompactColumnDown(int x, List<Cell> moved)
        {
            var stack = new List<Cell>();
            for (int y = 0; y < Height; y++)
            {
                var cell = Get(x, y);
                if (cell != null)
                    stack.Add(cell);
            }

            for (int y = 0; y < Height; y++)
                Set(x, y, null);

            for (int i = 0; i < stack.Count; i++)
            {
                int fromY = stack[i].Grid.y;
                Set(x, i, stack[i]);
                if (fromY != i)
                    moved.Add(stack[i]);
            }
        }

        public void ShuffleNormals(Random rng)
        {
            var cells = CollectMain(c => c.IsNormal);
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var gi = cells[i].Grid;
                var gj = cells[j].Grid;
                Swap(gi, gj);
            }
        }
    }
}
