using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Local per-player progress (PlayerPrefs). Device-only; not cloud-synced.
    /// </summary>
    [Serializable]
    public class PlayerProgressData
    {
        public List<string> keys = new List<string>();
        public List<int> stars = new List<int>();
    }

    [Serializable]
    public class PlayerNameRegistry
    {
        public List<string> names = new List<string>();
    }

    public static class PlayerProgress
    {
        public const string PlayerNamePrefsKey = "PlayerName";
        public const string PrefsPrefix = "Match3Progress_";
        public const string NameRegistryPrefsKey = "Match3PlayerNames";

        static string _playerKey = string.Empty;
        static readonly Dictionary<string, int> LevelStars = new Dictionary<string, int>();

        public static string LevelKey(HomeVideoId videoId, StreetMatch3Slot slot) =>
            $"{videoId}_{slot}";

        public static void LoadForPlayer(string playerName)
        {
            LevelStars.Clear();
            RegisterPlayerName(playerName);
            _playerKey = PrefsKey(playerName);
            string json = PlayerPrefs.GetString(_playerKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var data = JsonUtility.FromJson<PlayerProgressData>(json);
                if (data?.keys == null || data.stars == null)
                    return;

                int count = Mathf.Min(data.keys.Count, data.stars.Count);
                for (int i = 0; i < count; i++)
                {
                    if (string.IsNullOrEmpty(data.keys[i]))
                        continue;
                    LevelStars[data.keys[i]] = Mathf.Clamp(data.stars[i], 0, 3);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PlayerProgress: failed to load ({e.Message}).");
            }
        }

        public static int GetStars(HomeVideoId videoId, StreetMatch3Slot slot) =>
            GetStars(LevelKey(videoId, slot));

        public static int GetStars(string levelKey)
        {
            if (string.IsNullOrEmpty(levelKey))
                return 0;
            return LevelStars.TryGetValue(levelKey, out int stars) ? stars : 0;
        }

        /// <summary>Keeps the best star count for this level.</summary>
        public static void RecordStars(string levelKey, int stars)
        {
            if (string.IsNullOrEmpty(levelKey) || string.IsNullOrEmpty(_playerKey))
                return;

            stars = Mathf.Clamp(stars, 0, 3);
            if (LevelStars.TryGetValue(levelKey, out int existing) && existing >= stars)
                return;

            LevelStars[levelKey] = stars;
            Save();
        }

        public static List<string> GetRegisteredPlayerNames()
        {
            var names = new List<string>();
            string json = PlayerPrefs.GetString(NameRegistryPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return names;

            try
            {
                var registry = JsonUtility.FromJson<PlayerNameRegistry>(json);
                if (registry?.names == null)
                    return names;

                for (int i = 0; i < registry.names.Count; i++)
                {
                    string name = registry.names[i];
                    if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                        names.Add(name);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PlayerProgress: failed to read name registry ({e.Message}).");
            }

            return names;
        }

        public static void RegisterPlayerName(string playerName)
        {
            string name = NormalizeName(playerName);
            if (name == "_guest")
                return;

            var names = GetRegisteredPlayerNames();
            if (names.Contains(name))
                return;

            names.Add(name);
            var registry = new PlayerNameRegistry { names = names };
            PlayerPrefs.SetString(NameRegistryPrefsKey, JsonUtility.ToJson(registry));
            PlayerPrefs.Save();
        }

        public static void ClearPlayer(string playerName)
        {
            string name = NormalizeName(playerName);
            PlayerPrefs.DeleteKey(PrefsKey(name));

            var names = GetRegisteredPlayerNames();
            names.Remove(name);
            if (names.Count == 0)
                PlayerPrefs.DeleteKey(NameRegistryPrefsKey);
            else
                PlayerPrefs.SetString(NameRegistryPrefsKey, JsonUtility.ToJson(new PlayerNameRegistry { names = names }));

            string current = PlayerPrefs.GetString(PlayerNamePrefsKey, string.Empty);
            if (string.Equals(NormalizeName(current), name, StringComparison.Ordinal))
                PlayerPrefs.DeleteKey(PlayerNamePrefsKey);

            if (_playerKey == PrefsKey(name))
            {
                LevelStars.Clear();
                _playerKey = string.Empty;
            }

            PlayerPrefs.Save();
        }

        public static void ClearAllPlayers()
        {
            var names = GetRegisteredPlayerNames();
            for (int i = 0; i < names.Count; i++)
                PlayerPrefs.DeleteKey(PrefsKey(names[i]));

            PlayerPrefs.DeleteKey(PrefsKey("_guest"));
            PlayerPrefs.DeleteKey(NameRegistryPrefsKey);
            PlayerPrefs.DeleteKey(PlayerNamePrefsKey);
            LevelStars.Clear();
            _playerKey = string.Empty;
            PlayerPrefs.Save();
        }

        static void Save()
        {
            var data = new PlayerProgressData();
            foreach (var kv in LevelStars)
            {
                data.keys.Add(kv.Key);
                data.stars.Add(kv.Value);
            }

            PlayerPrefs.SetString(_playerKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        static string PrefsKey(string playerName) => PrefsPrefix + NormalizeName(playerName);

        static string NormalizeName(string playerName) =>
            string.IsNullOrWhiteSpace(playerName) ? "_guest" : playerName.Trim();
    }
}
