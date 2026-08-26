using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Draws the board and plays swap / fail / burst / drop tweens.
    /// Match rules live in Match3Engine; this class only presents them.
    /// </summary>
    public class Match3BoardView : MonoBehaviour
    {
        public int Width = 8;
        public int Height = 8;
        public int ColorCount = 5;
        public float CellSize = 1f;
        public float SwapDuration = 0.18f;
        public float FailSwapDuration = 0.14f;
        public float BurstDuration = 0.22f;
        public float DropSpeed = 12f;
        public float BottomMargin = 0.35f;
        public float BackgroundPadding = 0.12f;
        public Sprite[] ColorSprites;
        public Sprite BurstSprite;
        public Sprite BoardBackground;
        public Sprite MissileH;
        public Sprite MissileV;
        public Sprite Propeller;
        public Sprite PowderKeg;
        public Sprite LightBall;
        public Sprite Obstacle;

        Match3Engine _engine;
        readonly Dictionary<Cell, Transform> _views = new Dictionary<Cell, Transform>();
        readonly List<Sprite> _runtimeSprites = new List<Sprite>();
        GridPos? _selected;
        GridPos? _pressCell;
        Camera _camera;
        bool _busy;
        int _placedScreenW;
        int _placedScreenH;
        SpriteRenderer _boardBackground;

        public Match3Engine Engine => _engine;

        void Start()
        {
            _camera = Camera.main;
            ApplyPendingLevel();
            EnsureColorSprites();
            _engine = new Match3Engine(Width, Height);
            _engine.NewBoard(ColorCount);
            PlaceBoardAtBottom();
            EnsureBoardBackground();
            Rebuild();
            SyncHud();
        }

        void ApplyPendingLevel()
        {
            if (GameManager.Instance == null || !GameManager.Instance.HasPendingMatch3Level)
                return;

            var level = GameManager.Instance.ActiveMatch3Level;
            if (level == null)
                return;

            if (level.colorSprites != null && level.colorSprites.Length > 0)
            {
                ColorSprites = level.colorSprites;
                ColorCount = level.colorSprites.Length;
            }

            if (level.missileH != null) MissileH = level.missileH;
            if (level.missileV != null) MissileV = level.missileV;
            if (level.propeller != null) Propeller = level.propeller;
            if (level.powderKeg != null) PowderKeg = level.powderKeg;
            if (level.lightBall != null) LightBall = level.lightBall;
            if (level.obstacle != null) Obstacle = level.obstacle;
        }

        void SyncHud()
        {
            if (Match3ScoreUI.Instance == null)
                return;
            Match3ScoreUI.Instance.SetGoalSprite(ColorSprite(Match3ScoreUI.Instance.GoalColorId));
        }

        void OnDestroy()
        {
            for (int i = 0; i < _runtimeSprites.Count; i++)
            {
                if (_runtimeSprites[i] == null) continue;
                if (_runtimeSprites[i].texture != null)
                    Destroy(_runtimeSprites[i].texture);
                Destroy(_runtimeSprites[i]);
            }
        }

        void Update()
        {
            if (_engine == null) return;
            if (Screen.width != _placedScreenW || Screen.height != _placedScreenH)
            {
                PlaceBoardAtBottom();
                EnsureBoardBackground();
            }
            if (_busy) return;
            if (Match3ResultUI.Instance != null && Match3ResultUI.Instance.IsShowing)
                return;
            if (Match3ScoreUI.Instance != null && !Match3ScoreUI.Instance.HasTurnsLeft)
                return;

            if (PressedThisFrame())
            {
                if (TryGridAtPointer(out var cell))
                    _pressCell = cell;
            }

            if (ReleasedThisFrame() && _pressCell.HasValue)
            {
                if (TryGridAtPointer(out var released) && released.IsNeighbor(_pressCell.Value))
                    TrySwapCells(_pressCell.Value, released);
                else if (TryGridAtPointer(out var clicked))
                    HandleClick(clicked);

                _pressCell = null;
            }
        }

        public void Swap(Vector2Int a, Vector2Int b)
        {
            TrySwapCells(new GridPos(a.x, a.y), new GridPos(b.x, b.y));
        }

        public void Tap(Vector2Int a)
        {
        }

        void HandleClick(GridPos cell)
        {
            if (!_selected.HasValue)
            {
                _selected = cell;
                RefreshSelection();
                return;
            }

            if (_selected.Value.Equals(cell))
            {
                _selected = null;
                RefreshSelection();
                return;
            }

            if (_selected.Value.IsNeighbor(cell))
            {
                var from = _selected.Value;
                _selected = null;
                RefreshSelection();
                TrySwapCells(from, cell);
                return;
            }

            _selected = cell;
            RefreshSelection();
        }

        void TrySwapCells(GridPos a, GridPos b)
        {
            if (_busy) return;
            if (Match3ResultUI.Instance != null && Match3ResultUI.Instance.IsShowing)
                return;
            if (Match3ScoreUI.Instance != null && !Match3ScoreUI.Instance.HasTurnsLeft)
                return;
            StartCoroutine(PlaySwap(a, b));
        }

        IEnumerator PlaySwap(GridPos a, GridPos b)
        {
            _busy = true;
            _selected = null;
            RefreshSelection();

            var cellA = _engine.Board.Get(a);
            var cellB = _engine.Board.Get(b);
            bool matched = _engine.TrySwap(a, b, out var cleared);
            var viewA = ViewOf(cellA);
            var viewB = ViewOf(cellB);

            if (viewA != null && viewB != null)
            {
                yield return SwapViews(viewA, viewB, matched ? SwapDuration : FailSwapDuration);
                if (!matched)
                {
                    yield return SwapViews(viewA, viewB, FailSwapDuration);
                    _busy = false;
                    yield break;
                }
            }
            else if (!matched)
            {
                _busy = false;
                yield break;
            }

            if (Match3ScoreUI.Instance != null)
                Match3ScoreUI.Instance.ConsumeTurn();

            if (cleared.Count > 0)
            {
                ReportCleared(cleared);
                yield return BurstCells(cleared);
            }

            yield return ResolveCascade();
            _engine.FinishTurn();
            RefreshSelection();
            _busy = false;

            if (Match3ScoreUI.Instance != null)
                Match3ScoreUI.Instance.TryShowResultIfFinished();
        }

        IEnumerator ResolveCascade()
        {
            int guard = 64;
            while (guard-- > 0)
            {
                var moved = _engine.DropFill(out var spawned);
                if (moved.Count > 0 || spawned.Count > 0)
                    yield return AnimateDrop(moved, spawned);

                var matches = _engine.ClearMatches();
                if (matches.Count == 0)
                    yield break;

                ReportCleared(matches);
                yield return BurstCells(matches);
            }
        }

        void ReportCleared(List<Cell> cleared)
        {
            if (Match3ScoreUI.Instance != null)
                Match3ScoreUI.Instance.AddClearedCells(cleared);
        }

        IEnumerator SwapViews(Transform a, Transform b, float duration)
        {
            var posA = a.localPosition;
            var posB = b.localPosition;
            BumpSort(a, 5);
            BumpSort(b, 5);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Smooth(Mathf.Clamp01(t / duration));
                a.localPosition = Vector3.Lerp(posA, posB, k);
                b.localPosition = Vector3.Lerp(posB, posA, k);
                yield return null;
            }

            a.localPosition = posB;
            b.localPosition = posA;
            BumpSort(a, 1);
            BumpSort(b, 1);
        }

        IEnumerator BurstCells(List<Cell> cleared)
        {
            var running = new List<Transform>(cleared.Count);
            for (int i = 0; i < cleared.Count; i++)
            {
                if (!_views.TryGetValue(cleared[i], out var view) || view == null)
                    continue;
                _views.Remove(cleared[i]);
                running.Add(view);
                StartCoroutine(BurstOne(view));
            }

            yield return new WaitForSeconds(BurstDuration);

            for (int i = 0; i < running.Count; i++)
            {
                if (running[i] != null)
                    Destroy(running[i].gameObject);
            }
        }

        IEnumerator BurstOne(Transform view)
        {
            var sr = view.GetComponent<SpriteRenderer>();
            var startScale = view.localScale;
            var startColor = sr != null ? sr.color : Color.white;
            SpawnBurstShards(view.localPosition, startColor);

            float t = 0f;
            while (t < BurstDuration && view != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / BurstDuration);
                view.localScale = startScale * Mathf.Lerp(1f, 1.45f, k);
                if (sr != null)
                {
                    var c = startColor;
                    c.a = 1f - k;
                    sr.color = c;
                }
                yield return null;
            }
        }

        void SpawnBurstShards(Vector3 localPos, Color color)
        {
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("burst");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = localPos;
                go.transform.localScale = Vector3.one * (CellSize * 0.28f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BurstSprite != null ? BurstSprite : null;
                sr.color = color;
                sr.sortingOrder = 6;
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                StartCoroutine(FlyShard(go.transform, sr, dir * (CellSize * Random.Range(0.45f, 0.8f))));
            }
        }

        IEnumerator FlyShard(Transform shard, SpriteRenderer sr, Vector3 delta)
        {
            var from = shard.localPosition;
            var to = from + delta;
            float t = 0f;
            float duration = BurstDuration;
            while (t < duration && shard != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                shard.localPosition = Vector3.Lerp(from, to, k);
                shard.localScale = Vector3.one * (CellSize * 0.28f * (1f - k));
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = 1f - k;
                    sr.color = c;
                }
                yield return null;
            }
            if (shard != null)
                Destroy(shard.gameObject);
        }

        IEnumerator AnimateDrop(List<Cell> moved, List<Cell> spawned)
        {
            var movers = new List<MoveAnim>();

            for (int i = 0; i < moved.Count; i++)
            {
                var cell = moved[i];
                if (!_views.TryGetValue(cell, out var view) || view == null)
                    continue;
                var dest = LocalPos(cell.Grid);
                if ((view.localPosition - dest).sqrMagnitude <= 0.0001f)
                    continue;
                movers.Add(new MoveAnim { Transform = view, From = view.localPosition, To = dest });
            }

            spawned.Sort((a, b) =>
            {
                int cmp = a.Grid.x.CompareTo(b.Grid.x);
                return cmp != 0 ? cmp : a.Grid.y.CompareTo(b.Grid.y);
            });

            var spawnedInColumn = new Dictionary<int, int>();
            for (int i = 0; i < spawned.Count; i++)
            {
                var cell = spawned[i];
                spawnedInColumn.TryGetValue(cell.Grid.x, out int n);
                var dest = LocalPos(cell.Grid);
                var start = new Vector3(dest.x, Height * CellSize + (n + 1) * CellSize, 0f);
                var view = SpawnView(cell, start);
                movers.Add(new MoveAnim { Transform = view, From = start, To = dest });
                spawnedInColumn[cell.Grid.x] = n + 1;
            }

            if (movers.Count == 0)
                yield break;

            var durations = new float[movers.Count];
            float maxDuration = 0.08f;
            for (int i = 0; i < movers.Count; i++)
            {
                float dist = Vector3.Distance(movers[i].From, movers[i].To);
                durations[i] = 0.08f + dist / DropSpeed;
                if (durations[i] > maxDuration)
                    maxDuration = durations[i];
                BumpSort(movers[i].Transform, 3);
            }

            float t = 0f;
            while (t < maxDuration)
            {
                t += Time.deltaTime;
                for (int i = 0; i < movers.Count; i++)
                {
                    var m = movers[i];
                    if (m.Transform == null) continue;
                    float k = Smooth(Mathf.Clamp01(t / durations[i]));
                    m.Transform.localPosition = Vector3.Lerp(m.From, m.To, k);
                }
                yield return null;
            }

            for (int i = 0; i < movers.Count; i++)
            {
                if (movers[i].Transform == null) continue;
                movers[i].Transform.localPosition = movers[i].To;
                BumpSort(movers[i].Transform, 1);
            }
        }

        struct MoveAnim
        {
            public Transform Transform;
            public Vector3 From;
            public Vector3 To;
        }

        void Rebuild()
        {
            foreach (var kv in _views)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _views.Clear();

            foreach (var cell in _engine.Board.AllMain())
                SpawnView(cell, LocalPos(cell.Grid));

            RefreshSelection();
        }

        Transform SpawnView(Cell cell, Vector3 localPos)
        {
            var go = new GameObject($"cell_{cell.Grid.x}_{cell.Grid.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * (CellSize * 0.92f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteOf(cell);
            sr.sortingOrder = 1;
            _views[cell] = go.transform;
            return go.transform;
        }

        Transform ViewOf(Cell cell)
        {
            if (cell == null) return null;
            return _views.TryGetValue(cell, out var view) ? view : null;
        }

        Vector3 LocalPos(GridPos grid)
        {
            return new Vector3(grid.x * CellSize, grid.y * CellSize, 0f);
        }

        static void BumpSort(Transform view, int order)
        {
            var sr = view.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = order;
        }

        static float Smooth(float k)
        {
            return k * k * (3f - 2f * k);
        }

        void RefreshSelection()
        {
            foreach (var kv in _views)
            {
                if (kv.Value == null) continue;
                kv.Value.localScale = Vector3.one * (CellSize * 0.92f);
            }

            if (!_selected.HasValue || _engine == null) return;
            var cell = _engine.Board.Get(_selected.Value);
            if (cell != null && _views.TryGetValue(cell, out var t) && t != null)
                t.localScale = Vector3.one * (CellSize * 1.12f);
        }

        Sprite SpriteOf(Cell cell)
        {
            if (cell.IsObstacle) return Obstacle != null ? Obstacle : ColorSprite(cell.ColorId);
            switch (cell.Special)
            {
                case SpecialType.HMissile: return MissileH != null ? MissileH : ColorSprite(cell.ColorId);
                case SpecialType.VMissile: return MissileV != null ? MissileV : ColorSprite(cell.ColorId);
                case SpecialType.Propeller: return Propeller != null ? Propeller : ColorSprite(cell.ColorId);
                case SpecialType.PowderKeg: return PowderKeg != null ? PowderKeg : ColorSprite(cell.ColorId);
                case SpecialType.LightBall: return LightBall != null ? LightBall : ColorSprite(cell.ColorId);
            }
            return ColorSprite(cell.ColorId);
        }

        Sprite ColorSprite(int colorId)
        {
            int i = Mathf.Clamp(colorId - 1, 0, ColorSprites.Length - 1);
            return ColorSprites[i];
        }

        void EnsureColorSprites()
        {
            if (ColorSprites != null && ColorSprites.Length >= ColorCount) return;

            var colors = new[]
            {
                new Color(0.91f, 0.30f, 0.33f),
                new Color(0.36f, 0.72f, 0.36f),
                new Color(0.31f, 0.56f, 0.91f),
                new Color(0.96f, 0.78f, 0.22f),
                new Color(0.66f, 0.42f, 0.84f)
            };

            ColorSprites = new Sprite[ColorCount];
            for (int i = 0; i < ColorCount; i++)
            {
                var sprite = MakeSquareSprite(colors[i % colors.Length]);
                _runtimeSprites.Add(sprite);
                ColorSprites[i] = sprite;
            }
        }

        static Sprite MakeSquareSprite(Color color)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = border ? Color.Lerp(color, Color.black, 0.35f) : color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        void PlaceBoardAtBottom()
        {
            if (_camera == null) _camera = Camera.main;
            _placedScreenW = Screen.width;
            _placedScreenH = Screen.height;
            if (_camera == null)
            {
                transform.position = new Vector3(
                    -(Width - 1) * CellSize * 0.5f,
                    0f,
                    0f);
                return;
            }

            float planeZ = 0f;
            float distance = Mathf.Abs(_camera.transform.position.z - planeZ);
            var screenCenter = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, distance));
            var screenBottom = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, distance));

            transform.position = new Vector3(
                screenCenter.x - (Width - 1) * CellSize * 0.5f,
                screenBottom.y + BottomMargin + CellSize * 0.5f,
                planeZ);
            EnsureBoardBackground();
        }

        void EnsureBoardBackground()
        {
            if (BoardBackground == null)
            {
                if (_boardBackground != null)
                    _boardBackground.enabled = false;
                return;
            }

            if (_boardBackground == null)
            {
                var go = new GameObject("BoardBackground");
                go.transform.SetParent(transform, false);
                _boardBackground = go.AddComponent<SpriteRenderer>();
                _boardBackground.sortingOrder = -1;
            }

            _boardBackground.enabled = true;
            _boardBackground.sprite = BoardBackground;
            _boardBackground.transform.localPosition = new Vector3(
                (Width - 1) * CellSize * 0.5f,
                (Height - 1) * CellSize * 0.5f,
                0f);

            var size = BoardBackground.bounds.size;
            float targetW = Width * CellSize + BackgroundPadding * 2f;
            float targetH = Height * CellSize + BackgroundPadding * 2f;
            _boardBackground.transform.localScale = new Vector3(
                size.x > 0.0001f ? targetW / size.x : 1f,
                size.y > 0.0001f ? targetH / size.y : 1f,
                1f);
        }

        bool TryGridAtPointer(out GridPos grid)
        {
            grid = default;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Vector3 screen = PointerScreen();
            var ray = _camera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.forward, transform.position);
            if (!plane.Raycast(ray, out float enter)) return false;

            var local = transform.InverseTransformPoint(ray.GetPoint(enter));
            int x = Mathf.RoundToInt(local.x / CellSize);
            int y = Mathf.RoundToInt(local.y / CellSize);
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            grid = new GridPos(x, y);
            return true;
        }

        static Vector3 PointerScreen()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        static bool PressedThisFrame()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        static bool ReleasedThisFrame()
        {
            if (Input.GetMouseButtonUp(0)) return true;
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
        }
    }
}
