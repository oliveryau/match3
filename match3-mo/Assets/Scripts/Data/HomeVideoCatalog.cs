using UnityEngine;
using UnityEngine.Video;

namespace Match3
{
    [CreateAssetMenu(fileName = "HomeVideoCatalog", menuName = "Match3/Home Video Catalog")]
    public class HomeVideoCatalog : ScriptableObject
    {
        public const int DayCount = 4;
        public const int VideosPerDay = 3;

        [Header("Day 1")]
        [SerializeField, InspectorName("day 1 - video 1")] private VideoClip day1Video1;
        [SerializeField, InspectorName("day 1 - video 2")] private VideoClip day1Video2;
        [SerializeField, InspectorName("day 1 - video 3")] private VideoClip day1Video3;

        [Header("Day 2")]
        [SerializeField, InspectorName("day 2 - video 1")] private VideoClip day2Video1;
        [SerializeField, InspectorName("day 2 - video 2")] private VideoClip day2Video2;
        [SerializeField, InspectorName("day 2 - video 3")] private VideoClip day2Video3;

        [Header("Day 3")]
        [SerializeField, InspectorName("day 3 - video 1")] private VideoClip day3Video1;
        [SerializeField, InspectorName("day 3 - video 2")] private VideoClip day3Video2;
        [SerializeField, InspectorName("day 3 - video 3")] private VideoClip day3Video3;

        [Header("Day 4")]
        [SerializeField, InspectorName("day 4 - video 1")] private VideoClip day4Video1;
        [SerializeField, InspectorName("day 4 - video 2")] private VideoClip day4Video2;
        [SerializeField, InspectorName("day 4 - video 3")] private VideoClip day4Video3;

        public VideoClip GetClip(int day, int video)
        {
            switch (day)
            {
                case 1:
                    return GetDay1(video);
                case 2:
                    return GetDay2(video);
                case 3:
                    return GetDay3(video);
                case 4:
                    return GetDay4(video);
                default:
                    Debug.LogWarning($"HomeVideoCatalog: day {day} is out of range (1-{DayCount}).");
                    return null;
            }
        }

        private VideoClip GetDay1(int video)
        {
            switch (video)
            {
                case 1: return day1Video1;
                case 2: return day1Video2;
                case 3: return day1Video3;
                default:
                    LogBadVideo(1, video);
                    return null;
            }
        }

        private VideoClip GetDay2(int video)
        {
            switch (video)
            {
                case 1: return day2Video1;
                case 2: return day2Video2;
                case 3: return day2Video3;
                default:
                    LogBadVideo(2, video);
                    return null;
            }
        }

        private VideoClip GetDay3(int video)
        {
            switch (video)
            {
                case 1: return day3Video1;
                case 2: return day3Video2;
                case 3: return day3Video3;
                default:
                    LogBadVideo(3, video);
                    return null;
            }
        }

        private VideoClip GetDay4(int video)
        {
            switch (video)
            {
                case 1: return day4Video1;
                case 2: return day4Video2;
                case 3: return day4Video3;
                default:
                    LogBadVideo(4, video);
                    return null;
            }
        }

        private static void LogBadVideo(int day, int video)
        {
            Debug.LogWarning($"HomeVideoCatalog: video {video} is out of range for day {day} (1-{VideosPerDay}).");
        }
    }
}
