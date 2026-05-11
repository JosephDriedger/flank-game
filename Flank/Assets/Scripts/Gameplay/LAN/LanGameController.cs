using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class LanGameController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private BoardMapConfig _boardMapConfig;

    [Header("Scene")]
    [SerializeField] private BoardView _boardView;
    [SerializeField] private TurnPerspectiveController _turnPerspective;
    [SerializeField] private string _postGameSceneName = "PostGame";
    [SerializeField] private string _navigationSceneName = "NavigationScene";

    private BoardModel _board;
    private GameState _state;

    private TurnSystem _turnSystem;
    private RulesEngine _rules;

    private bool _boardViewBuilt;

    private readonly NetworkVariable<FixedString4096Bytes> _snapshotJson =
        new NetworkVariable<FixedString4096Bytes>(
            new FixedString4096Bytes(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private const string AttackerTag = "<color=#E8764A>";
    private const string DefenderTag = "<color=#4ABCE8>";
    private const string SystemTag   = "<color=#F5C518>";
    private const string AlertTag    = "<color=#FF6666>";
    private const string EndTag      = "</color>";

    private readonly NetworkVariable<int> _networkTimeLimitSeconds =
        new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    // Server-authoritative chess clocks.
    private float _serverAttackerTimeRemaining;
    private float _serverDefenderTimeRemaining;

    // Client-side display clocks (corrected by each snapshot).
    private float _localAttackerTimeRemaining;
    private float _localDefenderTimeRemaining;

    public event Action<GameState> StateChanged;
    public event Action<string> LogAdded;

    public GameState State
    {
        get
        {
            return _state;
        }
    }

    public BoardModel Board
    {
        get
        {
            return _board;
        }
    }

    public bool HasTimeLimit => _networkTimeLimitSeconds.Value > 0;

    public float AttackerTimeRemainingDisplay =>
        IsServer ? _serverAttackerTimeRemaining : _localAttackerTimeRemaining;

    public float DefenderTimeRemainingDisplay =>
        IsServer ? _serverDefenderTimeRemaining : _localDefenderTimeRemaining;

    private void Awake()
    {
        if (_boardView == null)
        {
            _boardView = FindAnyObjectByType<BoardView>(FindObjectsInactive.Include);
        }

        if (_turnPerspective == null)
        {
            _turnPerspective = FindAnyObjectByType<TurnPerspectiveController>(FindObjectsInactive.Include);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _snapshotJson.OnValueChanged -= HandleSnapshotChanged;
        _snapshotJson.OnValueChanged += HandleSnapshotChanged;

        if (IsServer)
        {
            _turnSystem = new TurnSystem(new TurnRules(), new MoveBudget());
            _rules = new RulesEngine();

            NetworkManager.OnClientDisconnectCallback += HandleRemoteClientDisconnected;

            bool isViewBoard = LanNetworkService.Instance != null &&
                               LanNetworkService.Instance.IsViewBoard &&
                               !string.IsNullOrWhiteSpace(LanNetworkService.Instance.LastGameSnapshotJson);

            if (isViewBoard)
            {
                _networkTimeLimitSeconds.Value = 0;
                _snapshotJson.Value = new FixedString4096Bytes(LanNetworkService.Instance.LastGameSnapshotJson);
            }
            else
            {
                int timeLimitSeconds = 0;
                if (GameSettingsManager.Instance?.Current != null)
                {
                    timeLimitSeconds = (int)GameSettingsManager.Instance.Current.timeLimit * 60;
                }

                _networkTimeLimitSeconds.Value = timeLimitSeconds;
                NewGameServer();
            }
        }

        if (_snapshotJson.Value.Length > 0)
        {
            ApplySnapshotLocal(_snapshotJson.Value.ToString());
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _snapshotJson.OnValueChanged -= HandleSnapshotChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleRemoteClientDisconnected;
        }
    }

    private void HandleRemoteClientDisconnected(ulong clientId)
    {
        if (!IsServer || NetworkManager.Singleton == null)
        {
            return;
        }

        // Only react to remote clients, not the host itself.
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        // Don't interfere during a post-game scene transition.
        if (LanNetworkService.Instance != null && LanNetworkService.Instance.IsPostGameTransition)
        {
            return;
        }

        // A player left mid-game. Save the correct return panel for the host then shut down.
        bool isOnline = GameSettingsManager.Instance?.Current?.mode == GameMode.OnlineMatchmaking;
        string panelName = isOnline ? "OnlineHostPanel" : "LanHostPanel";

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString("PanelManager.LastPanelName", panelName);
        }
        else
        {
            PlayerPrefs.SetString("PanelManager.LastPanelName", panelName);
            PlayerPrefs.Save();
        }

        // Flag before Shutdown so LanNetworkService's disconnect handler doesn't double-navigate.
        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.IsPostGameTransition = true;
        }

        LanNetworkService.Instance?.Shutdown();
        SceneRouter.Instance?.GoToNavigation(openLastPanel: true);
    }

    public bool IsLocalPlayersTurn()
    {
        if (_state == null || NetworkManager.Singleton == null)
        {
            return false;
        }

        if (!TryGetLocalRole(out Role localRole))
        {
            // If the lobby state hasn't replicated yet, fall back to a deterministic 2-player mapping
            // so a player is not soft-locked from moving.
            if (!TryInferRoleForClientId(NetworkManager.Singleton.LocalClientId, out localRole))
            {
                return false;
            }
        }

        return _state.currentTurn == localRole;
    }

    public void RequestEndTurnEarly()
    {
        if (!IsLocalPlayersTurn())
        {
            return;
        }

        EndTurnEarlyServerRpc();
    }

    public void RequestMove(string pieceId, HexCoord destination)
    {
        if (!IsLocalPlayersTurn())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(pieceId))
        {
            return;
        }

        RequestMoveServerRpc(pieceId, destination.q, destination.r);
    }

    private void Update()
    {
        if (IsServer)
        {
            if (!HasTimeLimit || _state == null || _state.result != GameResult.None)
            {
                return;
            }

            if (_state.currentTurn == Role.Attacker)
            {
                _serverAttackerTimeRemaining -= Time.deltaTime;
                if (_serverAttackerTimeRemaining <= 0f)
                {
                    _serverAttackerTimeRemaining = 0f;
                    AddLog($"{AlertTag}Time expired.{EndTag}");
                    BeginTurnServer(_turnSystem.NextRole(_state.currentTurn));
                }
            }
            else
            {
                _serverDefenderTimeRemaining -= Time.deltaTime;
                if (_serverDefenderTimeRemaining <= 0f)
                {
                    _serverDefenderTimeRemaining = 0f;
                    AddLog($"{AlertTag}Time expired.{EndTag}");
                    BeginTurnServer(_turnSystem.NextRole(_state.currentTurn));
                }
            }
        }
        else
        {
            if (!HasTimeLimit || _state == null || _state.result != GameResult.None)
            {
                return;
            }

            if (_state.currentTurn == Role.Attacker)
            {
                _localAttackerTimeRemaining -= Time.deltaTime;
                if (_localAttackerTimeRemaining < 0f) _localAttackerTimeRemaining = 0f;
            }
            else
            {
                _localDefenderTimeRemaining -= Time.deltaTime;
                if (_localDefenderTimeRemaining < 0f) _localDefenderTimeRemaining = 0f;
            }
        }
    }

    // Fires on the server immediately and replicates the message to all clients.
    private void AddLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        LogAdded?.Invoke(message);
        BroadcastLogClientRpc(new FixedString128Bytes(message));
    }

    [Rpc(SendTo.NotServer)]
    private void BroadcastLogClientRpc(FixedString128Bytes message)
    {
        LogAdded?.Invoke(message.ToString());
    }

    private void NewGameServer()
    {
        _board = new BoardModel();
        _state = new GameState();
        _state.result = GameResult.None;

        List<(string id, HexCoord coord)> attackers;
        List<(string id, HexCoord coord)> defenders;
        List<(string id, HexCoord coord)> flags;

        BoardMapBuilder.Build(_boardMapConfig, _board, out attackers, out defenders, out flags);

        for (int i = 0; i < attackers.Count; i++)
        {
            PieceModel p = new PieceModel(attackers[i].id, Role.Attacker, attackers[i].coord);
            _state.AddPiece(p);
            _board.GetHex(p.position).occupantPieceId = p.id;
        }

        for (int i = 0; i < defenders.Count; i++)
        {
            PieceModel p = new PieceModel(defenders[i].id, Role.Defender, defenders[i].coord);
            _state.AddPiece(p);
            _board.GetHex(p.position).occupantPieceId = p.id;
        }

        for (int i = 0; i < flags.Count; i++)
        {
            FlagModel f = new FlagModel(flags[i].id, flags[i].coord);
            _state.AddFlag(f);
            _board.GetHex(flags[i].coord).flagId = f.id;
        }

        _turnSystem.BeginTurn(_state, Role.Attacker);
        _serverAttackerTimeRemaining = _networkTimeLimitSeconds.Value;
        _serverDefenderTimeRemaining = _networkTimeLimitSeconds.Value;

        AddLog($"{SystemTag}New game started.{EndTag}");
        AddLog($"{AttackerTag}Attacker turn started.{EndTag}");
        PublishSnapshotServer();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveServerRpc(FixedString32Bytes pieceId, int q, int r, RpcParams rpcParams = default)
    {
        if (_state == null || _board == null)
        {
            return;
        }

        if (_state.result != GameResult.None)
        {
            return;
        }

        ulong sender = rpcParams.Receive.SenderClientId;

        if (!TryGetRoleForClientId(sender, out Role requesterRole))
        {
            // Sender role not known yet. Reject safely.
            return;
        }

        if (_state.currentTurn != requesterRole)
        {
            return;
        }

        PlayerAction action = new PlayerAction(pieceId.ToString(), new HexCoord(q, r));
        TryApplyActionServer(action);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void EndTurnEarlyServerRpc(RpcParams rpcParams = default)
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

        ulong sender = rpcParams.Receive.SenderClientId;

        if (!TryGetRoleForClientId(sender, out Role requesterRole))
        {
            return;
        }

        if (requesterRole != Role.Defender)
        {
            return;
        }

        AddLog($"{DefenderTag}Defender ended turn early.{EndTag}");
        BeginTurnServer(_turnSystem.NextRole(_state.currentTurn));
    }

    private void TryApplyActionServer(PlayerAction action)
    {
        if (action == null)
        {
            return;
        }

        if (!_turnSystem.CanAct(_state))
        {
            return;
        }

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
        _state.result = _rules.GetGameResult(_state);

        string roleTag  = _state.currentTurn == Role.Attacker ? AttackerTag : DefenderTag;
        string roleName = _state.currentTurn == Role.Attacker ? "Attacker" : "Defender";
        AddLog($"{roleTag}{roleName} moved {action.pieceId}.{EndTag}");

        if (_state.result != GameResult.None)
        {
            AddLog($"{SystemTag}<b>Game over: {_state.result}.</b>{EndTag}");
        }

        PublishSnapshotServer();

        if (_state.result != GameResult.None)
        {
            return;
        }

        if (_turnSystem.CanAct(_state))
        {
            return;
        }

        BeginTurnServer(_turnSystem.NextRole(_state.currentTurn));
    }

    private void BeginTurnServer(Role role)
    {
        // Re-evaluate result on turn start (win/lose can occur when a turn advances).
        _turnSystem.BeginTurn(_state, role);
        _state.result = _rules.GetGameResult(_state);

        // If the next player has no actions, auto-advance (prevents soft-lock turns).
        int safety = 0;
        while (_state.result == GameResult.None && !_turnSystem.CanAct(_state) && safety < 4)
        {
            Role next = _turnSystem.NextRole(_state.currentTurn);
            _turnSystem.BeginTurn(_state, next);
            _state.result = _rules.GetGameResult(_state);
            safety += 1;
        }

        if (_state.result == GameResult.None)
        {
            string turnTag  = _state.currentTurn == Role.Attacker ? AttackerTag : DefenderTag;
            string turnName = _state.currentTurn == Role.Attacker ? "Attacker" : "Defender";
            AddLog($"{turnTag}{turnName} turn started.{EndTag}");
        }

        PublishSnapshotServer();
    }

    private void PublishSnapshotServer()
    {
        GameStateSnapshot snap = BuildSnapshot(_state);
        snap.attackerTimeRemaining = _serverAttackerTimeRemaining;
        snap.defenderTimeRemaining = _serverDefenderTimeRemaining;
        string json = JsonUtility.ToJson(snap);

        if (json.Length > 4000)
        {
            Debug.LogWarning($"[LAN] Snapshot JSON is {json.Length} chars — approaching the 4096-byte NGO limit.");
        }

        _snapshotJson.Value = new FixedString4096Bytes(json);

        // Persist the final state so View Board can restore it after the PostGame scene.
        if (_state.result != GameResult.None && LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.LastGameSnapshotJson = json;
        }
    }

    private void HandleSnapshotChanged(FixedString4096Bytes previous, FixedString4096Bytes next)
    {
        if (next.Length <= 0)
        {
            return;
        }

        ApplySnapshotLocal(next.ToString());
    }

    private void ApplySnapshotLocal(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        GameStateSnapshot snap = JsonUtility.FromJson<GameStateSnapshot>(json);
        if (snap == null)
        {
            return;
        }

        LoadFromSnapshotLocal(snap);
        ApplyPerspectiveLocal();

        // Clients sync chess clock values from the server snapshot.
        if (!IsServer && HasTimeLimit)
        {
            _localAttackerTimeRemaining = snap.attackerTimeRemaining;
            _localDefenderTimeRemaining = snap.defenderTimeRemaining;
        }

        StateChanged?.Invoke(_state);
    }

    private void LoadFromSnapshotLocal(GameStateSnapshot snap)
    {
        _board = new BoardModel();
        _state = new GameState();

        _state.result = ParseResult(snap.result);
        _state.currentTurn = (Role)snap.currentTurn;

        // Replicate turn progress so clients can:
        // - display correct "Moves: used / allowed"
        // - enforce the same per-turn piece gating rules locally
        if (_state.turnProgress != null)
        {
            _state.turnProgress.movesUsed = snap.movesUsed;
            _state.turnProgress.movesAllowed = snap.movesAllowed;
            _state.turnProgress.firstMovedPieceId = string.IsNullOrWhiteSpace(snap.firstMovedPieceId)
                ? null
                : snap.firstMovedPieceId;

            if (snap.hasFirstMoveFrom)
            {
                _state.turnProgress.firstMoveFrom = new HexCoord(snap.firstMoveFromQ, snap.firstMoveFromR);
            }
            else
            {
                _state.turnProgress.firstMoveFrom = new HexCoord(0, 0);
            }
        }

        List<(string id, HexCoord coord)> attackers;
        List<(string id, HexCoord coord)> defenders;
        List<(string id, HexCoord coord)> flags;

        BoardMapBuilder.Build(_boardMapConfig, _board, out attackers, out defenders, out flags);

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

        for (int i = 0; i < snap.pieces.Count; i++)
        {
            GameStateSnapshot.PieceSnapshot ps = snap.pieces[i];
            PieceModel p = new PieceModel(ps.id, (Role)ps.role, new HexCoord(ps.q, ps.r));

            p.isCaptured = ps.isCaptured;
            p.carryingFlagId = string.IsNullOrWhiteSpace(ps.carryingFlagId) ? null : ps.carryingFlagId;

            _state.AddPiece(p);

            if (!p.isCaptured)
            {
                _board.GetHex(p.position).occupantPieceId = p.id;
            }
        }

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
            if (!_boardViewBuilt)
            {
                _boardView.Build(_board);
                _boardViewBuilt = true;
            }
            else
            {
                _boardView.SetBoard(_board);
            }

            _boardView.SyncPieces(_state);
            _boardView.ClearHighlights();
        }
    }

    private void ApplyPerspectiveLocal()
    {
        if (_turnPerspective == null || NetworkManager.Singleton == null || _state == null)
        {
            return;
        }

        if (!TryGetLocalRole(out Role localRole))
        {
            // Role not known yet; do not apply a potentially wrong perspective.
            return;
        }

        // In LAN, always show local player's perspective (prevents wrong view on join).
        _turnPerspective.Apply(localRole, true);
    }

    private static GameStateSnapshot BuildSnapshot(GameState state)
    {
        GameStateSnapshot snap = new GameStateSnapshot();

        snap.result = state.result.ToString();
        snap.currentTurn = (int)state.currentTurn;
        snap.turns = 0;

        if (state.turnProgress != null)
        {
            snap.movesUsed = state.turnProgress.movesUsed;
            snap.movesAllowed = state.turnProgress.movesAllowed;
            snap.firstMovedPieceId = state.turnProgress.firstMovedPieceId;

            // firstMoveFrom is only meaningful for defender backtrack logic, but harmless to replicate.
            snap.hasFirstMoveFrom = true;
            snap.firstMoveFromQ = state.turnProgress.firstMoveFrom.q;
            snap.firstMoveFromR = state.turnProgress.firstMoveFrom.r;
        }
        else
        {
            snap.movesUsed = 0;
            snap.movesAllowed = 0;
            snap.firstMovedPieceId = null;
            snap.hasFirstMoveFrom = false;
            snap.firstMoveFromQ = 0;
            snap.firstMoveFromR = 0;
        }

        foreach (PieceModel p in state.pieces.Values)
        {
            GameStateSnapshot.PieceSnapshot ps = new GameStateSnapshot.PieceSnapshot();
            ps.id = p.id;
            ps.role = (int)p.role;
            ps.q = p.position.q;
            ps.r = p.position.r;
            ps.isCaptured = p.isCaptured;
            ps.carryingFlagId = p.carryingFlagId;
            snap.pieces.Add(ps);
        }

        foreach (FlagModel f in state.flags.Values)
        {
            GameStateSnapshot.FlagSnapshot fs = new GameStateSnapshot.FlagSnapshot();
            fs.id = f.id;
            fs.isCaptured = f.isCaptured;
            fs.carrierPieceId = f.carrierPieceId;

            if (f.location.HasValue)
            {
                fs.hasLocation = true;
                fs.q = f.location.Value.q;
                fs.r = f.location.Value.r;
            }
            else
            {
                fs.hasLocation = false;
                fs.q = 0;
                fs.r = 0;
            }

            snap.flags.Add(fs);
        }

        return snap;
    }

    private static GameResult ParseResult(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return GameResult.None;
        }

        if (Enum.TryParse(s, out GameResult r))
        {
            return r;
        }

        return GameResult.None;
    }

    private bool TryGetLocalRole(out Role role)
    {
        role = Role.Attacker;

        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        if (LanLobbyState.Instance != null &&
            LanLobbyState.Instance.TryGetRoleForClientId(NetworkManager.Singleton.LocalClientId, out role))
        {
            return true;
        }

        // Fallback: deterministic 2-player mapping (host=attacker, client=defender).
        // Used if the lobby state has not replicated yet.
        return TryInferRoleForClientId(NetworkManager.Singleton.LocalClientId, out role);
    }

    private bool TryGetRoleForClientId(ulong clientId, out Role role)
    {
        role = Role.Attacker;

        if (LanLobbyState.Instance != null && LanLobbyState.Instance.TryGetRoleForClientId(clientId, out role))
        {
            return true;
        }

        // Fallback: deterministic 2-player mapping.
        return TryInferRoleForClientId(clientId, out role);
    }

    private bool TryInferRoleForClientId(ulong clientId, out Role role)
    {
        role = Role.Attacker;

        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        // Host is attacker by default. First (and only) client is defender.
        // This is only used if the lobby state isn't available yet.
        if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
        {
            role = Role.Attacker;
            return true;
        }

        // If we're on server and asking about someone else, that someone else is the client.
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            role = Role.Defender;
            return true;
        }

        // If we're a connected client, we are the defender by default.
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            role = Role.Defender;
            return true;
        }

        return false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void QuitToLobbyServerRpc()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.IsViewBoard = false;
            LanNetworkService.Instance.IsPostGameTransition = true;
        }

        string panelName = "LanLobbyPanel";
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString("PanelManager.LastPanelName", panelName);
        }
        else
        {
            PlayerPrefs.SetString("PanelManager.LastPanelName", panelName);
            PlayerPrefs.Save();
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            _navigationSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void BackToPostGameServerRpc()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (LanNetworkService.Instance != null)
        {
            LanNetworkService.Instance.IsViewBoard = false;
            LanNetworkService.Instance.IsPostGameTransition = true;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            _postGameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
