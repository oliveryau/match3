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
        [Header("Burst Stars")]
        [FormerlySerializedAs("BurstDuration")]
        [Tooltip("Burst / shard lifetime for a normal 3-match clear.")]
        public float BurstDurationMatch3 = 0.22f;
        [Tooltip("Burst / shard lifetime for a 4+ match clear.")]
        public float BurstDurationMatch4 = 0.28f;
        [Tooltip("Burst / shard lifetime for a gold peach detonation.")]
        public float BurstDurationGoldPeach = 0.35f;
        [Tooltip("Star shards spawned on a normal 3-match clear.")]
        public int BurstStarCountMatch3 = 5;
        [Tooltip("Star shards spawned on a 4+ match clear.")]
        public int BurstStarCountMatch4 = 10;
        [Tooltip("Star shards spawned on a gold peach detonation.")]
        public int BurstStarCountGoldPeach = 16;
        [Tooltip("Shard size as a fraction of CellSize for a normal 3-match.")]
        public float BurstStarScaleMatch3 = 0.28f;
        [Tooltip("Shard size as a fraction of CellSize for a 4+ match.")]
        public float BurstStarScaleMatch4 = 0.42f;
        [Tooltip("Shard size as a fraction of CellSize for a gold peach.")]
        public float BurstStarScaleGoldPeach = 0.5f;
        [Tooltip("Parent for pooled burst stars. Created under the board if left empty.")]
        [SerializeField] Transform burstStarPoolRoot;
        [Tooltip("How many burst stars to pre-create at start.")]
        [SerializeField] int burstStarPoolPrewarm = 64;
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
        [Tooltip("Obstacle art variants. Counts are balanced evenly when the board is built.")]
        public Sprite[] ObstacleSprites;
        [Tooltip("Used when ObstacleSprites is empty.")]
        public Sprite Obstacle;
        [Header("Match Hint")]
        [Tooltip("Pulsing circle shown on a potential swap after idle.")]
        [FormerlySerializedAs("hintCircleSprite")]
        public Sprite HintCircleSprite;
        [Tooltip("Seconds without a match before showing a hint on a potential swap.")]
        [FormerlySerializedAs("hintIdleSeconds")]
        public float HintIdleSeconds = 3f;
        [Tooltip("One pulse cycle length in seconds (scale up + fade).")]
        [FormerlySerializedAs("hintPulseDuration")]
        public float HintPulseDuration = 1.1f;
        [Tooltip("Start diameter as a fraction of CellSize.")]
        [FormerlySerializedAs("hintPulseScaleMin")]
        public float HintPulseScaleMin = 1.05f;
        [Tooltip("End diameter as a fraction of CellSize.")]
        [FormerlySerializedAs("hintPulseScaleMax")]
        public float HintPulseScaleMax = 1.55f;
        [Tooltip("Alpha at the start of each pulse.")]
        [Range(0f, 1f)]
        public float HintPulseAlphaMax = 1f;
        [Tooltip("Alpha at the end of each pulse (keep > 0 so the ring stays readable).")]
        [Range(0f, 1f)]
        public float HintPulseAlphaMin = 0.15f;
        [Tooltip("Sorting order above foods (1) / specials (2) / burst stars (6).")]
        public int HintSortingOrder = 12;
        [Header("Input")]
        [Tooltip("How far to drag (screen pixels) before a swap triggers.")]
        [SerializeField] float swipePixelsToSwap = 32f;
        [SerializeField] Match3LevelVideoPlayer levelVideo;

        Match3Engine _engine;
        readonly Dictionary<Cell, Transform> _views = new Dictionary<Cell, Transform>();
        readonly List<Sprite> _runtimeSprites = new List<Sprite>();
        GridPos? _selected;
        GridPos? _pressCell;
        Vector2? _pressScreen;
        bool _gestureHandled;
        int _activePointerId = -2;
        Camera _camera;
        bool _busy;
        int _placedScreenW;
        int _placedScreenH;
        SpriteRenderer _boardBackground;
        Transform _foodBgRoot;
        RectTransform _flyOverlayRoot;
        Canvas _flyOverlayCanvas;
        readonly Stack<Transform> _burstStarPool = new Stack<Transform>(128);
        float _hintIdleElapsed;
        float _hintPulseElapsed;
        bool _hintVisible;
        readonly Transform[] _hintCircles = new Transform[2];
        readonly SpriteRenderer[] _hintRenderers = new SpriteRenderer[2];

        public static Match3BoardView Instance { get; private set; }

        public Match3Engine Engine => _engine;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            _camera = Camera.main;
            ApplyPendingLevel();
            ApplyMatch3Bgm();
            EnsureFoodSprites();
            EnsureBurstStarPool();
            _engine = new Match3Engine(Width, Height);
            _engine.NewTileMatchChance = NewTileMatchChance;
            _engine.NewBoard(ColorCount);
            if (ShouldSpawnVacationEdgeObstacles())
            {
                _engine.PlaceEdgeColumnObstacles();
                AssignEvenObstacleVariants();
            }
            PlaceBoardAtBottom();
            EnsureBoardBackground();
            EnsureFoodBackgrounds();
            Rebuild();
            SyncHud();
            EnsureHintCircles();
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

        static void ApplyMatch3Bgm()
        {
            if (AudioManager.Instance == null)
                return;

            if (GameManager.Instance != null && GameManager.Instance.HasPendingMatch3Level)
                AudioManager.Instance.ApplyForMatch3(GameManager.Instance.ActiveStreetVideoId);
            else
                AudioManager.Instance.PlayNamedBgm(AudioManager.Bgm1Name);
        }

        void ConfigureLevelVideo(Match3LevelConfig level)
        {
            if (levelVideo == null)
                levelVideo = Match3LevelVideoPlayer.Instance;
            if (levelVideo != null)
                levelVideo.Configure(level);
        }

        static bool ShouldSpawnVacationEdgeObstacles()
        {
            var gm = GameManager.Instance;
            return gm != null
                   && gm.HasPendingMatch3Level
                   && gm.ActiveStreetVideoId == HomeVideoId.VacationStreet;
        }

        void AssignEvenObstacleVariants()
        {
            var sprites = ResolvedObstacleSprites();
            if (sprites.Length == 0 || _engine == null)
                return;

            var obstacles = new List<Cell>();
            foreach (var cell in _engine.Board.AllMain())
            {
                if (cell.IsObstacle)
                    obstacles.Add(cell);
            }

            if (obstacles.Count == 0)
                return;

            if (sprites.Length == 1)
            {
                for (int i = 0; i < obstacles.Count; i++)
                    obstacles[i].ColorId = 1;
                return;
            }

            var bag = BuildEvenVariantBag(obstacles.Count, sprites.Length);
            ShuffleVariantBag(bag);

            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i].ColorId = bag[i] + 1;
        }

        static List<int> BuildEvenVariantBag(int count, int variantCount)
        {
            var bag = new List<int>(count);
            int baseEach = count / variantCount;
            int remainder = count % variantCount;
            for (int variant = 0; variant < variantCount; variant++)
            {
                int amount = baseEach + (variant < remainder ? 1 : 0);
                for (int i = 0; i < amount; i++)
                    bag.Add(variant);
            }

            return bag;
        }

        static void ShuffleVariantBag(List<int> bag)
        {
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = bag[i];
                bag[i] = bag[j];
                bag[j] = tmp;
            }
        }

        Sprite[] ResolvedObstacleSprites()
        {
            if (ObstacleSprites != null && ObstacleSprites.Length > 0)
            {
                int count = 0;
                for (int i = 0; i < ObstacleSprites.Length; i++)
                {
                    if (ObstacleSprites[i] != null)
                        count++;
                }

                if (count == 0)
                    return Obstacle != null ? new[] { Obstacle } : new Sprite[0];

                var sprites = new Sprite[count];
                int write = 0;
                for (int i = 0; i < ObstacleSprites.Length; i++)
                {
                    if (ObstacleSprites[i] != null)
                        sprites[write++] = ObstacleSprites[i];
                }

                return sprites;
            }

            return Obstacle != null ? new[] { Obstacle } : new Sprite[0];
        }

        Sprite ObstacleSprite(Cell cell)
        {
            var sprites = ResolvedObstacleSprites();
            if (sprites.Length == 0)
                return FoodSprite(cell.ColorId);

            int index = Mathf.Clamp(cell.ColorId - 1, 0, sprites.Length - 1);
            return sprites[index];
        }

        void NotifyLevelVideo()
        {
            if (levelVideo == null)
                levelVideo = Match3LevelVideoPlayer.Instance;
            if (levelVideo == null || _engine == null)
                return;
            levelVideo.NotifyClear(_engine.LastMaxMatchRunLength, _engine.LastWasGoldPeachBurst);
        }

        void PlayMatchClearSfx()
        {
            if (AudioManager.Instance == null || _engine == null)
                return;
            AudioManager.Instance.PlayMatchClear(
                _engine.LastWasGoldPeachBurst,
                _engine.LastMaxMatchRunLength);
        }

        void SyncHud()
        {
            if (Match3ScoreUI.Instance == null)
                return;
            Match3ScoreUI.Instance.SetGoalSprite(FoodSprite(Match3ScoreUI.Instance.GoalFoodId));
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

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

        /// <summary>Screen point at the board grid's center (for star unlock FX).</summary>
        public bool TryGetBoardCenterScreenPoint(out Vector2 screenPoint)
        {
            screenPoint = default;
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return false;

            var local = new Vector3(
                (Width - 1) * 0.5f * CellSize,
                (Height - 1) * 0.5f * CellSize,
                0f);
            Vector3 world = transform.TransformPoint(local);
            screenPoint = _camera.WorldToScreenPoint(world);
            return true;
        }

        void Update()
        {
            if (_engine == null) return;
            if (Screen.width != _placedScreenW || Screen.height != _placedScreenH)
            {
                PlaceBoardAtBottom();
                EnsureBoardBackground();
            }

            UpdateMatchHint();

            if (_busy) return;
            if (Match3ResultUI.Instance != null && Match3ResultUI.Instance.IsShowing)
                return;
            if (Match3ScoreUI.Instance != null && !Match3ScoreUI.Instance.HasTurnsLeft)
                return;

            ProcessBoardInput();
        }

        void ProcessBoardInput()
        {
            if (PointerBegan(out Vector2 beganPos))
            {
                if (TryGridAtScreen(beganPos, out var cell) && IsOperableCell(cell))
                {
                    _pressCell = cell;
                    _pressScreen = beganPos;
                    _gestureHandled = false;
                }
                else
                {
                    ClearPointerGesture();
                }

                return;
            }

            if (!_pressCell.HasValue || !_pressScreen.HasValue)
                return;

            if (!TryGetPointerState(out Vector2 pos, out bool held, out bool ended))
            {
                ClearPointerGesture();
                return;
            }

            if (held && !_gestureHandled)
            {
                float minSwipe = Mathf.Max(8f, swipePixelsToSwap);
                Vector2 delta = pos - _pressScreen.Value;
                if (delta.sqrMagnitude >= minSwipe * minSwipe
                    && TryGetSwipeNeighbor(_pressCell.Value, delta, out var neighbor))
                {
                    TrySwapCells(_pressCell.Value, neighbor);
                    ClearPointerGesture();
                    return;
                }
            }

            if (!ended)
                return;

            if (!_gestureHandled && TryGridAtScreen(pos, out var released))
            {
                if (released.IsNeighbor(_pressCell.Value))
                    TrySwapCells(_pressCell.Value, released);
                else
                    HandleClick(released);
            }

            ClearPointerGesture();
        }

        void ClearPointerGesture()
        {
            _pressCell = null;
            _pressScreen = null;
            _gestureHandled = false;
            _activePointerId = -2;
        }

        bool TryGetSwipeNeighbor(GridPos from, Vector2 screenDelta, out GridPos neighbor)
        {
            neighbor = default;
            if (Mathf.Abs(screenDelta.x) >= Mathf.Abs(screenDelta.y))
            {
                neighbor = screenDelta.x >= 0f
                    ? new GridPos(from.x + 1, from.y)
                    : new GridPos(from.x - 1, from.y);
            }
            else
            {
                neighbor = screenDelta.y >= 0f
                    ? new GridPos(from.x, from.y + 1)
                    : new GridPos(from.x, from.y - 1);
            }

            if (neighbor.x < 0 || neighbor.y < 0 || neighbor.x >= Width || neighbor.y >= Height)
                return false;

            return neighbor.IsNeighbor(from);
        }

        bool PointerBegan(out Vector2 pos)
        {
            pos = default;
            if (_pressCell.HasValue)
                return false;

            if (Input.GetMouseButtonDown(0))
            {
                _activePointerId = -1;
                pos = Input.mousePosition;
                return true;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                _activePointerId = touch.fingerId;
                pos = touch.position;
                return true;
            }

            return false;
        }

        bool TryGetPointerState(out Vector2 pos, out bool held, out bool ended)
        {
            pos = default;
            held = false;
            ended = false;

            if (_activePointerId < 0)
            {
                if (Input.GetMouseButton(0))
                {
                    pos = Input.mousePosition;
                    held = true;
                    return true;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    pos = Input.mousePosition;
                    ended = true;
                    return true;
                }

                return false;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId != _activePointerId)
                    continue;

                pos = touch.position;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    ended = true;
                    return true;
                }

                held = true;
                return true;
            }

            ended = true;
            return true;
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
            if (!IsOperableCell(cell))
                return;

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
            if (!IsOperableCell(a) || !IsOperableCell(b))
                return;
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
            HideMatchHint();

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
                NotifyPlayerMatched();
                NotifyLevelVideo();
                PlayMatchClearSfx();
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

                NotifyPlayerMatched();
                ReportCleared(matches);
                NotifyLevelVideo();
                PlayMatchClearSfx();
                yield return BurstCells(matches);
                SpawnSpecialViews();
            }
        }

        void NotifyPlayerMatched()
        {
            HideMatchHint();
            _hintIdleElapsed = 0f;
        }

        void UpdateMatchHint()
        {
            if (_engine == null)
            {
                HideMatchHint();
                return;
            }

            if (HintCircleSprite == null)
                HintCircleSprite = BurstSprite;

            bool blocked = _busy
                || (Match3ResultUI.Instance != null && Match3ResultUI.Instance.IsShowing)
                || (Match3ScoreUI.Instance != null && !Match3ScoreUI.Instance.HasTurnsLeft);

            if (blocked)
            {
                HideMatchHint();
                return;
            }

            // Idle = time since last successful match (failed swaps do not reset).
            _hintIdleElapsed += Time.deltaTime;
            if (_hintIdleElapsed < Mathf.Max(0.1f, HintIdleSeconds))
                return;

            if (!_hintVisible)
                TryShowMatchHint();

            if (_hintVisible)
                AnimateMatchHintPulse();
        }

        void TryShowMatchHint()
        {
            if (HintCircleSprite == null)
            {
                HideMatchHint();
                return;
            }

            if (!_engine.TryGetHintSwap(out _, out _, out var highlight))
            {
                HideMatchHint();
                return;
            }

            EnsureHintCircles();
            PlaceHintCircle(0, highlight);
            if (_hintCircles.Length > 1 && _hintCircles[1] != null)
                _hintCircles[1].gameObject.SetActive(false);

            _hintPulseElapsed = 0f;
            _hintVisible = true;
            AnimateMatchHintPulse();
        }

        void PlaceHintCircle(int index, GridPos grid)
        {
            if (index < 0 || index >= _hintCircles.Length || _hintCircles[index] == null)
                return;

            _hintCircles[index].localPosition = LocalPos(grid);
            if (_hintRenderers[index] != null)
            {
                _hintRenderers[index].sprite = HintCircleSprite;
                _hintRenderers[index].sortingOrder = HintSortingOrder;
                var c = Color.white;
                c.a = HintPulseAlphaMax;
                _hintRenderers[index].color = c;
            }

            ApplyHintCircleScale(_hintCircles[index], HintPulseScaleMin);
            _hintCircles[index].gameObject.SetActive(true);
        }

        void AnimateMatchHintPulse()
        {
            float period = Mathf.Max(0.05f, HintPulseDuration);
            _hintPulseElapsed += Time.deltaTime;
            float k = (_hintPulseElapsed % period) / period;
            float scaleFrac = Mathf.Lerp(HintPulseScaleMin, HintPulseScaleMax, k);
            float alpha = Mathf.Lerp(HintPulseAlphaMax, HintPulseAlphaMin, k);

            for (int i = 0; i < _hintCircles.Length; i++)
            {
                if (_hintCircles[i] == null || !_hintCircles[i].gameObject.activeSelf)
                    continue;
                ApplyHintCircleScale(_hintCircles[i], scaleFrac);
                if (_hintRenderers[i] != null)
                {
                    var c = _hintRenderers[i].color;
                    c.a = alpha;
                    _hintRenderers[i].color = c;
                }
            }
        }

        void ApplyHintCircleScale(Transform circle, float diameterFrac)
        {
            if (circle == null)
                return;

            float target = CellSize * Mathf.Max(0.01f, diameterFrac);
            var sr = circle.GetComponent<SpriteRenderer>();
            var sprite = sr != null && sr.sprite != null ? sr.sprite : HintCircleSprite;

            if (sprite != null)
            {
                var size = sprite.bounds.size;
                float sx = size.x > 0.0001f ? target / size.x : target;
                float sy = size.y > 0.0001f ? target / size.y : target;
                circle.localScale = new Vector3(sx, sy, 1f);
            }
            else
            {
                circle.localScale = Vector3.one * target;
            }
        }

        void HideMatchHint()
        {
            _hintVisible = false;
            _hintPulseElapsed = 0f;
            for (int i = 0; i < _hintCircles.Length; i++)
            {
                if (_hintCircles[i] != null)
                    _hintCircles[i].gameObject.SetActive(false);
            }
        }

        void EnsureHintCircles()
        {
            for (int i = 0; i < _hintCircles.Length; i++)
            {
                if (_hintCircles[i] != null)
                    continue;

                var go = new GameObject($"Hint Circle {i + 1}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = HintCircleSprite;
                sr.sortingOrder = HintSortingOrder;
                sr.color = Color.white;
                go.SetActive(false);
                _hintCircles[i] = go.transform;
                _hintRenderers[i] = sr;
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

            GetBurstFxParams(out int starCount, out float starScale, out float burstDuration);
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
                    // Goal foods fly to the HUD instead of bursting, but still spawn stars.
                    var sr = view.GetComponent<SpriteRenderer>();
                    SpawnBurstShards(
                        view.localPosition,
                        sr != null ? sr.color : Color.white,
                        starCount,
                        starScale,
                        burstDuration);
                    StartCoroutine(FlyToGoalUi(view));
                }
                else
                {
                    running.Add(view);
                    StartCoroutine(BurstOne(view, starCount, starScale, burstDuration));
                }
            }

            float wait = anyFlyToGoal
                ? Mathf.Max(burstDuration, FlyToGoalDuration)
                : burstDuration;
            yield return new WaitForSeconds(wait);

            for (int i = 0; i < running.Count; i++)
            {
                if (running[i] != null)
                    Destroy(running[i].gameObject);
            }
        }

        void GetBurstFxParams(out int count, out float scaleFrac, out float duration)
        {
            if (_engine != null && _engine.LastWasGoldPeachBurst)
            {
                count = Mathf.Max(1, BurstStarCountGoldPeach);
                scaleFrac = BurstStarScaleGoldPeach;
                duration = Mathf.Max(0.01f, BurstDurationGoldPeach);
            }
            else if (_engine != null && _engine.LastMaxMatchRunLength >= 4)
            {
                count = Mathf.Max(1, BurstStarCountMatch4);
                scaleFrac = BurstStarScaleMatch4;
                duration = Mathf.Max(0.01f, BurstDurationMatch4);
            }
            else
            {
                count = Mathf.Max(1, BurstStarCountMatch3);
                scaleFrac = BurstStarScaleMatch3;
                duration = Mathf.Max(0.01f, BurstDurationMatch3);
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

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGoalDing();

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

        IEnumerator BurstOne(Transform view, int starCount, float starScaleFrac, float burstDuration)
        {
            var sr = view.GetComponent<SpriteRenderer>();
            var startScale = view.localScale;
            var startColor = sr != null ? sr.color : Color.white;
            SpawnBurstShards(view.localPosition, startColor, starCount, starScaleFrac, burstDuration);

            float t = 0f;
            while (t < burstDuration && view != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / burstDuration);
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

        void SpawnBurstShards(
            Vector3 localPos,
            Color color,
            int count,
            float scaleFrac,
            float burstDuration)
        {
            count = Mathf.Max(1, count);
            float size = CellSize * Mathf.Max(0.01f, scaleFrac);
            float travel = CellSize * (scaleFrac >= BurstStarScaleMatch4 ? 1.05f : 0.8f);
            for (int i = 0; i < count; i++)
            {
                var shard = RentBurstStar();
                if (shard == null)
                    continue;

                shard.SetParent(burstStarPoolRoot != null ? burstStarPoolRoot : transform, false);
                shard.localPosition = localPos;
                shard.localScale = Vector3.one * size;
                shard.gameObject.SetActive(true);

                var sr = shard.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = BurstSprite != null ? BurstSprite : null;
                    sr.color = color;
                    sr.sortingOrder = 6;
                }

                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                StartCoroutine(FlyShard(
                    shard,
                    sr,
                    dir * (travel * Random.Range(0.55f, 1f)),
                    size,
                    burstDuration));
            }
        }

        IEnumerator FlyShard(
            Transform shard,
            SpriteRenderer sr,
            Vector3 delta,
            float startSize,
            float burstDuration)
        {
            var from = shard.localPosition;
            var to = from + delta;
            float t = 0f;
            while (t < burstDuration && shard != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / burstDuration);
                shard.localPosition = Vector3.Lerp(from, to, k);
                shard.localScale = Vector3.one * (startSize * (1f - k));
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = 1f - k;
                    sr.color = c;
                }
                yield return null;
            }

            ReturnBurstStar(shard);
        }

        void EnsureBurstStarPool()
        {
            if (burstStarPoolRoot == null)
            {
                var existing = transform.Find("Burst Stars Pool");
                if (existing != null)
                    burstStarPoolRoot = existing;
                else
                {
                    var go = new GameObject("Burst Stars Pool");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    burstStarPoolRoot = go.transform;
                }
            }

            int prewarm = Mathf.Max(0, burstStarPoolPrewarm);
            while (_burstStarPool.Count < prewarm)
                _burstStarPool.Push(CreateBurstStar());
        }

        Transform CreateBurstStar()
        {
            var go = new GameObject("Burst Star");
            go.transform.SetParent(burstStarPoolRoot != null ? burstStarPoolRoot : transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BurstSprite;
            sr.sortingOrder = 6;
            go.SetActive(false);
            return go.transform;
        }

        Transform RentBurstStar()
        {
            if (_burstStarPool.Count == 0)
                return CreateBurstStar();

            var shard = _burstStarPool.Pop();
            if (shard == null)
                return CreateBurstStar();
            return shard;
        }

        void ReturnBurstStar(Transform shard)
        {
            if (shard == null)
                return;

            shard.gameObject.SetActive(false);
            shard.SetParent(burstStarPoolRoot != null ? burstStarPoolRoot : transform, false);
            shard.localPosition = Vector3.zero;
            shard.localScale = Vector3.one;
            var sr = shard.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
            _burstStarPool.Push(shard);
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
            var go = new GameObject(cell.IsObstacle
                ? $"obstacle_{cell.Grid.x}_{cell.Grid.y}"
                : cell.IsSpecial
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
            if (cell.IsObstacle) return ObstacleSprite(cell);
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

        bool IsOperableCell(GridPos grid)
        {
            if (_engine == null) return false;
            var cell = _engine.Board.Get(grid);
            return cell != null && cell.CanOperate;
        }

        bool TryGridAtScreen(Vector2 screen, out GridPos grid)
        {
            grid = default;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

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
    }
}
