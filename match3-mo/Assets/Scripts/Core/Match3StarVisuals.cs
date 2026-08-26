using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public static class Match3StarVisuals
    {
        public static readonly Color Locked = new Color(0x42 / 255f, 0x83 / 255f, 1f, 1f);

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
            {
                if (stars[i] == null)
                    continue;
                stars[i].color = i < earned ? Color.white : Locked;
            }
        }

        public static string ResultText(int earnedStars)
        {
            switch (Mathf.Clamp(earnedStars, 0, 3))
            {
                case 1: return "一颗星";
                case 2: return "两颗星";
                case 3: return "三颗星";
                default: return "零颗星";
            }
        }
    }
}
