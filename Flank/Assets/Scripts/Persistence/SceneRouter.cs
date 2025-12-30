using UnityEngine;

public sealed class SceneRouter : MonoBehaviour
{
    public static SceneRouter Instance { get; private set; }

    private const string PanelManagerLastPanelKey = "PanelManager.LastPanelName";

    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string navigationSceneName = "NavigationScene";
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sceneLoader == null)
        {
            sceneLoader = GetComponent<SceneLoader>();
        }
    }

    public void GoToGame()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadSingle(gameSceneName);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    public void GoToNavigation(bool openLastPanel)
    {
        if (!openLastPanel)
        {
            PlayerPrefs.DeleteKey(PanelManagerLastPanelKey);
            PlayerPrefs.Save();
        }

        if (sceneLoader != null)
        {
            sceneLoader.LoadSingle(navigationSceneName);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(navigationSceneName);
    }
}
