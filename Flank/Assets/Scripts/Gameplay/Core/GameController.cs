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
    private MonoBehaviour _attackerControllerBehaviour;
    private MonoBehaviour _defenderControllerBehaviour;

    private IPlayerController _attackerController;
    private IPlayerController _defenderController;

    public MonoBehaviour CurrentControllerBehaviour { get; private set; }
    public IPlayerController CurrentController { get; private set; }

    private BoardModel _board;
    private GameState _state;

    private TurnSystem _turnSystem;
    private RulesEngine _rules;

    // ============================================================
    // UI / OBSERVERS
    // ============================================================

    public event Action<GameState> StateChanged;
    public event Action<string> LogAdded;

    public GameState State
    {
        get
        {
            return _state;
        }
    }

    private void RaiseStateChanged()
    {
        if (_state == null)
        {
            return;
        }

        StateChanged?.Invoke(_state);
    }

    private void AddLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        LogAdded?.Invoke(message);
    }

    private void Start()
    {
        _turnSystem = new TurnSystem(new TurnRules(), new MoveBudget());
        _rules = new RulesEngine();

        if (TryLoadViewBoardSnapshot())
        {
            return;
        }

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

            AddLog("New game started.");

            RaiseStateChanged();

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

        if (_turnPerspective != null)
        {
            _turnPerspective.Apply(role, isHumanTurn);
        }

        CurrentController?.BeginTurn(_state, _board);

        string turnName = role == Role.Attacker ? "Attacker" : "Defender";
        AddLog($"{turnName} turn started.");

        RaiseStateChanged();
    }

    public bool TryApplyAction(PlayerAction action)
    {
        if (action == null)
        {
            return false;
        }

        if (_state == null || _board == null)
        {
            return false;
        }

        if (!_turnSystem.CanAct(_state))
        {
            return false;
        }

        if (!_turnSystem.CanUsePiece(_state, action.pieceId))
        {
            return false;
        }

        PieceModel movingPiece = _state.GetPiece(action.pieceId);
        HexCoord fromBeforeMove = movingPiece != null ? movingPiece.position : new HexCoord(0, 0);

        bool applied = _rules.TryApplyAction(action, _state, _board);
        if (!applied)
        {
            return false;
        }

        _turnSystem.SpendAction(_state, action.pieceId, fromBeforeMove);

        if (_boardView != null)
        {
            _boardView.ClearHighlights();
            _boardView.SyncPieces(_state);
        }

        _state.result = _rules.GetGameResult(_state);

        string roleName = _state.currentTurn == Role.Attacker ? "Attacker" : "Defender";
        AddLog($"{roleName} moved {action.pieceId}.");

        RaiseStateChanged();

        if (_state.result != GameResult.None)
        {
            AddLog($"Game Over: {_state.result}");
            Debug.Log($"Game Over: {_state.result}");
            CurrentController?.EndTurn();
            return true;
        }

        if (_turnSystem.CanAct(_state))
        {
            return true;
        }

        BeginTurn(_turnSystem.NextRole(_state.currentTurn));
        return true;
    }

    public bool CanCurrentTurnContinue()
    {
        if (_state == null)
        {
            return false;
        }

        return _turnSystem.CanAct(_state);
    }

    public bool CanUsePieceThisTurn(string pieceId)
    {
        if (_state == null)
        {
            return false;
        }

        return _turnSystem.CanUsePiece(_state, pieceId);
    }

    public void EndTurnEarly()
    {
        if (_state == null)
        {
            return;
        }

        if (_state.currentTurn != Role.Defender)
        {
            return;
        }

        if (_state.turnProgress != null && _state.turnProgress.movesUsed < 1)
        {
            return;
        }

        AddLog("Defender ended turn early.");

        BeginTurn(_turnSystem.NextRole(_state.currentTurn));
    }

    public void ConfigurePlayerControllers(MonoBehaviour attacker, MonoBehaviour defender)
    {
        _attackerControllerBehaviour = attacker;
        _defenderControllerBehaviour = defender;

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
    }

    // ============================================================
    // VIEW BOARD (PostGame -> GameScene read-only)
    // ============================================================

    private bool TryLoadViewBoardSnapshot()
    {
        int mode = LoadInt(PostGameKeys.LaunchMode, (int)GameLaunchMode.Normal);
        if (mode != (int)GameLaunchMode.ViewBoard)
        {
            return false;
        }

        string json = LoadString(PostGameKeys.LastBoardSnapshotJson, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        GameStateSnapshot snap = JsonUtility.FromJson<GameStateSnapshot>(json);
        if (snap == null)
        {
            return false;
        }

        LoadFromSnapshot(snap);
        return true;
    }

    private void LoadFromSnapshot(GameStateSnapshot snap)
    {
        _board = new BoardModel();
        _state = new GameState();

        _state.result = ParseResult(snap.result);
        _state.currentTurn = (Role)snap.currentTurn;

        List<(string id, HexCoord coord)> attackers;
        List<(string id, HexCoord coord)> defenders;
        List<(string id, HexCoord coord)> flags;

        BoardMapBuilder.Build(_boardMapConfig, _board, out attackers, out defenders, out flags);

        // Clear only known spawn hexes (BoardModel does not expose a hex dictionary).
        for (int i = 0; i < attackers.Count; i++)
        {
            _board.GetHex(attackers[i].coord).occupantPieceId = null;
        }

        for (int i = 0; i < defenders.Count; i++)
        {
            _board.GetHex(defenders[i].coord).occupantPieceId = null;
        }

        for (int i = 0; i < flags.Count; i++)
        {
            _board.GetHex(flags[i].coord).flagId = null;
        }

        // Restore pieces
        for (int i = 0; i < snap.pieces.Count; i++)
        {
            GameStateSnapshot.PieceSnapshot ps = snap.pieces[i];

            PieceModel p = new PieceModel(
                ps.id,
                (Role)ps.role,
                new HexCoord(ps.q, ps.r)
            );

            p.isCaptured = ps.isCaptured;
            p.carryingFlagId = string.IsNullOrWhiteSpace(ps.carryingFlagId) ? null : ps.carryingFlagId;

            _state.AddPiece(p);

            if (!p.isCaptured)
            {
                _board.GetHex(p.position).occupantPieceId = p.id;
            }
        }

        // Restore flags
        for (int i = 0; i < snap.flags.Count; i++)
        {
            GameStateSnapshot.FlagSnapshot fs = snap.flags[i];

            HexCoord home = fs.hasLocation ? new HexCoord(fs.q, fs.r) : new HexCoord(0, 0);
            FlagModel f = new FlagModel(fs.id, home);

            f.isCaptured = fs.isCaptured;
            f.carrierPieceId = string.IsNullOrWhiteSpace(fs.carrierPieceId) ? null : fs.carrierPieceId;

            if (fs.hasLocation)
            {
                f.location = new HexCoord(fs.q, fs.r);
            }
            else
            {
                f.location = null;
            }

            _state.AddFlag(f);

            if (!f.isCaptured && f.location.HasValue && string.IsNullOrWhiteSpace(f.carrierPieceId))
            {
                _board.GetHex(f.location.Value).flagId = f.id;
            }
        }

        if (_boardView != null)
        {
            _boardView.Build(_board);
            _boardView.SyncPieces(_state);
            _boardView.ClearHighlights();
        }

        // Disable interaction: we do not start a turn, and we clear controllers.
        CurrentController?.EndTurn();
        CurrentControllerBehaviour = null;
        CurrentController = null;

        AddLog("Viewing final board (read-only).");
        RaiseStateChanged();
    }

    private GameResult ParseResult(string raw)
    {
        if (raw == GameResult.AttackersWin.ToString())
        {
            return GameResult.AttackersWin;
        }

        if (raw == GameResult.DefendersWin.ToString())
        {
            return GameResult.DefendersWin;
        }

        return GameResult.None;
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
            return int.TryParse(raw, out int v) ? v : fallback;
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
