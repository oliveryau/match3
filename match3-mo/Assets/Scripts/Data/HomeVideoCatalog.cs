using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Match3
{
    public enum HomeVideoPlaybackMode
    {
        HorizontalDrag = 0,
        Segmented = 1,
        Normal = 2
    }

    [Serializable]
    public struct HomeVideoSegment
    {
        [Tooltip("Unused for playback. Kept for inspector layout.")]
        public float startSeconds;
        [Tooltip("Pause the looping video at this time (seconds).")]
        public float endSeconds;
    }

    [Serializable]
    public class HomeVideoEntry
    {
        [Tooltip("Stable id used by code / HomeVideoDirector.")]
        public HomeVideoId id;
        public VideoClip clip;
        public HomeVideoPlaybackMode mode = HomeVideoPlaybackMode.Normal;
        [Tooltip("When enabled, the clip loops. Normal mode should leave this off.")]
        public bool loop = true;
        [Tooltip("Mute this home video's audio when playing.")]
        public bool mute = true;
        [Tooltip("For Segmented mode: each End Seconds is a pause point while the clip loops.")]
        public HomeVideoSegment[] segments;
        [Tooltip("Shown on Button - Left at the 1st, 3rd, ... pause.")]
        public Sprite leftButtonSprite;
        [Tooltip("Shown on Button - Right at the 2nd, 4th, ... pause.")]
        public Sprite rightButtonSprite;
        [Tooltip("Match3 level loaded by Button - Left for this street video.")]
        public Match3LevelConfig leftLevel = new Match3LevelConfig();
        [Tooltip("Match3 level loaded by Button - Right for this street video.")]
        public Match3LevelConfig rightLevel = new Match3LevelConfig();
        [HideInInspector]
        public int loopToSegment;

        public Match3LevelConfig GetLevel(StreetMatch3Slot slot)
        {
            return slot == StreetMatch3Slot.Left ? leftLevel : rightLevel;
        }
    }

    public enum HomeVideoId
    {
        NormalDay = 0,
        [Obsolete("Removed from catalog")]
        NormalPreppingVacation = 1,
        NormalNight = 2,
        NormalStreet = 3,
        VacationDay = 4,
        VacationNight = 5,
        VacationStreet = 6,
        Micro1 = 7,
        Micro2 = 8,
        Micro3 = 9,
        Micro4 = 10,
        Micro5 = 11,
        Micro6 = 12,
        Micro7 = 13
    }

    [CreateAssetMenu(fileName = "HomeVideoCatalog", menuName = "Match3/Home Video Catalog")]
    public class HomeVideoCatalog : ScriptableObject
    {
        [SerializeField] List<HomeVideoEntry> videos = new List<HomeVideoEntry>();

        public IReadOnlyList<HomeVideoEntry> Videos => videos;

        public int VideoCount => videos != null ? videos.Count : 0;

        public VideoClip GetClip(HomeVideoId id)
        {
            var entry = GetEntry(id);
            return entry != null ? entry.clip : null;
        }

        public HomeVideoEntry GetEntry(HomeVideoId id)
        {
            if (videos == null)
                return null;

            for (int i = 0; i < videos.Count; i++)
            {
                var entry = videos[i];
                if (entry != null && entry.id == id)
                    return entry;
            }

            Debug.LogWarning($"HomeVideoCatalog: no entry with id {id}.");
            return null;
        }

        public Match3LevelConfig GetLevel(HomeVideoId id, StreetMatch3Slot slot)
        {
            var entry = GetEntry(id);
            return entry != null ? entry.GetLevel(slot) : null;
        }

        public int IndexOf(HomeVideoId id)
        {
            if (videos == null)
                return -1;

            for (int i = 0; i < videos.Count; i++)
            {
                if (videos[i] != null && videos[i].id == id)
                    return i;
            }

            return -1;
        }

        public HomeVideoId GetIdAt(int index)
        {
            if (videos == null || index < 0 || index >= videos.Count || videos[index] == null)
                return HomeVideoId.NormalDay;
            return videos[index].id;
        }
    }
}
