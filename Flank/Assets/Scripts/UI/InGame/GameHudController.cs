using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameHudController : MonoBehaviour
{
    private static readonly Color AttackerColor  = new Color(0.91f, 0.46f, 0.29f);
    private static readonly Color DefenderColor  = new Color(0.29f, 0.74f, 0.91f);

    [Header("Scene References")]
    [SerializeField] private GameController _gameController;

    [Header("Top-left Panel")]
    [SerializeField] private TMP_Text _turnIndicatorText;
    [SerializeField] private TMP_Text _movesText;

    [Header("Counts")]
    [SerializeField] private TMP_Text _flagsText;
    [SerializeField] private TMP_Text _attackersText;

    [Header("Timer")]
    [SerializeField] private TMP_Text _attackerTimerText;
    [SerializeField] private TMP_Text _defenderTimerText;

    [Header("Buttons")]
    [SerializeField] private Button _endTurnButton;

    private int _initialAttackerCount;
    private int _initialFlagCount;
    private bool _isInitialized;
    private bool _attackerTimerVisible;
    private bool _defenderTimerVisible;

    private void OnEnable()
    {
        if (_gameController == null)
        {
            _gameController = FindFirstObjectByType<GameController>();
        }

        if (_gameController != null)
        {
            _gameController.StateChanged += HandleStateChanged;

            if (_gameController.State != null)
            {
                HandleStateChanged(_gameController.State);
            }
        }

        if (_endTurnButton != null)
        {
            _endTurnButton.onClick.AddListener(HandleEndTurnClicked);
        }
    }

    private void OnDisable()
    {
        if (_gameController != null)
        {
            _gameController.StateChanged -= HandleStateChanged;
        }

        if (_endTurnButton != null)
        {
            _endTurnButton.onClick.RemoveListener(HandleEndTurnClicked);
        }
    }

    private void Update()
    {
        if (_gameController == null)
        {
            return;
        }

        bool timerActive = _gameController.HasTimeLimit
            && (_gameController.State == null || _gameController.State.result == GameResult.None);

        SetTimerVisible(_attackerTimerText, ref _attackerTimerVisible, timerActive);
        SetTimerVisible(_defenderTimerText, ref _defenderTimerVisible, timerActive);

        if (!timerActive)
        {
            return;
        }

        Role active = _gameController.State?.currentTurn ?? Role.Attacker;
        float attTime = Mathf.Max(0f, _gameController.AttackerTimeRemaining);
        float defTime = Mathf.Max(0f, _gameController.DefenderTimeRemaining);

        UpdateClock(_attackerTimerText, attTime, active == Role.Attacker, "#E8764A");
        UpdateClock(_defenderTimerText, defTime, active == Role.Defender, "#4ABCE8");
    }

    private static void SetTimerVisible(TMP_Text text, ref bool visible, bool active)
    {
        if (text == null) return;
        if (active != visible)
        {
            visible = active;
            text.gameObject.SetActive(active);
        }
    }

    private static void UpdateClock(TMP_Text text, float rem, bool isActive, string roleColor)
    {
        if (text == null) return;
        string color = isActive
            ? (rem <= 10f ? "#FF4D33" : rem <= 30f ? "#F5C518" : roleColor)
            : "#707070";
        text.color = Color.white;
        text.text = $"<color={color}>{FormatTime(rem)}</color>";
    }

    private static string FormatTime(float rem)
    {
        int mins = Mathf.FloorToInt(rem / 60f);
        int secs = Mathf.FloorToInt(rem % 60f);
        return $"{mins}:{secs:D2}";
    }

    private void HandleEndTurnClicked()
    {
        if (_gameController == null)
        {
            return;
        }

        _gameController.EndTurnEarly();
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (!_isInitialized)
        {
            CacheInitialCounts(state);
            _isInitialized = true;
        }

        UpdateTurnText(state);
        UpdateMovesText(state);
        UpdateCounts(state);
        UpdateButtons(state);
    }

    private void CacheInitialCounts(GameState state)
    {
        _initialFlagCount = state.flags.Count;

        int attackerTotal = 0;
        foreach (PieceModel piece in state.pieces.Values)
        {
            if (piece.role == Role.Attacker)
            {
                attackerTotal++;
            }
        }

        _initialAttackerCount = attackerTotal;
    }

    private void UpdateTurnText(GameState state)
    {
        if (_turnIndicatorText == null)
        {
            return;
        }

        GameSettings settings = GameSettingsManager.Instance?.Current;

        if (state.currentTurn == Role.Attacker)
        {
            string name = settings?.attacker?.displayName;
            if (string.IsNullOrWhiteSpace(name)) name = "Attacker";
            _turnIndicatorText.text = $"{name}'s Turn";
            _turnIndicatorText.color = AttackerColor;
        }
        else
        {
            string name = settings?.defender?.displayName;
            if (string.IsNullOrWhiteSpace(name)) name = "Defender";
            _turnIndicatorText.text = $"{name}'s Turn";
            _turnIndicatorText.color = DefenderColor;
        }
    }

    private void UpdateMovesText(GameState state)
    {
        if (_movesText == null)
        {
            return;
        }

        TurnProgress progress = state.turnProgress;
        int used = progress != null ? progress.movesUsed : 0;
        int allowed = progress != null ? progress.movesAllowed : 0;

        _movesText.text = $"Moves: {used} / {allowed}";
    }

    private void UpdateCounts(GameState state)
    {
        if (_flagsText != null)
        {
            int flagsOnBoard = 0;
            foreach (FlagModel flag in state.flags.Values)
            {
                if (!flag.isCaptured)
                {
                    flagsOnBoard++;
                }
            }

            int totalFlags = _initialFlagCount > 0 ? _initialFlagCount : state.flags.Count;
            _flagsText.text = $"{flagsOnBoard} / {totalFlags}";
        }

        if (_attackersText != null)
        {
            int attackersAlive = 0;
            foreach (PieceModel piece in state.pieces.Values)
            {
                if (piece.role == Role.Attacker && !piece.isCaptured)
                {
                    attackersAlive++;
                }
            }

            int totalAttackers = _initialAttackerCount > 0 ? _initialAttackerCount : attackersAlive;
            _attackersText.text = $"{attackersAlive} / {totalAttackers}";
        }
    }

    private void UpdateButtons(GameState state)
    {
        if (_endTurnButton == null)
        {
            return;
        }

        // Only defenders can end turn early, and only after at least one move.
        if (state.currentTurn != Role.Defender)
        {
            _endTurnButton.interactable = false;
            return;
        }

        TurnProgress progress = state.turnProgress;
        int used = progress != null ? progress.movesUsed : 0;

        _endTurnButton.interactable = used >= 1;
    }
}
