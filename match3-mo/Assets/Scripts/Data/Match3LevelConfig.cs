using System;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Per street-button Match3 setup. Embedded on segmented HomeVideoEntry left/right levels.
    /// </summary>
    [Serializable]
    public class Match3LevelConfig
    {
        [Tooltip("Food / tile sprites for this level (also sets color count).")]
        public Sprite[] colorSprites;
        public Sprite missileH;
        public Sprite missileV;
        public Sprite propeller;
        public Sprite powderKeg;
        public Sprite lightBall;
        public Sprite obstacle;
        [Tooltip("1-based color id for the goal icon / match target.")]
        public int goalColorId = 1;
        public int targetMatchCount = 15;
        public int maxTurns = 99;
    }

    public enum StreetMatch3Slot
    {
        Left = 0,
        Right = 1
    }
}
