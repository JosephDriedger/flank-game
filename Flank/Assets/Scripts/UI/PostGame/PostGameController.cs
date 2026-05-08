using TMPro;
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
        if (SceneRouter.Instance != null)
        {
            SceneRouter.Instance.GoToNavigation(openLastPanel: true);
            return;
        }

        SceneManager.LoadScene(navigationSceneName);
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

        if (result == GameResult.AttackersWin.ToString())
        {
            string name = settings?.attacker?.displayName;
            winText.text = string.IsNullOrWhiteSpace(name) ? "Attackers Win!" : $"{name} Wins!";
        }
        else if (result == GameResult.DefendersWin.ToString())
        {
            string name = settings?.defender?.displayName;
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
