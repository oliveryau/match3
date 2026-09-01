using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// Goal: clear a target number of tiles of one food (default: FoodSprites[0]).
    /// Fill shows progress percent. Stars unlock at 50% / 75% / 100%.
    /// </summary>
    public class Match3ScoreUI : MonoBehaviour
    {
        static readonly float[] StarThresholds = { 0.5f, 0.75f, 1f };

        [SerializeField] Image fillImage;
        [SerializeField] Image star1;
        [SerializeField] Image star2;
        [SerializeField] Image star3;
        [SerializeField] Image goalIcon;
        [SerializeField] TMP_Text targetText;
        [SerializeField] TMP_Text turnsLeftText;
        [Tooltip("How many tiles of the goal food must be matched this level.")]
        [SerializeField] int targetMatchCount = 15;
        [Tooltip("1-based food id. 1 = FoodSprites element 0.")]
        [FormerlySerializedAs("goalColorId")]
        [SerializeField] int goalFoodId = 1;
        [SerializeField] int maxTurns = 99;
        [SerializeField] float fillLerpSpeed = 2f;
        [SerializeField] float pulseSpeed = 4f;
        [SerializeField] float pulseMinScale = 0.96f;
        [SerializeField] float pulseMaxScale = 1.04f;
        [SerializeField] float pulseDuration = 2f;
        [Header("Star Unlock Fly FX")]
        [Tooltip("How long the big star stays at the board center before flying.")]
        [SerializeField] float starFlyHoldDuration = 0.35f;
        [Tooltip("How long the star takes to fly to the scorebar star.")]
        [SerializeField] float starFlyDuration = 0.55f;
        [Tooltip("UI size of the big star above the board.")]
        [SerializeField] float starFlyStartSize = 180f;
        [Tooltip("Scale when it lands on the scorebar star (1 = keep start size).")]
        [SerializeField] float starFlyEndScale = 0.35f;
        [Tooltip("Arc height as a fraction of start size.")]
        [SerializeField] float starFlyArcHeight = 0.35f;
        [Tooltip("Peak scale of the one pulse during hold (1 = no pulse).")]
        [SerializeField] float starFlyHoldPulseMax = 1.25f;
        [Tooltip("Extra delay before each subsequent simultaneous star starts flying.")]
        [SerializeField] float starFlyStaggerDelay = 0.28f;
        [SerializeField] int starFlyOverlaySortOrder = 110;

        public static Match3ScoreUI Instance { get; private set; }

        int _matchedCount;
        int _turnsLeft;
        float _displayFill;
        float _targetFill;
        Image[] _stars;
        bool[] _unlocked;
        Coroutine[] _pulseRoutines;
        Coroutine[] _flyRoutines;
        Vector3[] _baseScales;
        RectTransform _starFlyOverlayRoot;
        Canvas _starFlyOverlayCanvas;

        public int MatchedCount => _matchedCount;
        public int TargetMatchCount => Mathf.Max(1, targetMatchCount);
        public int GoalFoodId => goalFoodId;
        public int TurnsLeft => _turnsLeft;
        public bool HasTurnsLeft => _turnsLeft > 0;
        public float Progress => Mathf.Clamp01((float)_matchedCount / TargetMatchCount);
        public int EarnedStars
        {
            get
            {
                float progress = Progress;
                int stars = 0;
                for (int i = 0; i < StarThresholds.Length; i++)
                {
                    if (progress + 0.0001f >= StarThresholds[i])
                        stars++;
                }
                return stars;
            }
        }
        public bool IsLevelOver => Progress >= 1f - 0.0001f || _turnsLeft <= 0;

        bool _resultShown;

        void Awake()
        {
            Instance = this;
            _stars = new[] { star1, star2, star3 };
            _unlocked = new bool[3];
            _pulseRoutines = new Coroutine[3];
            _flyRoutines = new Coroutine[3];
            _baseScales = new Vector3[3];
            ApplyPendingLevel();
            ResetScore();
            _resultShown = false;
        }

        void Start()
        {
            // Same issue as fly-to-goal: first conversion on a brand-new overlay is wrong.
            EnsureStarFlyOverlay();
            Canvas.ForceUpdateCanvases();
        }

        void ApplyPendingLevel()
        {
            if (GameManager.Instance == null || !GameManager.Instance.HasPendingMatch3Level)
                return;

            var level = GameManager.Instance.ActiveMatch3Level;
            if (level == null)
                return;

            if (level.targetMatchCount > 0)
                targetMatchCount = level.targetMatchCount;
            if (level.maxTurns >= 0)
                maxTurns = level.maxTurns;
            if (level.goalFoodId > 0)
                goalFoodId = level.goalFoodId;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_starFlyOverlayRoot != null)
                Destroy(_starFlyOverlayRoot.gameObject);
        }

        void Update()
        {
            if (fillImage == null)
                return;

            if (Mathf.Abs(_displayFill - _targetFill) < 0.0001f)
            {
                _displayFill = _targetFill;
                fillImage.fillAmount = _displayFill;
                return;
            }

            _displayFill = Mathf.MoveTowards(_displayFill, _targetFill, fillLerpSpeed * Time.deltaTime);
            fillImage.fillAmount = _displayFill;
        }

        public void AddClearedCells(IList<Cell> cleared)
        {
            if (cleared == null || cleared.Count == 0)
                return;

            int gained = 0;
            for (int i = 0; i < cleared.Count; i++)
            {
                var cell = cleared[i];
                if (cell != null && cell.IsNormal && cell.ColorId == goalFoodId)
                    gained++;
            }

            if (gained > 0)
                AddProgress(gained);
        }

        public void AddProgress(int amount)
        {
            if (amount <= 0 || _matchedCount >= TargetMatchCount)
                return;

            _matchedCount = Mathf.Min(TargetMatchCount, _matchedCount + amount);
            _targetFill = Progress;
            RefreshTargetText();
            RefreshStars();
        }

        public void ResetScore()
        {
            for (int i = 0; i < _pulseRoutines.Length; i++)
            {
                if (_pulseRoutines[i] != null)
                {
                    StopCoroutine(_pulseRoutines[i]);
                    _pulseRoutines[i] = null;
                }
            }

            for (int i = 0; i < _flyRoutines.Length; i++)
            {
                if (_flyRoutines[i] != null)
                {
                    StopCoroutine(_flyRoutines[i]);
                    _flyRoutines[i] = null;
                }
            }

            _matchedCount = 0;
            _turnsLeft = Mathf.Max(0, maxTurns);
            _resultShown = false;
            ResetVisuals();
            RefreshTargetText();
            RefreshTurnsText();
        }

        /// <summary>Call after a move/cascade fully resolves.</summary>
        public void TryShowResultIfFinished()
        {
            if (_resultShown || !IsLevelOver)
                return;

            _resultShown = true;
            if (Match3ResultUI.Instance != null)
                Match3ResultUI.Instance.Show(EarnedStars);
        }

        void RefreshTargetText()
        {
            if (targetText != null)
                targetText.text = $"{_matchedCount}/{TargetMatchCount}";
        }

        public void SetGoalSprite(Sprite sprite)
        {
            if (goalIcon == null)
                return;
            goalIcon.sprite = sprite;
            goalIcon.enabled = sprite != null;
        }

        public RectTransform GoalIconRect => goalIcon != null ? goalIcon.rectTransform : null;

        /// <summary>Screen-space center of the goal icon (for UI fly-to-goal FX).</summary>
        public bool TryGetGoalScreenPoint(out Vector2 screenPoint)
        {
            screenPoint = default;
            if (goalIcon == null)
                return false;

            var canvas = goalIcon.canvas;
            Camera uiCam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera;

            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, goalIcon.rectTransform.position);
            return true;
        }

        public bool ConsumeTurn()
        {
            if (_turnsLeft <= 0)
                return false;

            _turnsLeft--;
            RefreshTurnsText();
            return true;
        }

        void RefreshTurnsText()
        {
            if (turnsLeftText != null)
                turnsLeftText.text = _turnsLeft.ToString();
        }

        void RefreshStars()
        {
            float progress = Progress;
            int stagger = 0;
            for (int i = 0; i < StarThresholds.Length; i++)
            {
                if (progress + 0.0001f < StarThresholds[i])
                    continue;
                if (_unlocked[i])
                    continue;
                UnlockStar(i, stagger);
                stagger++;
            }
        }

        void ResetVisuals()
        {
            _displayFill = 0f;
            _targetFill = 0f;
            if (fillImage != null)
                fillImage.fillAmount = 0f;

            for (int i = 0; i < _stars.Length; i++)
            {
                _unlocked[i] = false;
                if (_stars[i] == null)
                    continue;
                Match3StarVisuals.SetEarned(_stars[i], false);
                _baseScales[i] = _stars[i].rectTransform.localScale;
                if (_baseScales[i].sqrMagnitude < 0.0001f)
                    _baseScales[i] = Vector3.one;
                _stars[i].rectTransform.localScale = _baseScales[i];
            }
        }

        void UnlockStar(int index, int staggerIndex = 0)
        {
            if (index < 0 || index >= _stars.Length || _unlocked[index])
                return;
            if (_stars[index] == null)
                return;

            _unlocked[index] = true;
            if (_flyRoutines[index] != null)
                StopCoroutine(_flyRoutines[index]);
            _flyRoutines[index] = StartCoroutine(FlyStarUnlock(index, staggerIndex));
        }

        IEnumerator FlyStarUnlock(int index, int staggerIndex)
        {
            var targetStar = _stars[index];
            Sprite starSprite = Match3StarVisuals.EarnedStarSprite;
            if (starSprite == null && targetStar != null)
                starSprite = targetStar.sprite;

            if (starSprite == null || targetStar == null || Match3BoardView.Instance == null)
            {
                FinishStarUnlock(index);
                yield break;
            }

            EnsureStarFlyOverlay();
            if (_starFlyOverlayRoot == null)
            {
                FinishStarUnlock(index);
                yield break;
            }

            // Wait one frame so CanvasScaler / rect size are valid before converting points.
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (!Match3BoardView.Instance.TryGetBoardCenterScreenPoint(out var startScreen)
                || !TryGetStarScreenPoint(index, out var endScreen))
            {
                FinishStarUnlock(index);
                yield break;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _starFlyOverlayRoot, startScreen, null, out var startLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _starFlyOverlayRoot, endScreen, null, out var endLocal))
            {
                FinishStarUnlock(index);
                yield break;
            }

            float startSize = Mathf.Max(40f, starFlyStartSize);
            var go = new GameObject("FlyEarnedStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_starFlyOverlayRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(startSize, startSize);
            rt.anchoredPosition = startLocal;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();

            var image = go.GetComponent<Image>();
            image.sprite = starSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            // One pulse during hold (sin 0→π → scale 1→max→1).
            float hold = Mathf.Max(0.05f, starFlyHoldDuration);
            float pulseMax = Mathf.Max(1f, starFlyHoldPulseMax);
            float holdT = 0f;
            while (holdT < hold && rt != null)
            {
                holdT += Time.deltaTime;
                float k = Mathf.Clamp01(holdT / hold);
                float scale = Mathf.Lerp(1f, pulseMax, Mathf.Sin(k * Mathf.PI));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (rt != null)
                rt.localScale = Vector3.one;

            // Stagger fly start when multiple stars unlock together.
            float stagger = Mathf.Max(0, staggerIndex) * Mathf.Max(0f, starFlyStaggerDelay);
            if (stagger > 0f)
                yield return new WaitForSeconds(stagger);

            // Refresh end point in case UI moved.
            if (TryGetStarScreenPoint(index, out endScreen)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _starFlyOverlayRoot, endScreen, null, out var refreshedEnd))
            {
                endLocal = refreshedEnd;
            }

            float duration = Mathf.Max(0.05f, starFlyDuration);
            float endScale = Mathf.Clamp01(starFlyEndScale);
            float arc = startSize * starFlyArcHeight;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float k = Smooth(Mathf.Clamp01(t / duration));
                var pos = Vector2.Lerp(startLocal, endLocal, k);
                pos.y += Mathf.Sin(k * Mathf.PI) * arc;
                rt.anchoredPosition = pos;
                float scale = Mathf.Lerp(1f, endScale, k);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (go != null)
                Destroy(go);

            FinishStarUnlock(index);
            _flyRoutines[index] = null;
        }

        void FinishStarUnlock(int index)
        {
            if (index < 0 || index >= _stars.Length || _stars[index] == null)
                return;

            Match3StarVisuals.SetEarned(_stars[index], true);

            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(AudioManager.GetStar);

            if (_pulseRoutines[index] != null)
                StopCoroutine(_pulseRoutines[index]);
            _pulseRoutines[index] = StartCoroutine(PulseStar(index));
        }

        bool TryGetStarScreenPoint(int index, out Vector2 screenPoint)
        {
            screenPoint = default;
            if (index < 0 || index >= _stars.Length || _stars[index] == null)
                return false;

            var star = _stars[index];
            var canvas = star.canvas;
            Camera uiCam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera;

            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, star.rectTransform.position);
            return true;
        }

        void EnsureStarFlyOverlay()
        {
            if (_starFlyOverlayRoot != null)
            {
                if (_starFlyOverlayCanvas != null)
                    _starFlyOverlayCanvas.sortingOrder = starFlyOverlaySortOrder;
                return;
            }

            var go = new GameObject("Star Unlock Fly Overlay");
            _starFlyOverlayRoot = go.AddComponent<RectTransform>();
            _starFlyOverlayCanvas = go.AddComponent<Canvas>();
            _starFlyOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _starFlyOverlayCanvas.sortingOrder = starFlyOverlaySortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1156f, 2510f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            _starFlyOverlayRoot.anchorMin = Vector2.zero;
            _starFlyOverlayRoot.anchorMax = Vector2.one;
            _starFlyOverlayRoot.offsetMin = Vector2.zero;
            _starFlyOverlayRoot.offsetMax = Vector2.zero;
            _starFlyOverlayRoot.pivot = new Vector2(0.5f, 0.5f);
            Canvas.ForceUpdateCanvases();
        }

        static float Smooth(float k) => k * k * (3f - 2f * k);

        IEnumerator PulseStar(int index)
        {
            var rt = _stars[index].rectTransform;
            var baseScale = _baseScales[index];
            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, wave);
                rt.localScale = baseScale * scale;
                yield return null;
            }

            rt.localScale = baseScale;
            _pulseRoutines[index] = null;
        }
    }
}
