using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
        [Header("Fly To Goal (UI Overlay)")]
        [Tooltip("How long goal-food tiles take to fly into the goal icon.")]
        public float FlyToGoalDuration = 0.4f;
        [Tooltip("Scale multiplier at the end of the fly (1 = no shrink, 0 = fully shrunk).")]
        [Range(0f, 1f)]
        public float FlyToGoalEndScale = 0.25f;
        [Tooltip("Normalized time (0–1) when the tile starts fading out during the fly.")]
        [Range(0f, 1f)]
        public float FlyToGoalFadeStart = 0.55f;
        [Tooltip("Alpha at the end of the fly (0 = fully invisible).")]
        [Range(0f, 1f)]
        public float FlyToGoalEndAlpha = 0f;
        [Tooltip("Upward arc height as a multiple of one cell's on-screen size.")]
        public float FlyToGoalArcHeight = 0.55f;
        [Tooltip("Overlay canvas sort order; must be above Match3 UI Canvas (usually 0).")]
        public int FlyOverlaySortOrder = 100;
        public float DropSpeed = 12f;
        public float BottomMargin = 0.35f;
        [Tooltip("Extra background size on left/right (world units).")]
        public float BackgroundPaddingWidth = 0.12f;
        [Tooltip("Extra background size on top/bottom (world units).")]
        public float BackgroundPaddingHeight = 0.12f;
        [Range(0f, 1f)]
        [Tooltip("Chance a newly spawned tile is allowed to form an immediate match. 0 = avoid when possible.")]
        public float NewTileMatchChance = 0.02f;
        [FormerlySerializedAs("ColorSprites")]
        public Sprite[] FoodSprites;
        [Tooltip("Static per-cell plate behind foods. Does not move with swaps/drops.")]
        public Sprite FoodBackground;
        public Sprite BurstSprite;
        public Sprite BoardBackground;
        public Sprite MissileH;
        public Sprite MissileV;
        [Tooltip("Unused for now if Missile H/V are set — gold peach uses those sprites.")]
        public Sprite Propeller;
        public Sprite PowderKeg;
        public Sprite LightBall;
        public Sprite Obstacle;
        [SerializeField] Match3LevelVideoPlayer levelVideo;

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
        Transform _foodBgRoot;
        RectTransform _flyOverlayRoot;
        Canvas _flyOverlayCanvas;

        public Match3Engine Engine => _engine;

        void Start()
        {
            _camera = Camera.main;
            ApplyPendingLevel();
            EnsureFoodSprites();
            _engine = new Match3Engine(Width, Height);
            _engine.NewTileMatchChance = NewTileMatchChance;
            _engine.NewBoard(ColorCount);
            PlaceBoardAtBottom();
            EnsureBoardBackground();
            EnsureFoodBackgrounds();
            Rebuild();
            SyncHud();
            // Build overlay early so the first fly-to-goal isn't using an unscaled/zero-size canvas.
            EnsureFlyOverlay();
            Canvas.ForceUpdateCanvases();
        }

        void ApplyPendingLevel()
        {
            if (GameManager.Instance == null || !GameManager.Instance.HasPendingMatch3Level)
            {
                ConfigureLevelVideo(null);
                return;
            }

            var level = GameManager.Instance.ActiveMatch3Level;
            if (level == null)
            {
                ConfigureLevelVideo(null);
                return;
            }

            if (level.foodSprites != null && level.foodSprites.Length > 0)
            {
                FoodSprites = level.foodSprites;
                ColorCount = level.foodSprites.Length;
            }

            if (level.boardBgSprite != null)
                BoardBackground = level.boardBgSprite;

            ConfigureLevelVideo(level);
        }

        void ConfigureLevelVideo(Match3LevelConfig level)
        {
            if (levelVideo == null)
                levelVideo = Match3LevelVideoPlayer.Instance;
            if (levelVideo != null)
                levelVideo.Configure(level);
        }

        void NotifyLevelVideo()
        {
            if (levelVideo == null)
                levelVideo = Match3LevelVideoPlayer.Instance;
            if (levelVideo == null || _engine == null)
                return;
            levelVideo.NotifyClear(_engine.LastMaxMatchRunLength, _engine.LastWasGoldPeachBurst);
        }

        void SyncHud()
        {
            if (Match3ScoreUI.Instance == null)
                return;
            Match3ScoreUI.Instance.SetGoalSprite(FoodSprite(Match3ScoreUI.Instance.GoalFoodId));
        }

        void OnDestroy()
        {
            if (_flyOverlayRoot != null)
                Destroy(_flyOverlayRoot.gameObject);

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
                NotifyLevelVideo();
                ReportCleared(cleared);
                yield return BurstCells(cleared);
                SpawnSpecialViews();
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
                NotifyLevelVideo();
                yield return BurstCells(matches);
                SpawnSpecialViews();
            }
        }

        void SpawnSpecialViews()
        {
            if (_engine == null || _engine.LastSpawnedSpecials == null)
                return;

            for (int i = 0; i < _engine.LastSpawnedSpecials.Count; i++)
            {
                var cell = _engine.LastSpawnedSpecials[i];
                if (cell == null)
                    continue;

                // Replace any leftover view on this grid, then spawn the gold peach.
                ClearViewAtGrid(cell.Grid);
                var view = SpawnView(cell, LocalPos(cell.Grid));
                if (view != null)
                    StartCoroutine(PopInSpecial(view));
            }
        }

        void ClearViewAtGrid(GridPos grid)
        {
            Cell removeKey = null;
            foreach (var kv in _views)
            {
                if (kv.Key != null && kv.Key.Grid.Equals(grid))
                {
                    removeKey = kv.Key;
                    if (kv.Value != null)
                        Destroy(kv.Value.gameObject);
                    break;
                }
            }

            if (removeKey != null)
                _views.Remove(removeKey);
        }

        IEnumerator PopInSpecial(Transform view)
        {
            if (view == null)
                yield break;

            var baseScale = Vector3.one * (CellSize * 0.92f);
            view.localScale = baseScale * 0.2f;
            float t = 0f;
            const float duration = 0.22f;
            while (t < duration && view != null)
            {
                t += Time.deltaTime;
                float k = Smooth(Mathf.Clamp01(t / duration));
                float punch = 1f + 0.18f * Mathf.Sin(k * Mathf.PI);
                view.localScale = baseScale * Mathf.Lerp(0.2f, punch, k);
                yield return null;
            }

            if (view != null)
                view.localScale = baseScale;
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
            int goalFoodId = Match3ScoreUI.Instance != null
                ? Match3ScoreUI.Instance.GoalFoodId
                : -1;

            var running = new List<Transform>(cleared.Count);
            bool anyFlyToGoal = false;

            for (int i = 0; i < cleared.Count; i++)
            {
                var cell = cleared[i];
                if (!_views.TryGetValue(cell, out var view) || view == null)
                    continue;
                _views.Remove(cell);

                bool isGoalFood = cell.IsNormal && cell.ColorId == goalFoodId;
                if (isGoalFood)
                {
                    anyFlyToGoal = true;
                    StartCoroutine(FlyToGoalUi(view));
                }
                else
                {
                    running.Add(view);
                    StartCoroutine(BurstOne(view));
                }
            }

            float wait = anyFlyToGoal
                ? Mathf.Max(BurstDuration, FlyToGoalDuration)
                : BurstDuration;
            yield return new WaitForSeconds(wait);

            for (int i = 0; i < running.Count; i++)
            {
                if (running[i] != null)
                    Destroy(running[i].gameObject);
            }
        }

        IEnumerator FlyToGoalUi(Transform view)
        {
            if (view == null)
                yield break;

            var sr = view.GetComponent<SpriteRenderer>();
            Sprite sprite = sr != null ? sr.sprite : null;
            Color startColor = sr != null ? sr.color : Color.white;
            Vector3 worldPos = view.position;

            // World tile is replaced by a UI overlay icon.
            Destroy(view.gameObject);

            if (sprite == null
                || Match3ScoreUI.Instance == null
                || !Match3ScoreUI.Instance.TryGetGoalScreenPoint(out var goalScreen)
                || _camera == null)
            {
                yield break;
            }

            EnsureFlyOverlay();
            if (_flyOverlayRoot == null)
                yield break;

            // CanvasScaler / layout must be current or ScreenPointToLocalPoint is wrong
            // (first fly used to create the overlay mid-frame and land off-target).
            Canvas.ForceUpdateCanvases();

            Vector2 startScreen = _camera.WorldToScreenPoint(worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _flyOverlayRoot, startScreen, null, out var startLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _flyOverlayRoot, goalScreen, null, out var goalLocal))
            {
                yield break;
            }

            float cellScreen = EstimateCellScreenSize();
            float scaleFactor = _flyOverlayCanvas != null ? Mathf.Max(0.001f, _flyOverlayCanvas.scaleFactor) : 1f;
            float size = cellScreen / scaleFactor;

            var go = new GameObject("FlyToGoal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_flyOverlayRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = startLocal;
            rt.localScale = Vector3.one;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = startColor;

            float t = 0f;
            float duration = Mathf.Max(0.05f, FlyToGoalDuration);
            float endScale = Mathf.Clamp01(FlyToGoalEndScale);
            float fadeStart = Mathf.Clamp01(FlyToGoalFadeStart);
            float endAlpha = Mathf.Clamp01(FlyToGoalEndAlpha);
            float arc = (cellScreen / scaleFactor) * FlyToGoalArcHeight;

            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float k = Smooth(Mathf.Clamp01(t / duration));
                var pos = Vector2.Lerp(startLocal, goalLocal, k);
                pos.y += Mathf.Sin(k * Mathf.PI) * arc;
                rt.anchoredPosition = pos;
                float scale = Mathf.Lerp(1f, endScale, k);
                rt.localScale = new Vector3(scale, scale, 1f);
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, endAlpha, Mathf.SmoothStep(fadeStart, 1f, k));
                image.color = c;
                yield return null;
            }

            if (go != null)
                Destroy(go);
        }

        void EnsureFlyOverlay()
        {
            if (_flyOverlayRoot != null)
            {
                if (_flyOverlayCanvas != null)
                    _flyOverlayCanvas.sortingOrder = FlyOverlaySortOrder;
                return;
            }

            var go = new GameObject("FlyToGoal Overlay");
            _flyOverlayRoot = go.AddComponent<RectTransform>();
            _flyOverlayCanvas = go.AddComponent<Canvas>();
            _flyOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _flyOverlayCanvas.sortingOrder = FlyOverlaySortOrder;
            // Don't add GraphicRaycaster — FX must not block UI clicks.

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1156f, 2510f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            _flyOverlayRoot.anchorMin = Vector2.zero;
            _flyOverlayRoot.anchorMax = Vector2.one;
            _flyOverlayRoot.offsetMin = Vector2.zero;
            _flyOverlayRoot.offsetMax = Vector2.zero;
            _flyOverlayRoot.pivot = new Vector2(0.5f, 0.5f);

            Canvas.ForceUpdateCanvases();
        }

        float EstimateCellScreenSize()
        {
            if (_camera == null)
                return 80f;

            Vector3 a = _camera.WorldToScreenPoint(transform.TransformPoint(Vector3.zero));
            Vector3 b = _camera.WorldToScreenPoint(transform.TransformPoint(new Vector3(0f, CellSize, 0f)));
            float size = Vector3.Distance(a, b);
            return size > 1f ? size : 80f;
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
            var go = new GameObject(cell.IsSpecial
                ? $"special_{cell.Special}_{cell.Grid.x}_{cell.Grid.y}"
                : $"cell_{cell.Grid.x}_{cell.Grid.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * (CellSize * 0.92f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteOf(cell);
            sr.sortingOrder = cell.IsSpecial ? 2 : 1;
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
            var sr = view != null ? view.GetComponent<SpriteRenderer>() : null;
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
            if (cell.IsObstacle) return Obstacle != null ? Obstacle : FoodSprite(cell.ColorId);
            switch (cell.Special)
            {
                case SpecialType.HMissile:
                case SpecialType.VMissile:
                    return GoldPeachSprite(cell.ColorId);
                case SpecialType.Propeller: return Propeller != null ? Propeller : FoodSprite(cell.ColorId);
                case SpecialType.PowderKeg: return PowderKeg != null ? PowderKeg : GoldPeachSprite(cell.ColorId);
                case SpecialType.LightBall: return LightBall != null ? LightBall : FoodSprite(cell.ColorId);
            }
            return FoodSprite(cell.ColorId);
        }

        Sprite GoldPeachSprite(int foodId)
        {
            if (MissileH != null) return MissileH;
            if (MissileV != null) return MissileV;
            return FoodSprite(foodId);
        }

        Sprite FoodSprite(int foodId)
        {
            if (FoodSprites == null || FoodSprites.Length == 0)
                return null;
            int i = Mathf.Clamp(foodId - 1, 0, FoodSprites.Length - 1);
            return FoodSprites[i];
        }

        void EnsureFoodSprites()
        {
            if (FoodSprites != null && FoodSprites.Length >= ColorCount) return;

            var colors = new[]
            {
                new Color(0.91f, 0.30f, 0.33f),
                new Color(0.36f, 0.72f, 0.36f),
                new Color(0.31f, 0.56f, 0.91f),
                new Color(0.96f, 0.78f, 0.22f),
                new Color(0.66f, 0.42f, 0.84f)
            };

            FoodSprites = new Sprite[ColorCount];
            for (int i = 0; i < ColorCount; i++)
            {
                var sprite = MakeSquareSprite(colors[i % colors.Length]);
                _runtimeSprites.Add(sprite);
                FoodSprites[i] = sprite;
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
            EnsureFoodBackgrounds();
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
            float targetW = Width * CellSize + BackgroundPaddingWidth * 2f;
            float targetH = Height * CellSize + BackgroundPaddingHeight * 2f;
            _boardBackground.transform.localScale = new Vector3(
                size.x > 0.0001f ? targetW / size.x : 1f,
                size.y > 0.0001f ? targetH / size.y : 1f,
                1f);
        }

        void EnsureFoodBackgrounds()
        {
            if (FoodBackground == null)
            {
                if (_foodBgRoot != null)
                    _foodBgRoot.gameObject.SetActive(false);
                return;
            }

            if (_foodBgRoot == null)
            {
                var root = new GameObject("FoodBackgrounds");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                _foodBgRoot = root.transform;
            }

            _foodBgRoot.gameObject.SetActive(true);

            int needed = Width * Height;
            while (_foodBgRoot.childCount < needed)
            {
                var go = new GameObject($"FoodBg_{_foodBgRoot.childCount}");
                go.transform.SetParent(_foodBgRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 0;
            }

            for (int i = 0; i < _foodBgRoot.childCount; i++)
            {
                var child = _foodBgRoot.GetChild(i);
                bool active = i < needed;
                child.gameObject.SetActive(active);
                if (!active)
                    continue;

                int x = i % Width;
                int y = i / Width;
                child.localPosition = LocalPos(new GridPos(x, y));
                child.localScale = Vector3.one * (CellSize * 0.92f);

                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = FoodBackground;
                    sr.sortingOrder = 0;
                }
            }
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
