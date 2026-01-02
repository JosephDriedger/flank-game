using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class LanGameController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private BoardMapConfig boardMapConfig;

    [Header("Scene")]
    [SerializeField] private BoardView boardView;
    [SerializeField] private TurnPerspectiveController turnPerspective;

    private readonly NetworkVariable<FixedString4096Bytes> stateJson =
        new NetworkVariable<FixedString4096Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public event Action<GameState> StateChanged;

    private BoardModel board;
    private GameState state;

    private TurnSystem turnSystem;
    private RulesEngine rules;

    public GameState State => state;
    public BoardModel Board => board;

    private void Awake()
    {
        turnSystem = new TurnSystem(new TurnRules(), new MoveBudget());
        rules = new RulesEngine();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        EnsureSceneReferences();
        BuildBoardLocal();

        stateJson.OnValueChanged += HandleStateJsonChanged;

        if (IsServer)
        {
            NewGameServer();
        }

        // IMPORTANT: everyone (including host) renders from the same replicated pipeline.
        // Host will apply its own value immediately after PublishSnapshotServer.
        if (!IsServer)
        {
            // Client waits for OnValueChanged.
        }
    }

    public override void OnNetworkDespawn()
    {
        stateJson.OnValueChanged -= HandleStateJsonChanged;
        base.OnNetworkDespawn();
    }

    // ============================================================
    // SIDE / TURN RESOLUTION
    // ============================================================

    public bool IsLanSessionActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    public LobbySide GetLocalSide()
    {
        if (NetworkManager.Singleton == null || LanLobbyState.Instance == null)
        {
            return LobbySide.None;
        }

        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (LanLobbyState.Instance.TryGetSide(localId, out LobbySide side))
        {
            return side;
        }

        // Fallback path
        for (int i = 0; i < LanLobbyState.Instance.Players.Count; i++)
        {
            LobbyPlayerData p = LanLobbyState.Instance.Players[i];
            if (p.ClientId == localId)
            {
                return p.Side;
            }
        }

        return LobbySide.None;
    }

    public bool IsLocalPlayersTurn()
    {
        if (state == null)
        {
            return false;
        }

        LobbySide side = GetLocalSide();

        if (state.currentTurn == Role.Attacker)
        {
            return side == LobbySide.Attacker;
        }

        if (state.currentTurn == Role.Defender)
        {
            return side == LobbySide.Defender;
        }

        return false;
    }

    public bool CanUsePieceThisTurn(string pieceId)
    {
        if (state == null)
        {
            return false;
        }

        if (!IsLocalPlayersTurn())
        {
            return false;
        }

        return turnSystem.CanUsePiece(state, pieceId);
    }

    // ============================================================
    // CLIENT REQUESTS
    // ============================================================

    public void RequestMove(string pieceId, HexCoord destination)
    {
        if (!IsLanSessionActive() || !IsLocalPlayersTurn())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(pieceId))
        {
            return;
        }

        RequestMoveServerRpc(pieceId, destination.q, destination.r);
    }

    public void RequestEndTurnEarly()
    {
        if (!IsLanSessionActive() || !IsLocalPlayersTurn())
        {
            return;
        }

        RequestEndTurnEarlyServerRpc();
    }

    // ============================================================
    // SERVER: GAME SETUP + PUBLISH
    // ============================================================

    private void NewGameServer()
    {
        board = new BoardModel();
        state = new GameState();
        state.result = GameResult.None;

        List<(string id, HexCoord coord)> attackers;
        List<(string id, HexCoord coord)> defenders;
        List<(string id, HexCoord coord)> flags;

        BoardMapBuilder.Build(boardMapConfig, board, out attackers, out defenders, out flags);

        for (int i = 0; i < attackers.Count; i++)
        {
            PieceModel p = new PieceModel(attackers[i].id, Role.Attacker, attackers[i].coord);
            state.AddPiece(p);
            board.GetHex(p.position).occupantPieceId = p.id;
        }

        for (int i = 0; i < defenders.Count; i++)
        {
            PieceModel p = new PieceModel(defenders[i].id, Role.Defender, defenders[i].coord);
            state.AddPiece(p);
            board.GetHex(p.position).occupantPieceId = p.id;
        }

        for (int i = 0; i < flags.Count; i++)
        {
            FlagModel f = new FlagModel(flags[i].id, flags[i].coord);
            state.AddFlag(f);
            board.GetHex(flags[i].coord).flagId = f.id;
        }

        BeginTurnServer(Role.Attacker);
        PublishSnapshotServer();
    }

    private void BeginTurnServer(Role role)
    {
        turnSystem.BeginTurn(state, role);
    }

    private void PublishSnapshotServer()
    {
        GameStateSnapshot snap = BuildSnapshot(state);
        string json = JsonUtility.ToJson(snap);

        stateJson.Value = new FixedString4096Bytes(json);

        // IMPORTANT: Host applies EXACTLY the same snapshot pipeline as clients.
        ApplySnapshotLocal(snap);
    }

    // ============================================================
    // SERVER RPCs
    // ============================================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveServerRpc(string pieceId, int q, int r, RpcParams rpcParams = default)
    {
        if (!IsServer || state == null || board == null)
        {
            return;
        }

        if (!turnSystem.CanAct(state))
        {
            return;
        }

        LobbySide senderSide = GetSenderSide(rpcParams.Receive.SenderClientId);
        if (!DoesSideMatchTurn(senderSide, state.currentTurn))
        {
            return;
        }

        PieceModel movingPiece = state.GetPiece(pieceId);
        if (movingPiece == null || movingPiece.isCaptured)
        {
            return;
        }

        if (movingPiece.role != state.currentTurn)
        {
            return;
        }

        if (!turnSystem.CanUsePiece(state, pieceId))
        {
            return;
        }

        HexCoord fromBeforeMove = movingPiece.position;

        PlayerAction action = new PlayerAction(pieceId, new HexCoord(q, r));
        bool applied = rules.TryApplyAction(action, state, board);
        if (!applied)
        {
            return;
        }

        turnSystem.SpendAction(state, pieceId, fromBeforeMove);

        state.result = rules.GetGameResult(state);

        if (state.result == GameResult.None && !turnSystem.CanAct(state))
        {
            BeginTurnServer(turnSystem.NextRole(state.currentTurn));
        }

        PublishSnapshotServer();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestEndTurnEarlyServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || state == null)
        {
            return;
        }

        LobbySide senderSide = GetSenderSide(rpcParams.Receive.SenderClientId);
        if (senderSide != LobbySide.Defender)
        {
            return;
        }

        if (state.currentTurn != Role.Defender)
        {
            return;
        }

        if (state.turnProgress != null && state.turnProgress.movesUsed < 1)
        {
            return;
        }

        BeginTurnServer(turnSystem.NextRole(state.currentTurn));
        PublishSnapshotServer();
    }

    // ============================================================
    // CLIENT: APPLY REPLICATED STATE
    // ============================================================

    private void HandleStateJsonChanged(FixedString4096Bytes previous, FixedString4096Bytes current)
    {
        string json = current.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        GameStateSnapshot snap = JsonUtility.FromJson<GameStateSnapshot>(json);
        if (snap == null)
        {
            return;
        }

        ApplySnapshotLocal(snap);
    }

    private void ApplySnapshotLocal(GameStateSnapshot snap)
    {
        EnsureSceneReferences();

        if (board == null)
        {
            BuildBoardLocal();
        }

        state = SnapshotToState(snap);

        if (boardView != null)
        {
            boardView.SyncPieces(state);
            boardView.ClearHighlights();
        }

        ApplyPerspectiveIfPossible();

        StateChanged?.Invoke(state);
    }

    private void BuildBoardLocal()
    {
        if (board == null)
        {
            board = new BoardModel();

            List<(string id, HexCoord coord)> attackers;
            List<(string id, HexCoord coord)> defenders;
            List<(string id, HexCoord coord)> flags;

            BoardMapBuilder.Build(boardMapConfig, board, out attackers, out defenders, out flags);
        }

        if (boardView != null)
        {
            boardView.Build(board);
        }
    }

    private void EnsureSceneReferences()
    {
        if (boardView == null)
        {
            boardView = FindFirstObjectByType<BoardView>();
        }

        if (turnPerspective == null)
        {
            turnPerspective = FindFirstObjectByType<TurnPerspectiveController>();
        }
    }

    private void ApplyPerspectiveIfPossible()
    {
        if (turnPerspective == null || state == null)
        {
            return;
        }

        // IMPORTANT: LAN perspective is "who I am", not "is it my turn"
        LobbySide side = GetLocalSide();
        bool isLocalAttacker = side == LobbySide.Attacker;

        turnPerspective.Apply(state.currentTurn, isLocalAttacker);
    }

    // ============================================================
    // SNAPSHOT (INCLUDING TURN PROGRESS!)
    // ============================================================

    [Serializable]
    private sealed class GameStateSnapshot
    {
        public string result;
        public int currentTurn;

        public int movesUsed;
        public int movesAllowed;

        public List<PieceSnapshot> pieces = new List<PieceSnapshot>();
        public List<FlagSnapshot> flags = new List<FlagSnapshot>();

        [Serializable]
        public sealed class PieceSnapshot
        {
            public string id;
            public int role;
            public int q;
            public int r;
            public bool isCaptured;
            public string carryingFlagId;
        }

        [Serializable]
        public sealed class FlagSnapshot
        {
            public string id;
            public bool isCaptured;
            public string carrierPieceId;
            public bool hasLocation;
            public int q;
            public int r;
        }
    }

    private static GameStateSnapshot BuildSnapshot(GameState src)
    {
        GameStateSnapshot snap = new GameStateSnapshot
        {
            result = src.result.ToString(),
            currentTurn = (int)src.currentTurn,
            movesUsed = src.turnProgress != null ? src.turnProgress.movesUsed : 0,
            movesAllowed = src.turnProgress != null ? src.turnProgress.movesAllowed : 0
        };

        foreach (PieceModel p in src.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            GameStateSnapshot.PieceSnapshot ps = new GameStateSnapshot.PieceSnapshot
            {
                id = p.id,
                role = (int)p.role,
                q = p.position.q,
                r = p.position.r,
                isCaptured = p.isCaptured,
                carryingFlagId = p.carryingFlagId
            };

            snap.pieces.Add(ps);
        }

        foreach (FlagModel f in src.flags.Values)
        {
            if (f == null)
            {
                continue;
            }

            bool hasLoc = f.location.HasValue;

            GameStateSnapshot.FlagSnapshot fs = new GameStateSnapshot.FlagSnapshot
            {
                id = f.id,
                isCaptured = f.isCaptured,
                carrierPieceId = f.carrierPieceId,
                hasLocation = hasLoc,
                q = hasLoc ? f.location.Value.q : 0,
                r = hasLoc ? f.location.Value.r : 0
            };

            snap.flags.Add(fs);
        }

        return snap;
    }

    private static GameState SnapshotToState(GameStateSnapshot snap)
    {
        GameState gs = new GameState();
        gs.result = ParseResult(snap.result);
        gs.currentTurn = (Role)snap.currentTurn;

        // TurnProgress is readonly in your project, so DO NOT assign it.
        // Instead, update it if it exists.
        if (gs.turnProgress != null)
        {
            gs.turnProgress.movesUsed = snap.movesUsed;
            gs.turnProgress.movesAllowed = snap.movesAllowed;
        }

        for (int i = 0; i < snap.pieces.Count; i++)
        {
            GameStateSnapshot.PieceSnapshot ps = snap.pieces[i];

            PieceModel p = new PieceModel(ps.id, (Role)ps.role, new HexCoord(ps.q, ps.r));
            p.isCaptured = ps.isCaptured;
            p.carryingFlagId = string.IsNullOrWhiteSpace(ps.carryingFlagId) ? null : ps.carryingFlagId;

            gs.AddPiece(p);
        }

        for (int i = 0; i < snap.flags.Count; i++)
        {
            GameStateSnapshot.FlagSnapshot fs = snap.flags[i];

            HexCoord home = fs.hasLocation ? new HexCoord(fs.q, fs.r) : new HexCoord(0, 0);
            FlagModel f = new FlagModel(fs.id, home);

            f.isCaptured = fs.isCaptured;
            f.carrierPieceId = string.IsNullOrWhiteSpace(fs.carrierPieceId) ? null : fs.carrierPieceId;
            f.location = fs.hasLocation ? new HexCoord(fs.q, fs.r) : (HexCoord?)null;

            gs.AddFlag(f);
        }

        return gs;
    }


    private static GameResult ParseResult(string raw)
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

    // ============================================================
    // LOBBY ROLE HELPERS
    // ============================================================

    private LobbySide GetSenderSide(ulong clientId)
    {
        if (LanLobbyState.Instance == null)
        {
            return LobbySide.None;
        }

        if (LanLobbyState.Instance.TryGetSide(clientId, out LobbySide side))
        {
            return side;
        }

        for (int i = 0; i < LanLobbyState.Instance.Players.Count; i++)
        {
            LobbyPlayerData p = LanLobbyState.Instance.Players[i];
            if (p.ClientId == clientId)
            {
                return p.Side;
            }
        }

        return LobbySide.None;
    }

    private static bool DoesSideMatchTurn(LobbySide side, Role turn)
    {
        if (turn == Role.Attacker)
        {
            return side == LobbySide.Attacker;
        }

        if (turn == Role.Defender)
        {
            return side == LobbySide.Defender;
        }

        return false;
    }
}
