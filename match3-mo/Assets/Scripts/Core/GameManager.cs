using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            if (ShouldLoadLanding())
                LoadLanding();
        }

        public void LoadLanding() => LoadScene(SceneIds.Landing);

        public void LoadHome() => LoadScene(SceneIds.Home);

        public void LoadMatch3() => LoadScene(SceneIds.Match3);

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private static bool ShouldLoadLanding()
        {
            if (SceneManager.sceneCount != 1)
                return false;

            return SceneManager.GetActiveScene().name == SceneIds.Boot;
        }
    }
}
