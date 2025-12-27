using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fast, one-ply AI that scores each legal move by simulating it and evaluating the resulting state.
/// Designed to feel "intentional" without search depth.
/// </summary>
public sealed class HeuristicBrain : MonoBehaviour, IAIBrain
{
    [Header("Search")]
    [Tooltip("If true, the brain simulates each candidate move via RulesEngine before scoring.")]
    [SerializeField] private bool _simulateMoves = true;

    [Header("Evaluation")]
    [SerializeField] private int _winScore = 1_000_000;
    [SerializeField] private int _loseScore = -1_000_000;

    [Tooltip("Huge reward per flag already captured (removed from board).")]
    [SerializeField] private int _capturedFlagBonus = 250_000;

    [Tooltip("Reward for carrying a flag (encourages pickup and protection).")]
    [SerializeField] private int _carryingFlagBonus = 40_000;

    [Tooltip("Reward per step closer to return while carrying a flag.")]
    [SerializeField] private int _carryToReturnStepBonus = 4_000;

    [Tooltip("Reward per step closer to the nearest uncaptured flag.")]
    [SerializeField] private int _towardFlagStepBonus = 1_000;

    [Tooltip("Penalty when a flag carrier is adjacent to a defender.")]
    [SerializeField] private int _carrierAdjDefenderPenalty = 12_000;

    [Tooltip("Attackers: small penalty per attacker that has been captured.")]
    [SerializeField] private int _attackerDownPenalty = 15_000;

    [Tooltip("Defenders: reward per attacker captured.")]
    [SerializeField] private int _attackerCapturedRewardForDefender = 30_000;

    [Tooltip("Defenders: reward for moves that end adjacent to an attacker (pressure).")]
    [SerializeField] private int _defenderPressureBonus = 1_500;

    [Tooltip("Small random jitter added to break ties.")]
    [SerializeField] private int _tieBreakJitter = 3;

    private readonly RulesEngine _rules = new RulesEngine();
    private readonly TurnSystem _turnSystem = new TurnSystem(new TurnRules(), new MoveBudget());
    private readonly MovementRules _movement = new MovementRules();

    public PlayerAction ChooseAction(List<PlayerAction> legal, GameState state, BoardModel board)
    {
        if (legal == null || legal.Count == 0 || state == null || board == null)
        {
            return null;
        }

        Role myRole = state.currentTurn;

        PlayerAction best = legal[0];
        int bestScore = int.MinValue;

        for (int i = 0; i < legal.Count; i++)
        {
            PlayerAction a = legal[i];

            int score;
            if (_simulateMoves)
            {
                (GameState s2, BoardModel b2) = Clone(state, board);
                bool ok = SimApplyOneAction(a, s2, b2);
                score = ok ? EvaluatePosition(s2, b2, myRole) : int.MinValue / 2;
            }
            else
            {
                // Cheap fallback: score by destination only (kept for debugging).
                score = ScoreDestinationOnly(a, state, board);
            }

            if (_tieBreakJitter > 0)
            {
                score += UnityEngine.Random.Range(-_tieBreakJitter, _tieBreakJitter + 1);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        return best;
    }

    // ============================================================
    // Evaluation
    // ============================================================

    private int EvaluatePosition(GameState state, BoardModel board, Role perspective)
    {
        if (state == null || board == null)
        {
            return 0;
        }

        // Terminal.
        if (state.result == GameResult.AttackersWin)
        {
            return perspective == Role.Attacker ? _winScore : _loseScore;
        }

        if (state.result == GameResult.DefendersWin)
        {
            return perspective == Role.Defender ? _winScore : _loseScore;
        }

        int score = 0;

        // Flags captured.
        int capturedFlags = 0;
        foreach (FlagModel f in state.flags.Values)
        {
            if (f != null && f.isCaptured)
            {
                capturedFlags++;
            }
        }

        if (capturedFlags > 0)
        {
            int flagScore = capturedFlags * _capturedFlagBonus;
            score += perspective == Role.Attacker ? flagScore : -flagScore;
        }

        // Count pieces.
        int attackersAlive = 0;
        int attackersCaptured = 0;
        List<PieceModel> attackerPieces = new List<PieceModel>(8);
        List<PieceModel> defenderPieces = new List<PieceModel>(4);

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            if (p.role == Role.Attacker)
            {
                attackerPieces.Add(p);
                if (!p.isCaptured)
                {
                    attackersAlive++;
                }
                else
                {
                    attackersCaptured++;
                }
            }
            else if (p.role == Role.Defender)
            {
                defenderPieces.Add(p);
            }
        }

        // Attacker attrition.
        if (attackersCaptured > 0)
        {
            int attr = attackersCaptured * _attackerDownPenalty;
            score += perspective == Role.Attacker ? -attr : attr;
        }

        // Defender reward for captures.
        if (attackersCaptured > 0)
        {
            int cap = attackersCaptured * _attackerCapturedRewardForDefender;
            score += perspective == Role.Defender ? cap : -cap;
        }

        HexCoord returnHex = FindReturnHex(board);

        // Evaluate attackers' objectives.
        foreach (PieceModel a in attackerPieces)
        {
            if (a == null || a.isCaptured)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(a.carryingFlagId))
            {
                // Carry to return.
                score += (perspective == Role.Attacker ? +1 : -1) * _carryingFlagBonus;
                int distToReturn = HexDistance(a.position, returnHex);
                score += (perspective == Role.Attacker ? +1 : -1) * (_carryToReturnStepBonus * (12 - Mathf.Clamp(distToReturn, 0, 12)));

                // If a defender is adjacent to the carrier, penalize (danger).
                if (IsAnyDefenderAdjacent(a.position, defenderPieces, board))
                {
                    score += (perspective == Role.Attacker ? -1 : +1) * _carrierAdjDefenderPenalty;
                }
            }
            else
            {
                // Move toward nearest available flag.
                int best = int.MaxValue;
                foreach (FlagModel f in state.flags.Values)
                {
                    if (f == null || f.isCaptured)
                    {
                        continue;
                    }

                    HexCoord? loc = GetFlagLocation(f, state, board);
                    if (!loc.HasValue)
                    {
                        continue;
                    }

                    int d = HexDistance(a.position, loc.Value);
                    if (d < best)
                    {
                        best = d;
                    }
                }

                if (best != int.MaxValue)
                {
                    int toward = _towardFlagStepBonus * (12 - Mathf.Clamp(best, 0, 12));
                    score += (perspective == Role.Attacker ? +1 : -1) * toward;
                }
            }
        }

        // Evaluate defenders' pressure / positioning.
        foreach (PieceModel d in defenderPieces)
        {
            if (d == null || d.isCaptured)
            {
                continue;
            }

            int adjacentAttackers = CountAdjacentAttackers(d.position, attackerPieces, board);
            if (adjacentAttackers > 0)
            {
                int press = adjacentAttackers * _defenderPressureBonus;
                score += (perspective == Role.Defender ? +1 : -1) * press;
            }
        }

        // Slight bias: if it's your turn and you have remaining actions, small bonus.
        if (state.currentTurn == perspective)
        {
            int remaining = Mathf.Max(0, state.turnProgress.movesAllowed - state.turnProgress.movesUsed);
            score += remaining * 250;
        }

        return score;
    }

    private int ScoreDestinationOnly(PlayerAction action, GameState state, BoardModel board)
    {
        int score = 0;

        if (action == null || state == null || board == null)
        {
            return score;
        }

        if (board.TryGetHex(action.destination, out HexModel hex))
        {
            if (hex.tag == HexTag.Flag)
            {
                score += 1000;
            }

            if (hex.tag == HexTag.Return)
            {
                score += 50;
            }
        }

        return score;
    }

    // ============================================================
    // Simulation helpers
    // ============================================================

    private bool SimApplyOneAction(PlayerAction action, GameState state, BoardModel board)
    {
        if (action == null || state == null || board == null)
        {
            return false;
        }

        if (!_turnSystem.CanAct(state))
        {
            return false;
        }

        if (!_turnSystem.CanUsePiece(state, action.pieceId))
        {
            return false;
        }

        PieceModel movingPiece = state.GetPiece(action.pieceId);
        HexCoord fromBeforeMove = movingPiece != null ? movingPiece.position : new HexCoord(0, 0);

        bool applied = _rules.TryApplyAction(action, state, board);
        if (!applied)
        {
            return false;
        }

        _turnSystem.SpendAction(state, action.pieceId, fromBeforeMove);
        state.result = _rules.GetGameResult(state);

        if (state.result != GameResult.None)
        {
            return true;
        }

        if (_turnSystem.CanAct(state))
        {
            return true;
        }

        Role next = _turnSystem.NextRole(state.currentTurn);
        _turnSystem.BeginTurn(state, next);
        return true;
    }

    // ============================================================
    // Pure helpers
    // ============================================================

    private static (GameState state, BoardModel board) Clone(GameState srcState, BoardModel srcBoard)
    {
        // Clone board.
        BoardModel board = new BoardModel();
        foreach (HexModel h in srcBoard.AllHexes)
        {
            HexModel nh = new HexModel(h.coord, h.tag);
            nh.occupantPieceId = h.occupantPieceId;
            nh.flagId = h.flagId;
            board.AddHex(nh);
        }

        // Clone state.
        GameState state = new GameState();
        state.currentTurn = srcState.currentTurn;
        state.result = srcState.result;

        state.turnProgress.movesAllowed = srcState.turnProgress.movesAllowed;
        state.turnProgress.movesUsed = srcState.turnProgress.movesUsed;
        state.turnProgress.firstMovedPieceId = srcState.turnProgress.firstMovedPieceId;
        state.turnProgress.firstMoveFrom = srcState.turnProgress.firstMoveFrom;

        foreach (PieceModel p in srcState.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            PieceModel np = new PieceModel(p.id, p.role, p.position);
            np.isCaptured = p.isCaptured;
            np.carryingFlagId = p.carryingFlagId;
            state.AddPiece(np);
        }

        foreach (FlagModel f in srcState.flags.Values)
        {
            if (f == null)
            {
                continue;
            }

            // If location is null, use a dummy coord. We'll immediately overwrite isCaptured/carrier/location.
            HexCoord home = f.location.HasValue ? f.location.Value : new HexCoord(0, 0);
            FlagModel nf = new FlagModel(f.id, home);
            nf.isCaptured = f.isCaptured;
            nf.carrierPieceId = f.carrierPieceId;
            nf.location = f.location;
            state.AddFlag(nf);
        }

        return (state, board);
    }

    private static HexCoord FindReturnHex(BoardModel board)
    {
        foreach (HexModel h in board.AllHexes)
        {
            if (h != null && h.tag == HexTag.Return)
            {
                return h.coord;
            }
        }

        return new HexCoord(0, 0);
    }

    private static HexCoord? GetFlagLocation(FlagModel flag, GameState state, BoardModel board)
    {
        if (flag == null)
        {
            return null;
        }

        if (flag.isCaptured)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(flag.carrierPieceId))
        {
            PieceModel carrier = state.GetPiece(flag.carrierPieceId);
            if (carrier != null)
            {
                return carrier.position;
            }
        }

        return flag.location;
    }

    private static bool IsAnyDefenderAdjacent(HexCoord pos, List<PieceModel> defenders, BoardModel board)
    {
        List<HexCoord> neighbors = board.GetNeighbors(pos);
        for (int i = 0; i < neighbors.Count; i++)
        {
            HexCoord n = neighbors[i];
            for (int j = 0; j < defenders.Count; j++)
            {
                PieceModel d = defenders[j];
                if (d == null || d.isCaptured)
                {
                    continue;
                }

                if (d.position.q == n.q && d.position.r == n.r)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int CountAdjacentAttackers(HexCoord pos, List<PieceModel> attackers, BoardModel board)
    {
        int count = 0;
        List<HexCoord> neighbors = board.GetNeighbors(pos);
        for (int i = 0; i < neighbors.Count; i++)
        {
            HexCoord n = neighbors[i];
            for (int j = 0; j < attackers.Count; j++)
            {
                PieceModel a = attackers[j];
                if (a == null || a.isCaptured)
                {
                    continue;
                }

                if (a.position.q == n.q && a.position.r == n.r)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int HexDistance(HexCoord a, HexCoord b)
    {
        int dq = a.q - b.q;
        int dr = a.r - b.r;
        int ds = (a.q + a.r) - (b.q + b.r);
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
    }
}
