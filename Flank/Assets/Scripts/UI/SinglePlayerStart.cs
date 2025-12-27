using TMPro;
using UnityEngine;

public sealed class SinglePlayerStart : MonoBehaviour
{
    [SerializeField] private SceneLoader _sceneLoader;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown _difficultyDropdown;
    [SerializeField] private TMP_Dropdown _timeLimitDropdown;

    [Header("Play As (set by your toggles)")]
    [SerializeField] private bool _playAsAttacker = true;

    public void SetPlayAsAttacker()
    {
        _playAsAttacker = true;
    }

    public void SetPlayAsDefender()
    {
        _playAsAttacker = false;
    }

    public void OnStartPressed()
    {
        Difficulty difficulty = ReadDifficulty();
        TimeLimitOption timeLimit = ReadTimeLimit();

        GameSettings s = GameSettings.CreateDefault();
        s.mode = GameMode.SinglePlayer;
        s.timeLimit = timeLimit;

        if (_playAsAttacker)
        {
            s.attacker.type = PlayerType.Human;
            s.attacker.displayName = "Player";

            s.defender.type = PlayerType.AI;
            s.defender.displayName = "CPU";
            s.defender.aiDifficulty = difficulty;
        }
        else
        {
            s.defender.type = PlayerType.Human;
            s.defender.displayName = "Player";

            s.attacker.type = PlayerType.AI;
            s.attacker.displayName = "CPU";
            s.attacker.aiDifficulty = difficulty;
        }

        GameSettingsManager.Instance.Set(s);

        if (_sceneLoader != null)
        {
            _sceneLoader.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }

    private Difficulty ReadDifficulty()
    {
        if (_difficultyDropdown == null)
        {
            return Difficulty.Easy;
        }

        int i = _difficultyDropdown.value;

        if (i == 0) { return Difficulty.Easy; }
        if (i == 1) { return Difficulty.Medium; }
        return Difficulty.Hard;
    }

    private TimeLimitOption ReadTimeLimit()
    {
        if (_timeLimitDropdown == null)
        {
            return TimeLimitOption.Unlimited;
        }

        int i = _timeLimitDropdown.value;

        if (i == 0) { return TimeLimitOption.Unlimited; }
        if (i == 1) { return TimeLimitOption.FiveMinutes; }
        if (i == 2) { return TimeLimitOption.TenMinutes; }
        return TimeLimitOption.FifteenMinutes;
    }
}
