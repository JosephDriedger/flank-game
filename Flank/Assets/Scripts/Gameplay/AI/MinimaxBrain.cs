using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Depth-limited minimax (with alpha-beta pruning) for Flank.
/// Note: In Flank, a "turn" can contain multiple moves (attackers: 2 different pieces; defenders: 1 piece twice).
/// This brain searches at the granularity of individual moves (plies) while faithfully simulating the turn budget.
/// </summary>
public sealed class MinimaxBrain : MonoBehaviour, IAIBrain
{
    [Header("Search")]
    [Tooltip("Number of plies (individual moves) to search. 3-5 is usually a good range for this board size.")]
    [SerializeField] private int _maxDepth = 4;

    [Tooltip("Hard cap on the number of nodes evaluated per ChooseAction call.")]
    [SerializeField] private int _nodeBudget = 10_000;

    [Tooltip("If true, randomly shuffles legal moves before searching to reduce predictability and help pruning.")]
    [SerializeField] private bool _shuffleMoves = true;

    [Tooltip("If true, orders moves with a quick heuristic to improve alpha-beta pruning.")]
    [SerializeField] private bool _orderMoves = true;

    [Header("Evaluation")]
    [SerializeField] private int _winScore = 1_000_000;
    [SerializeField] private int _loseScore = -1_000_000;
    [SerializeField] private int _capturedFlagBonus = 250_000;
    [SerializeField] private int _carryingFlagBonus = 40_000;
    [SerializeField] private int _carryToReturnStepBonus = 4_000;
    [SerializeField] private int _towardFlagStepBonus = 1_000;
    [SerializeField] private int _carrierAdjDefenderPenalty = 12_000;
    [SerializeField] private int _attackerDownPenalty = 15_000;
    [SerializeField] private int _attackerCapturedRewardForDefender = 30_000;
    [SerializeField] private int _defenderPressureBonus = 1_500;

    [Header("Fallback")]
    [SerializeField] private HeuristicBrain _fallback;

    private readonly RulesEngine _rules = new RulesEngine();
    private readonly TurnSystem _turnSystem = new TurnSystem(new TurnRules(), new MoveBudget());
    private readonly MovementRules _movement = new MovementRules();

    private int _nodes;

    public PlayerAction ChooseAction(List<PlayerAction> legal, GameState state, BoardModel board)
    {
        if (legal == null || legal.Count == 0 || state == null || board == null)
        {
            return null;
        }

        // If depth is effectively disabled, fall back.
        if (_maxDepth <= 1)
        {
            if (_fallback != null)
            {
                return _fallback.ChooseAction(legal, state, board);
            }

            return legal[UnityEngine.Random.Range(0, legal.Count)];
        }

        _nodes = 0;
        Role rootRole = state.currentTurn;

        List<PlayerAction> moves = new List<PlayerAction>(legal);

        if (_shuffleMoves)
        {
            ShuffleInPlace(moves);
        }

        if (_orderMoves)
        {
            OrderMovesBestFirst(moves, state, board, rootRole);
        }

        PlayerAction bestAction = moves[0];
        int bestValue = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        for (int i = 0; i < moves.Count; i++)
        {
            if (_nodes >= _nodeBudget)
            {
                break;
            }

            PlayerAction a = moves[i];
            (GameState s2, BoardModel b2) = Clone(state, board);
            if (!SimApplyOneAction(a, s2, b2))
            {
                continue;
            }

            int v = Minimax(s2, b2, rootRole, _maxDepth - 1, alpha, beta);
            if (v > bestValue)
            {
                bestValue = v;
                bestAction = a;
            }

            if (v > alpha)
            {
                alpha = v;
            }
        }

        // If we somehow failed to evaluate anything, use fallback.
        if (bestAction == null)
        {
            if (_fallback != null)
            {
                return _fallback.ChooseAction(legal, state, board);
            }

            return legal[UnityEngine.Random.Range(0, legal.Count)];
        }

        return bestAction;
    }

    // ============================================================
    // Core minimax
    // ============================================================

    private int Minimax(GameState state, BoardModel board, Role rootRole, int depth, int alpha, int beta)
    {
        _nodes++;

        if (state == null || board == null)
        {
            return 0;
        }

        if (_nodes >= _nodeBudget)
        {
            return EvaluatePosition(state, board, rootRole);
        }

        if (depth <= 0 || state.result != GameResult.None)
        {
            return EvaluatePosition(state, board, rootRole);
        }

        List<PlayerAction> legal = LegalMoveGenerator.GenerateLegalActionsForTurn(state, board, _movement);
        if (legal == null || legal.Count == 0)
        {
            // No legal actions: treat as end of turn (defenders may end early, but also handle stalemates).
            // Switch turn and evaluate from there.
            (GameState s2, BoardModel b2) = Clone(state, board);
            ForceEndTurn(s2);
            return EvaluatePosition(s2, b2, rootRole);
        }

        bool maximizing = state.currentTurn == rootRole;

        if (_shuffleMoves)
        {
            ShuffleInPlace(legal);
        }

        if (_orderMoves)
        {
            OrderMovesBestFirst(legal, state, board, rootRole);
        }

        if (maximizing)
        {
            int value = int.MinValue;
            for (int i = 0; i < legal.Count; i++)
            {
                if (_nodes >= _nodeBudget)
                {
                    break;
                }

                (GameState s2, BoardModel b2) = Clone(state, board);
                if (!SimApplyOneAction(legal[i], s2, b2))
                {
                    continue;
                }

                int child = Minimax(s2, b2, rootRole, depth - 1, alpha, beta);
                if (child > value)
                {
                    value = child;
                }

                if (value > alpha)
                {
                    alpha = value;
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            return value;
        }
        else
        {
            int value = int.MaxValue;
            for (int i = 0; i < legal.Count; i++)
            {
                if (_nodes >= _nodeBudget)
                {
                    break;
                }

                (GameState s2, BoardModel b2) = Clone(state, board);
                if (!SimApplyOneAction(legal[i], s2, b2))
                {
                    continue;
                }

                int child = Minimax(s2, b2, rootRole, depth - 1, alpha, beta);
                if (child < value)
                {
                    value = child;
                }

                if (value < beta)
                {
                    beta = value;
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            return value;
        }
    }

    // ============================================================
    // Evaluation (same spirit as HeuristicBrain, but self-contained)
    // ============================================================

    private int EvaluatePosition(GameState state, BoardModel board, Role perspective)
    {
        if (state == null || board == null)
        {
            return 0;
        }

        if (state.result == GameResult.AttackersWin)
        {
            return perspective == Role.Attacker ? _winScore : _loseScore;
        }

        if (state.result == GameResult.DefendersWin)
        {
            return perspective == Role.Defender ? _winScore : _loseScore;
        }

        int score = 0;

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

        int attackersCaptured = 0;
        List<PieceModel> attackers = new List<PieceModel>(8);
        List<PieceModel> defenders = new List<PieceModel>(4);

        foreach (PieceModel p in state.pieces.Values)
        {
            if (p == null)
            {
                continue;
            }

            if (p.role == Role.Attacker)
            {
                attackers.Add(p);
                if (p.isCaptured)
                {
                    attackersCaptured++;
                }
            }
            else if (p.role == Role.Defender)
            {
                defenders.Add(p);
            }
        }

        if (attackersCaptured > 0)
        {
            int attr = attackersCaptured * _attackerDownPenalty;
            score += perspective == Role.Attacker ? -attr : attr;

            int cap = attackersCaptured * _attackerCapturedRewardForDefender;
            score += perspective == Role.Defender ? cap : -cap;
        }

        HexCoord returnHex = FindReturnHex(board);

        foreach (PieceModel a in attackers)
        {
            if (a == null || a.isCaptured)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(a.carryingFlagId))
            {
                score += (perspective == Role.Attacker ? +1 : -1) * _carryingFlagBonus;
                int distToReturn = HexDistance(a.position, returnHex);
                score += (perspective == Role.Attacker ? +1 : -1) * (_carryToReturnStepBonus * (12 - Mathf.Clamp(distToReturn, 0, 12)));

                if (IsAnyDefenderAdjacent(a.position, defenders, board))
                {
                    score += (perspective == Role.Attacker ? -1 : +1) * _carrierAdjDefenderPenalty;
                }
            }
            else
            {
                int best = int.MaxValue;
                foreach (FlagModel f in state.flags.Values)
                {
                    if (f == null || f.isCaptured)
                    {
                        continue;
                    }

                    HexCoord? loc = GetFlagLocation(f, state);
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

        foreach (PieceModel d in defenders)
        {
            if (d == null || d.isCaptured)
            {
                continue;
            }

            int adjacent = CountAdjacentAttackers(d.position, attackers, board);
            if (adjacent > 0)
            {
                int press = adjacent * _defenderPressureBonus;
                score += (perspective == Role.Defender ? +1 : -1) * press;
            }
        }

        // Slight bias to having remaining moves in your current turn.
        if (state.currentTurn == perspective)
        {
            int remaining = Mathf.Max(0, state.turnProgress.movesAllowed - state.turnProgress.movesUsed);
            score += remaining * 250;
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

    private void ForceEndTurn(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (state.result != GameResult.None)
        {
            return;
        }

        Role next = _turnSystem.NextRole(state.currentTurn);
        _turnSystem.BeginTurn(state, next);
    }

    // ============================================================
    // Move ordering
    // ============================================================

    private void OrderMovesBestFirst(List<PlayerAction> moves, GameState state, BoardModel board, Role rootRole)
    {
        // Score each move with a quick 1-ply evaluation; sort descending for max player, ascending for min player.
        bool maximizing = state.currentTurn == rootRole;

        moves.Sort((a, b) =>
        {
            int sa = QuickMoveScore(a, state, board, rootRole);
            int sb = QuickMoveScore(b, state, board, rootRole);
            return maximizing ? sb.CompareTo(sa) : sa.CompareTo(sb);
        });
    }

    private int QuickMoveScore(PlayerAction action, GameState state, BoardModel board, Role rootRole)
    {
        if (action == null || state == null || board == null)
        {
            return 0;
        }

        (GameState s2, BoardModel b2) = Clone(state, board);
        if (!SimApplyOneAction(action, s2, b2))
        {
            return int.MinValue / 4;
        }

        return EvaluatePosition(s2, b2, rootRole);
    }

    private static void ShuffleInPlace<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ============================================================
    // Cloning + pure helpers
    // ============================================================

    private static (GameState state, BoardModel board) Clone(GameState srcState, BoardModel srcBoard)
    {
        BoardModel board = new BoardModel();
        foreach (HexModel h in srcBoard.AllHexes)
        {
            HexModel nh = new HexModel(h.coord, h.tag);
            nh.occupantPieceId = h.occupantPieceId;
            nh.flagId = h.flagId;
            board.AddHex(nh);
        }

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

    private static HexCoord? GetFlagLocation(FlagModel flag, GameState state)
    {
        if (flag == null || flag.isCaptured)
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
