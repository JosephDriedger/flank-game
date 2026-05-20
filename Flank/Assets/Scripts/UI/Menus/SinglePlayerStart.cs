using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SinglePlayerStart : MonoBehaviour
{
    [SerializeField] private SceneLoader _sceneLoader;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown _difficultyDropdown;
    [SerializeField] private TMP_Dropdown _timeLimitDropdown;
    [SerializeField] private Toggle _attackerToggle;
    [SerializeField] private Toggle _defenderToggle;

    private bool _playAsAttacker = true;

    private void Awake()
    {
        _attackerToggle?.onValueChanged.AddListener(on =>
        {
            if (!on) return;
            _playAsAttacker = true;
            _defenderToggle?.SetIsOnWithoutNotify(false);
        });

        _defenderToggle?.onValueChanged.AddListener(on =>
        {
            if (!on) return;
            _playAsAttacker = false;
            _attackerToggle?.SetIsOnWithoutNotify(false);
        });
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

        if (GameSettingsManager.Instance == null)
        {
            Debug.LogError("[SinglePlayerStart] GameSettingsManager.Instance is null. " +
                           "Make sure the Persistence scene is loaded.");
            return;
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

        switch (_timeLimitDropdown.value)
        {
            case 1: return TimeLimitOption.ThreeMinutes;
            case 2: return TimeLimitOption.FiveMinutes;
            case 3: return TimeLimitOption.TenMinutes;
            case 4: return TimeLimitOption.TwentyMinutes;
            default: return TimeLimitOption.Unlimited;
        }
    }
}
