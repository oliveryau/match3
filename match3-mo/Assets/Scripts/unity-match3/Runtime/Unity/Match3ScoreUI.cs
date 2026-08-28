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

        public static Match3ScoreUI Instance { get; private set; }

        int _matchedCount;
        int _turnsLeft;
        float _displayFill;
        float _targetFill;
        Image[] _stars;
        bool[] _unlocked;
        Coroutine[] _pulseRoutines;
        Vector3[] _baseScales;

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
            _baseScales = new Vector3[3];
            ApplyPendingLevel();
            ResetScore();
            _resultShown = false;
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
                targetText.text = $"x{_matchedCount}/{TargetMatchCount}";
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
            for (int i = 0; i < StarThresholds.Length; i++)
            {
                if (progress + 0.0001f >= StarThresholds[i])
                    UnlockStar(i);
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

        void UnlockStar(int index)
        {
            if (index < 0 || index >= _stars.Length || _unlocked[index])
                return;
            if (_stars[index] == null)
                return;

            _unlocked[index] = true;
            Match3StarVisuals.SetEarned(_stars[index], true);

            if (_pulseRoutines[index] != null)
                StopCoroutine(_pulseRoutines[index]);
            _pulseRoutines[index] = StartCoroutine(PulseStar(index));
        }

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
