using TMPro;
using UnityEngine;

public sealed class PassNPlayStart : MonoBehaviour
{
    [SerializeField] private SceneLoader _sceneLoader;

    [Header("UI")]
    [SerializeField] private TMP_InputField _attackerName;
    [SerializeField] private TMP_InputField _defenderName;
    [SerializeField] private TMP_Dropdown _timeLimitDropdown;

    public void OnStartPressed()
    {
        GameSettings s = GameSettings.CreateDefault();
        s.mode = GameMode.PassNPlay;
        s.timeLimit = ReadTimeLimit();

        s.attacker.type = PlayerType.Human;
        s.attacker.displayName = ReadName(_attackerName, "Attacker");

        s.defender.type = PlayerType.Human;
        s.defender.displayName = ReadName(_defenderName, "Defender");

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

    private static string ReadName(TMP_InputField field, string fallback)
    {
        if (field == null)
        {
            return fallback;
        }

        string t = field.text;
        if (string.IsNullOrWhiteSpace(t))
        {
            return fallback;
        }

        return t.Trim();
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
