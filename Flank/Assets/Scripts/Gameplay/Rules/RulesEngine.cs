
public sealed class RulesEngine
{
    private readonly MovementRules _movement = new MovementRules();
    private readonly FlagRules _flags = new FlagRules();
    private readonly CaptureRules _capture = new CaptureRules();
    private readonly WinRules _win = new WinRules();

    public bool TryApplyAction(PlayerAction action, GameState state, BoardModel board)
    {
        if (action == null || state == null || board == null)
        {
            return false;
        }

        PieceModel piece = state.GetPiece(action.pieceId);
        if (piece == null)
        {
            return false;
        }

        if (piece.isCaptured)
        {
            return false;
        }

        if (piece.role != state.currentTurn)
        {
            return false;
        }

        if (!_movement.IsLegalMove(piece, action.destination, state, board))
        {
            return false;
        }

        _movement.ApplyMove(piece, action.destination, board);

        _flags.TryPickupFlag(piece, state, board);

        if (piece.role == Role.Defender)
        {
            _capture.ResolveAfterDefenderMove(state, board, piece.id);
        }

        _flags.DropFlagsIfCarrierCaptured(state, board);
        _flags.TryCaptureAtReturn(piece, state, board);

        return true;
    }

    public GameResult GetGameResult(GameState state)
    {
        return _win.Evaluate(state);
    }

    public MovementRules GetMovementRules()
    {
        return _movement;
    }
}
