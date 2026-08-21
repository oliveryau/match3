using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Match3
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject landingSceneUI;
        [SerializeField] private GameObject homeSceneUI;
        [SerializeField] private HomeVideoCatalog homeVideoCatalog;

        public Canvas Canvas { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Canvas = GetComponent<Canvas>();
            CacheRootsIfNeeded();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            Refresh(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Refresh(scene);
        }

        public void Refresh(Scene scene)
        {
            SetRootActive(landingSceneUI, scene.name == SceneIds.Landing);
            SetRootActive(homeSceneUI, scene.name == SceneIds.Home);
        }

        private void CacheRootsIfNeeded()
        {
            if (landingSceneUI == null)
            {
                var landing = transform.Find("LandingScene");
                if (landing != null)
                    landingSceneUI = landing.gameObject;
            }

            if (homeSceneUI == null)
            {
                var home = transform.Find("HomeScene");
                if (home != null)
                    homeSceneUI = home.gameObject;
            }
        }

        private static void SetRootActive(GameObject root, bool active)
        {
            if (root == null || root.activeSelf == active)
                return;

            if (!active)
                StopVideos(root);

            root.SetActive(active);
        }

        private static void StopVideos(GameObject root)
        {
            var players = root.GetComponentsInChildren<VideoPlayer>(true);
            for (var i = 0; i < players.Length; i++)
                players[i].Stop();
        }
    }
}
