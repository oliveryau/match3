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
        public bool phoneNotificationDismissed;
        public bool suzhouFanPopupSeen;
        public bool friendsPhotoPopupSeen;
        public bool moneyPlantPopupSeen;
        public bool suzhouFanUnlocked;
        public bool friendsPhotoUnlocked;
        public bool moneyPlantUnlocked;
        public bool homeDragIntroCompleted;
        public bool achievement1Watched;
        public bool achievement2Watched;
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
        static bool _phoneNotificationDismissed;
        static bool _suzhouFanPopupSeen;
        static bool _friendsPhotoPopupSeen;
        static bool _moneyPlantPopupSeen;
        static bool _suzhouFanUnlocked;
        static bool _friendsPhotoUnlocked;
        static bool _moneyPlantUnlocked;
        static bool _homeDragIntroCompleted;
        static bool _achievement1Watched;
        static bool _achievement2Watched;

        public static string LevelKey(HomeVideoId videoId, StreetMatch3Slot slot) =>
            $"{videoId}_{slot}";

        public static void LoadForPlayer(string playerName)
        {
            LevelStars.Clear();
            _phoneNotificationDismissed = false;
            _suzhouFanPopupSeen = false;
            _friendsPhotoPopupSeen = false;
            _moneyPlantPopupSeen = false;
            _suzhouFanUnlocked = false;
            _friendsPhotoUnlocked = false;
            _moneyPlantUnlocked = false;
            _homeDragIntroCompleted = false;
            _achievement1Watched = false;
            _achievement2Watched = false;
            RegisterPlayerName(playerName);
            _playerKey = PrefsKey(playerName);
            string json = PlayerPrefs.GetString(_playerKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var data = JsonUtility.FromJson<PlayerProgressData>(json);
                if (data == null)
                    return;

                if (data.keys != null && data.stars != null)
                {
                    int count = Mathf.Min(data.keys.Count, data.stars.Count);
                    for (int i = 0; i < count; i++)
                    {
                        if (string.IsNullOrEmpty(data.keys[i]))
                            continue;
                        LevelStars[data.keys[i]] = Mathf.Clamp(data.stars[i], 0, 3);
                    }
                }

                _phoneNotificationDismissed = data.phoneNotificationDismissed;
                _suzhouFanPopupSeen = data.suzhouFanPopupSeen;
                _friendsPhotoPopupSeen = data.friendsPhotoPopupSeen;
                _moneyPlantPopupSeen = data.moneyPlantPopupSeen;
                _suzhouFanUnlocked = data.suzhouFanUnlocked;
                _friendsPhotoUnlocked = data.friendsPhotoUnlocked;
                _moneyPlantUnlocked = data.moneyPlantUnlocked;
                _homeDragIntroCompleted = data.homeDragIntroCompleted;
                _achievement1Watched = data.achievement1Watched;
                _achievement2Watched = data.achievement2Watched;
                // Existing saves from before this flag: skip intro if they already played.
                if (!_homeDragIntroCompleted
                    && ((data.keys != null && data.keys.Count > 0)
                        || data.suzhouFanUnlocked
                        || data.friendsPhotoUnlocked
                        || data.moneyPlantUnlocked
                        || data.phoneNotificationDismissed))
                {
                    _homeDragIntroCompleted = true;
                }
                SyncPhotoCollectibleUnlocks(saveIfChanged: true);
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

        /// <summary>Sum of best stars across all levels for the loaded player.</summary>
        public static int GetTotalStars()
        {
            int total = 0;
            foreach (var kv in LevelStars)
                total += Mathf.Clamp(kv.Value, 0, 3);
            return total;
        }

        public static bool IsPhoneNotificationDismissed() => _phoneNotificationDismissed;

        /// <summary>After Agree on phone bubble — notification never shows again for this player.</summary>
        public static void DismissPhoneNotification()
        {
            if (_phoneNotificationDismissed || string.IsNullOrEmpty(_playerKey))
                return;
            _phoneNotificationDismissed = true;
            Save();
        }

        public static bool HasCompletedHomeDragIntro() => _homeDragIntroCompleted;

        public static void MarkHomeDragIntroCompleted()
        {
            if (_homeDragIntroCompleted || string.IsNullOrEmpty(_playerKey))
                return;
            _homeDragIntroCompleted = true;
            Save();
        }

        public static bool HasWatchedAchievement1() => _achievement1Watched;

        public static bool HasWatchedAchievement2() => _achievement2Watched;

        public static void MarkAchievement1Watched()
        {
            if (_achievement1Watched)
                return;
            _achievement1Watched = true;
            if (!string.IsNullOrEmpty(_playerKey))
                Save();
        }

        public static void MarkAchievement2Watched()
        {
            if (_achievement2Watched)
                return;
            _achievement2Watched = true;
            if (!string.IsNullOrEmpty(_playerKey))
                Save();
        }

        public static bool HasSeenSuzhouFanPopup() => _suzhouFanPopupSeen;

        public static bool HasSeenFriendsPhotoPopup() => _friendsPhotoPopupSeen;

        public static bool HasSeenMoneyPlantPopup() => _moneyPlantPopupSeen;

        public static bool IsSuzhouFanUnlocked() => _suzhouFanUnlocked;

        public static bool IsFriendsPhotoUnlocked() => _friendsPhotoUnlocked;

        public static bool IsMoneyPlantUnlocked() => _moneyPlantUnlocked;

        public static void MarkSuzhouFanPopupSeen()
        {
            if (_suzhouFanPopupSeen || string.IsNullOrEmpty(_playerKey))
                return;
            _suzhouFanPopupSeen = true;
            Save();
        }

        public static void MarkFriendsPhotoPopupSeen()
        {
            if (_friendsPhotoPopupSeen || string.IsNullOrEmpty(_playerKey))
                return;
            _friendsPhotoPopupSeen = true;
            Save();
        }

        public static void MarkMoneyPlantPopupSeen()
        {
            if (_moneyPlantPopupSeen || string.IsNullOrEmpty(_playerKey))
                return;
            _moneyPlantPopupSeen = true;
            Save();
        }

        /// <summary>
        /// Persist photo collectibles from 3★ clears so they stay unlocked on Normal Day.
        /// </summary>
        public static void SyncPhotoCollectibleUnlocks(bool saveIfChanged = true)
        {
            bool dirty = false;

            if (!_suzhouFanUnlocked
                && (GetStars(HomeVideoId.VacationStreet, StreetMatch3Slot.Left) >= 3
                    || GetStars(HomeVideoId.VacationStreet, StreetMatch3Slot.Right) >= 3))
            {
                _suzhouFanUnlocked = true;
                dirty = true;
            }

            if (!_friendsPhotoUnlocked
                && (GetStars(HomeVideoId.Micro3, StreetMatch3Slot.Left) >= 3
                    || GetStars(HomeVideoId.Micro3, StreetMatch3Slot.Right) >= 3))
            {
                _friendsPhotoUnlocked = true;
                dirty = true;
            }

            if (!_moneyPlantUnlocked && GetTotalStars() >= 9)
            {
                _moneyPlantUnlocked = true;
                dirty = true;
            }

            if (dirty && saveIfChanged && !string.IsNullOrEmpty(_playerKey))
                Save();
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
            SyncPhotoCollectibleUnlocks(saveIfChanged: false);
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
                _phoneNotificationDismissed = false;
                _suzhouFanPopupSeen = false;
                _friendsPhotoPopupSeen = false;
                _moneyPlantPopupSeen = false;
                _suzhouFanUnlocked = false;
                _friendsPhotoUnlocked = false;
                _moneyPlantUnlocked = false;
                _homeDragIntroCompleted = false;
                _achievement1Watched = false;
                _achievement2Watched = false;
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
            _phoneNotificationDismissed = false;
            _suzhouFanPopupSeen = false;
            _friendsPhotoPopupSeen = false;
            _moneyPlantPopupSeen = false;
            _suzhouFanUnlocked = false;
            _friendsPhotoUnlocked = false;
            _moneyPlantUnlocked = false;
            _homeDragIntroCompleted = false;
            _achievement1Watched = false;
            _achievement2Watched = false;
            _playerKey = string.Empty;
            PlayerPrefs.Save();
        }

        static void Save()
        {
            var data = new PlayerProgressData
            {
                phoneNotificationDismissed = _phoneNotificationDismissed,
                suzhouFanPopupSeen = _suzhouFanPopupSeen,
                friendsPhotoPopupSeen = _friendsPhotoPopupSeen,
                moneyPlantPopupSeen = _moneyPlantPopupSeen,
                suzhouFanUnlocked = _suzhouFanUnlocked,
                friendsPhotoUnlocked = _friendsPhotoUnlocked,
                moneyPlantUnlocked = _moneyPlantUnlocked,
                homeDragIntroCompleted = _homeDragIntroCompleted,
                achievement1Watched = _achievement1Watched,
                achievement2Watched = _achievement2Watched
            };
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
