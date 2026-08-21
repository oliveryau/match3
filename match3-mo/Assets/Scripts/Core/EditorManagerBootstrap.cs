#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Match3
{
    public static class EditorManagerBootstrap
    {
        private const string GameManagerPath = "Assets/Prefabs/Game Manager.prefab";
        private const string AudioManagerPath = "Assets/Prefabs/Audio Manager.prefab";
        private const string UIManagerPath = "Assets/Prefabs/UI Manager.prefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SpawnManagersIfMissing()
        {
            Spawn(GameManagerPath);
            Spawn(AudioManagerPath);
            Spawn(UIManagerPath);
        }

        private static void Spawn(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"EditorManagerBootstrap: missing prefab at {path}");
                return;
            }

            Object.Instantiate(prefab);
        }
    }
}
#endif
