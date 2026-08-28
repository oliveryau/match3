#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    /// <summary>
    /// Forces H.264 + half-res transcode for Android / iPhone.
    /// Your phone rejected 2560x1440 (MediaCodec ERROR_UNSUPPORTED).
    /// Run: Match3 → Fix Video Import For Mobile, wait for transcode, then rebuild.
    /// </summary>
    public static class FixVideoImportForMobile
    {
        const string VideosFolder = "Assets/Videos";

        [MenuItem("Match3/Fix Video Import For Mobile")]
        public static void Run()
        {
            string[] guids = AssetDatabase.FindAssets("t:VideoClip", new[] { VideosFolder });
            int updated = 0;

            // HalfRes: 2560x1440 → 1280x720 (decoder-safe on most Android SoCs).
            var settings = new VideoImporterTargetSettings
            {
                enableTranscoding = true,
                codec = VideoCodec.H264,
                resizeMode = VideoResizeMode.HalfRes,
                aspectRatio = VideoEncodeAspectRatio.NoScaling,
                customWidth = 1280,
                customHeight = 720,
                bitrateMode = VideoBitrateMode.Medium,
                spatialQuality = VideoSpatialQuality.MediumSpatialQuality
            };

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
                if (importer == null)
                    continue;

                importer.importAudio = true;
                importer.SetTargetSettings("Default", Clone(settings));
                importer.SetTargetSettings("Android", Clone(settings));
                importer.SetTargetSettings("iPhone", Clone(settings));
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                updated++;
                Debug.Log($"FixVideoImportForMobile: reimporting {path} (H.264 HalfRes)");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"FixVideoImportForMobile: queued {updated} clip(s). " +
                "Wait until Unity finishes transcoding (progress bar), then build to device. " +
                "Do NOT skip the transcode dialog.");
        }

        static VideoImporterTargetSettings Clone(VideoImporterTargetSettings src)
        {
            return new VideoImporterTargetSettings
            {
                enableTranscoding = src.enableTranscoding,
                codec = src.codec,
                resizeMode = src.resizeMode,
                aspectRatio = src.aspectRatio,
                customWidth = src.customWidth,
                customHeight = src.customHeight,
                bitrateMode = src.bitrateMode,
                spatialQuality = src.spatialQuality
            };
        }
    }
}
#endif
