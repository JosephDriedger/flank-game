using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PostGameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text winText;
    [SerializeField] private TMP_Text turnsText;
    [SerializeField] private TMP_Text flagsRemainingText;
    [SerializeField] private TMP_Text attackersRemainingText;

    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button viewBoardButton;
    [SerializeField] private Button exitGameButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string navigationSceneName = "NavigationScene";

    private void OnEnable()
    {
        ApplyWinner();
        ApplyStats();

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(HandlePlayAgain);
        }

        if (viewBoardButton != null)
        {
            viewBoardButton.onClick.AddListener(HandleViewBoard);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.AddListener(HandleExit);
        }

        // In network mode only the host can initiate Play Again or View Board.
        if (IsNetworkMode())
        {
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            if (playAgainButton != null) playAgainButton.interactable = isHost;
            if (viewBoardButton != null) viewBoardButton.interactable = isHost;
        }
    }

    private void OnDisable()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(HandlePlayAgain);
        }

        if (viewBoardButton != null)
        {
            viewBoardButton.onClick.RemoveListener(HandleViewBoard);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(HandleExit);
        }
    }

    private void HandlePlayAgain()
    {
        if (IsNetworkMode())
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (LanNetworkService.Instance != null) LanNetworkService.Instance.IsViewBoard = false;
                NetworkManager.Singleton.SceneManager.LoadScene(
                    gameSceneName, LoadSceneMode.Single);
            }
            return;
        }

        RestoreLastPlayedSettingsIfAvailable();

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToGame();
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void HandleViewBoard()
    {
        if (IsNetworkMode())
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (LanNetworkService.Instance != null) LanNetworkService.Instance.IsViewBoard = true;
                NetworkManager.Singleton.SceneManager.LoadScene(
                    gameSceneName, LoadSceneMode.Single);
            }
            return;
        }

        SaveString(PostGameKeys.LaunchMode, ((int)GameLaunchMode.ViewBoard).ToString());

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToGame();
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void HandleExit()
    {
        if (IsNetworkMode())
        {
            // Return to the shared lobby panel; stay connected.
            SaveString("PanelManager.LastPanelName", "LanLobbyPanel");
        }

        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToNavigation(openLastPanel: true);
            return;
        }

        SceneManager.LoadScene(navigationSceneName);
    }

    private static bool IsNetworkMode()
    {
        if (GameSettingsManager.Instance == null || GameSettingsManager.Instance.Current == null)
        {
            return false;
        }

        GameMode mode = GameSettingsManager.Instance.Current.mode;
        return mode == GameMode.Lan || mode == GameMode.OnlineMatchmaking;
    }

    private void RestoreLastPlayedSettingsIfAvailable()
    {
        string json = LoadString(PostGameKeys.LastPlayedSettingsJson, string.Empty);
        if (string.IsNullOrWhiteSpace(json) || GameSettingsManager.Instance == null)
        {
            return;
        }

        GameSettingsManager.Instance.ImportJson(json);
    }

    private void ApplyWinner()
    {
        if (winText == null)
        {
            return;
        }

        string result = LoadString(PostGameKeys.LastGameResult, string.Empty);
        GameSettings settings = LoadLastPlayedSettings();

        // In network modes the client may have stale single-player settings
        // (including AI display names). Always use generic role names there.
        bool useRoleNames = settings == null ||
                            settings.mode == GameMode.Lan ||
                            settings.mode == GameMode.OnlineMatchmaking;

        if (result == GameResult.AttackersWin.ToString())
        {
            string name = useRoleNames ? null : settings?.attacker?.displayName;
            winText.text = string.IsNullOrWhiteSpace(name) ? "Attackers Win!" : $"{name} Wins!";
        }
        else if (result == GameResult.DefendersWin.ToString())
        {
            string name = useRoleNames ? null : settings?.defender?.displayName;
            winText.text = string.IsNullOrWhiteSpace(name) ? "Defenders Win!" : $"{name} Wins!";
        }
        else
        {
            winText.text = "Game Over";
        }
    }

    private GameSettings LoadLastPlayedSettings()
    {
        string json = LoadString(PostGameKeys.LastPlayedSettingsJson, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<GameSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyStats()
    {
        int turns = LoadInt(PostGameKeys.TotalTurns, 0);
        int flagsRemaining = LoadInt(PostGameKeys.FlagsRemaining, 0);
        int attackersRemaining = LoadInt(PostGameKeys.AttackersRemaining, 0);

        if (turnsText != null)
        {
            turnsText.text = $"Turns: {turns}";
        }

        if (flagsRemainingText != null)
        {
            flagsRemainingText.text = $"Flags Remaining: {flagsRemaining}";
        }

        if (attackersRemainingText != null)
        {
            attackersRemainingText.text = $"Attackers Remaining: {attackersRemaining}";
        }
    }

    private string LoadString(string key, string fallback)
    {
        if (SaveSystem.Instance != null)
        {
            return SaveSystem.Instance.LoadString(key, fallback);
        }

        return PlayerPrefs.GetString(key, fallback);
    }

    private int LoadInt(string key, int fallback)
    {
        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(key, fallback.ToString());
            return int.TryParse(raw, out int value) ? value : fallback;
        }

        return PlayerPrefs.GetInt(key, fallback);
    }

    private void SaveString(string key, string value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value);
            return;
        }

        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }
}
