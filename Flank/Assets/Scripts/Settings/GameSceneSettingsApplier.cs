using UnityEngine;

public sealed class GameSceneSettingsApplier : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameController _gameController;

    [Header("Attacker Controllers")]
    [SerializeField] private MonoBehaviour _attackerHumanController;
    [SerializeField] private AIPlayerController _attackerAiController;

    [Header("Defender Controllers")]
    [SerializeField] private MonoBehaviour _defenderHumanController;
    [SerializeField] private AIPlayerController _defenderAiController;

    private void Awake()
    {
        if (_gameController == null)
        {
            return;
        }

        GameSettings settings = GameSettingsManager.Instance.Current;
        if (settings == null)
        {
            settings = GameSettings.CreateDefault();
        }

        MonoBehaviour attackerController = PickController(
            settings.attacker,
            _attackerHumanController,
            _attackerAiController);

        MonoBehaviour defenderController = PickController(
            settings.defender,
            _defenderHumanController,
            _defenderAiController);

        _gameController.ConfigurePlayerControllers(attackerController, defenderController);

        ApplyAiDifficultyIfNeeded(settings.attacker, _attackerAiController);
        ApplyAiDifficultyIfNeeded(settings.defender, _defenderAiController);
    }

    private static MonoBehaviour PickController(PlayerConfig cfg, MonoBehaviour human, AIPlayerController ai)
    {
        if (cfg != null && cfg.type == PlayerType.AI)
        {
            return ai;
        }

        return human;
    }

    private static void ApplyAiDifficultyIfNeeded(PlayerConfig cfg, AIPlayerController ai)
    {
        if (cfg == null || ai == null)
        {
            return;
        }

        if (cfg.type != PlayerType.AI)
        {
            return;
        }

        ai.ApplyDifficulty(cfg.aiDifficulty);
    }
}
