using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private BoardMapConfig _boardMapConfig;

    [Header("Scene")]
    [SerializeField] private BoardView _boardView;
    [SerializeField] private TurnPerspectiveController _turnPerspective;

    [Header("Players")]
    [SerializeField] private MonoBehaviour _attackerControllerBehaviour;
    [SerializeField] private MonoBehaviour _defenderControllerBehaviour;

    // Strong references
    private IPlayerController _attackerController;
    private IPlayerController _defenderController;

    // Current turn owner (both forms)
    public MonoBehaviour CurrentControllerBehaviour { get; private set; }
    public IPlayerController CurrentController { get; private set; }

    private BoardModel _board;
    private GameState _state;

    private TurnSystem _turnSystem;
    private RulesEngine _rules;

    private void Awake()
    {
        _attackerController = _attackerControllerBehaviour as IPlayerController;
        _defenderController = _defenderControllerBehaviour as IPlayerController;

        if (_attackerController == null)
        {
            Debug.LogError("Attacker controller does not implement IPlayerController.");
        }

        if (_defenderController == null)
        {
            Debug.LogError("Defender controller does not implement IPlayerController.");
        }

        _turnSystem = new TurnSystem(new TurnRules(), new MoveBudget());
        _rules = new RulesEngine();
    }

    private void Start()
    {
        NewGame();
    }

    public void NewGame()
    {
        try
        {
            _board = new BoardModel();
            _state = new GameState();
            _state.result = GameResult.None;

            List<(string id, HexCoord coord)> attackers;
            List<(string id, HexCoord coord)> defenders;
            List<(string id, HexCoord coord)> flags;

            BoardMapBuilder.Build(
                _boardMapConfig,
                _board,
                out attackers,
                out defenders,
                out flags
            );

            // Spawn attackers
            for (int i = 0; i < attackers.Count; i++)
            {
                PieceModel p = new PieceModel(
                    attackers[i].id,
                    Role.Attacker,
                    attackers[i].coord
                );

                _state.AddPiece(p);
                _board.GetHex(p.position).occupantPieceId = p.id;
            }

            // Spawn defenders
            for (int i = 0; i < defenders.Count; i++)
            {
                PieceModel p = new PieceModel(
                    defenders[i].id,
                    Role.Defender,
                    defenders[i].coord
                );

                _state.AddPiece(p);
                _board.GetHex(p.position).occupantPieceId = p.id;
            }

            // Spawn flags
            for (int i = 0; i < flags.Count; i++)
            {
                FlagModel f = new FlagModel(
                    flags[i].id,
                    flags[i].coord
                );

                _state.AddFlag(f);
                _board.GetHex(flags[i].coord).flagId = f.id;
            }

            _boardView.Build(_board);
            _boardView.SyncPieces(_state);

            BeginTurn(Role.Attacker);
        }
        catch (Exception ex)
        {
            Debug.LogError($"NewGame failed: {ex.Message}");
            throw;
        }
    }

    private void BeginTurn(Role role)
    {
        // End previous controller turn
        CurrentController?.EndTurn();

        _turnSystem.BeginTurn(_state, role);

        if (role == Role.Attacker)
        {
            CurrentControllerBehaviour = _attackerControllerBehaviour;
            CurrentController = _attackerController;
        }
        else
        {
            CurrentControllerBehaviour = _defenderControllerBehaviour;
            CurrentController = _defenderController;
        }

        bool isHumanTurn = CurrentControllerBehaviour is HumanPlayerController;

        _turnPerspective.Apply(role, isHumanTurn);

        CurrentController?.BeginTurn(_state, _board);
    }

    public void TryApplyAction(PlayerAction action)
    {
        if (action == null)
        {
            return;
        }

        if (!_turnSystem.CanAct(_state))
        {
            return;
        }

        // HARD RULE: piece must be eligible this turn (attackers: second move must be different piece).
        if (!_turnSystem.CanUsePiece(_state, action.pieceId))
        {
            return;
        }

        PieceModel movingPiece = _state.GetPiece(action.pieceId);
        HexCoord fromBeforeMove = movingPiece != null ? movingPiece.position : new HexCoord(0, 0);

        bool applied = _rules.TryApplyAction(action, _state, _board);
        if (!applied)
        {
            return;
        }

        _turnSystem.SpendAction(_state, action.pieceId, fromBeforeMove);

        _boardView.ClearHighlights();
        _boardView.SyncPieces(_state);

        _state.result = _rules.GetGameResult(_state);

        if (_state.result != GameResult.None)
        {
            Debug.Log($"Game Over: {_state.result}");
            CurrentController?.EndTurn();
            return;
        }

        if (_turnSystem.CanAct(_state))
        {
            return;
        }

        BeginTurn(_turnSystem.NextRole(_state.currentTurn));
    }



    public bool CanCurrentTurnContinue()
    {
        return _turnSystem.CanAct(_state);
    }

    public bool CanUsePieceThisTurn(string pieceId)
    {
        return _turnSystem.CanUsePiece(_state, pieceId);
    }

    public void EndTurnEarly()
    {
        // Only defenders can choose to stop after 1 move.
        if (_state == null)
        {
            return;
        }

        if (_state.currentTurn != Role.Defender)
        {
            return;
        }

        // Only allow ending early after at least one move has been used.
        if (_state.turnProgress != null && _state.turnProgress.movesUsed < 1)
        {
            return;
        }

        BeginTurn(_turnSystem.NextRole(_state.currentTurn));
    }
}