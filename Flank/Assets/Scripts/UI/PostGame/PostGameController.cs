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

        playAgainButton.onClick.AddListener(HandlePlayAgain);
        viewBoardButton.onClick.AddListener(HandleViewBoard);
        exitGameButton.onClick.AddListener(HandleExit);
    }

    private void OnDisable()
    {
        playAgainButton.onClick.RemoveListener(HandlePlayAgain);
        viewBoardButton.onClick.RemoveListener(HandleViewBoard);
        exitGameButton.onClick.RemoveListener(HandleExit);
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
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        if (GameSettingsManager.Instance == null)
        {
            return;
        }

        GameSettingsManager.Instance.ImportJson(json);
    }

    private void ApplyWinner()
    {
        string result = LoadString(PostGameKeys.LastGameResult, string.Empty);

        if (result == GameResult.AttackersWin.ToString())
        {
            winText.text = "Attackers Win";
            return;
        }

        if (result == GameResult.DefendersWin.ToString())
        {
            winText.text = "Defenders Win";
            return;
        }

        winText.text = "Game Over";
    }

    private void ApplyStats()
    {
        int turns = LoadInt(PostGameKeys.TotalTurns, 0);
        int flagsRemaining = LoadInt(PostGameKeys.FlagsRemaining, 0);
        int attackersRemaining = LoadInt(PostGameKeys.AttackersRemaining, 0);

        turnsText.text = $"Turns: {turns}";
        flagsRemainingText.text = $"Flags Remaining: {flagsRemaining}";
        attackersRemainingText.text = $"Attackers Remaining: {attackersRemaining}";
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
