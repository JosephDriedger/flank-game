using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameHudController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameController _gameController;

    [Header("Top-left Panel")]
    [SerializeField] private TMP_Text _turnIndicatorText;
    [SerializeField] private TMP_Text _movesText;

    [Header("Counts")]
    [SerializeField] private TMP_Text _flagsText;
    [SerializeField] private TMP_Text _attackersText;

    [Header("Buttons")]
    [SerializeField] private Button _endTurnButton;

    private int _initialAttackerCount;
    private int _initialFlagCount;
    private bool _isInitialized;

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

        if (state.currentTurn == Role.Attacker)
        {
            _turnIndicatorText.text = "Attacker Turn";
        }
        else
        {
            _turnIndicatorText.text = "Defender Turn";
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
