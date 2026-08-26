#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Match3
{
    public static class PlayerProgressEditorMenu
    {
        const string MenuRoot = "Match3/Player Saves/";

        [MenuItem(MenuRoot + "Clear All Saved Players And Progress", false, 0)]
        static void ClearAll()
        {
            var names = PlayerProgress.GetRegisteredPlayerNames();
            string summary = names.Count == 0
                ? "No registered player names found.\n\nThis will still clear the current PlayerName and guest progress keys."
                : "This will delete local saves for:\n• " + string.Join("\n• ", names);

            if (!EditorUtility.DisplayDialog(
                    "Clear Match3 Player Saves",
                    summary + "\n\nContinue?",
                    "Clear All",
                    "Cancel"))
                return;

            PlayerProgress.ClearAllPlayers();
            if (Application.isPlaying && GameManager.Instance != null)
                GameManager.Instance.SetPlayerName(string.Empty);

            Debug.Log("Match3: cleared all saved player names and progress.");
            EditorUtility.DisplayDialog("Match3 Player Saves", "All local player names and progress were cleared.", "OK");
        }

        [MenuItem(MenuRoot + "List Saved Players", false, 1)]
        static void ListPlayers()
        {
            var names = PlayerProgress.GetRegisteredPlayerNames();
            string current = PlayerPrefs.GetString(PlayerProgress.PlayerNamePrefsKey, string.Empty);
            if (names.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Match3 Player Saves",
                    string.IsNullOrEmpty(current)
                        ? "No saved player names."
                        : $"No registry entries.\nCurrent PlayerName pref: \"{current}\"",
                    "OK");
                return;
            }

            var lines = new List<string>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                string mark = names[i] == current.Trim() ? " (current)" : string.Empty;
                lines.Add($"• {names[i]}{mark}");
            }

            EditorUtility.DisplayDialog("Match3 Player Saves", string.Join("\n", lines), "OK");
        }

        [MenuItem(MenuRoot + "Clear Current Player Only", false, 2)]
        static void ClearCurrent()
        {
            string current = PlayerPrefs.GetString(PlayerProgress.PlayerNamePrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(current))
            {
                EditorUtility.DisplayDialog("Match3 Player Saves", "No current PlayerName is saved.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Clear Current Player",
                    $"Delete local save for \"{current.Trim()}\"?",
                    "Clear",
                    "Cancel"))
                return;

            PlayerProgress.ClearPlayer(current);
            if (Application.isPlaying && GameManager.Instance != null)
                GameManager.Instance.SetPlayerName(string.Empty);

            Debug.Log($"Match3: cleared save for \"{current.Trim()}\".");
            EditorUtility.DisplayDialog("Match3 Player Saves", $"Cleared \"{current.Trim()}\".", "OK");
        }
    }
}
#endif
