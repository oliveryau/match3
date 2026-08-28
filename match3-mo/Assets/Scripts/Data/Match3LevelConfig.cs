using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;

namespace Match3
{
    /// <summary>
    /// Per street-button Match3 setup. Missile H/V and other shared specials
    /// stay on the Match3 scene board; board bg can be overridden per level.
    /// </summary>
    [Serializable]
    public class Match3LevelConfig
    {
        [Tooltip("Food / tile sprites for this level (also sets color count).")]
        [FormerlySerializedAs("colorSprites")]
        public Sprite[] foodSprites;
        [Tooltip("Optional board background for this level. Leave empty to keep the Match3 scene default.")]
        public Sprite boardBgSprite;
        [Tooltip("1-based food id for the goal icon / match target.")]
        [FormerlySerializedAs("goalColorId")]
        public int goalFoodId = 1;
        public int targetMatchCount = 15;
        public int maxTurns = 99;

        [Header("Level videos")]
        [Tooltip("Loops while idle / between reactions.")]
        public VideoClip video1;
        [Tooltip("Mute video1 audio.")]
        public bool muteVideo1 = true;
        [Tooltip("Plays once when the player clears an exact 3-match.")]
        public VideoClip video2;
        [Tooltip("Mute video2 audio.")]
        public bool muteVideo2 = true;
        [Tooltip("Plays once when the player clears a 4+ match or bursts a gold peach.")]
        public VideoClip video3;
        [Tooltip("Mute video3 audio.")]
        public bool muteVideo3 = true;
    }

    public enum StreetMatch3Slot
    {
        Left = 0,
        Right = 1
    }
}
