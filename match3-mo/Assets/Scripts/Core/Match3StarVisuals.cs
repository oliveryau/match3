using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public static class Match3StarVisuals
    {
        static Sprite _star;
        static Sprite _starEmpty;

        public static void SetSprites(Sprite star, Sprite starEmpty)
        {
            _star = star;
            _starEmpty = starEmpty;
        }

        public static void Apply(Image star1, Image star2, Image star3, int earnedStars)
        {
            Apply(new[] { star1, star2, star3 }, earnedStars);
        }

        public static void Apply(Image[] stars, int earnedStars)
        {
            if (stars == null)
                return;

            int earned = Mathf.Clamp(earnedStars, 0, 3);
            for (int i = 0; i < stars.Length; i++)
                SetEarned(stars[i], i < earned);
        }

        public static void SetEarned(Image image, bool earned)
        {
            if (image == null)
                return;

            image.color = Color.white;
            Sprite sprite = earned ? _star : _starEmpty;
            if (sprite != null)
                image.sprite = sprite;
        }
    }
}
